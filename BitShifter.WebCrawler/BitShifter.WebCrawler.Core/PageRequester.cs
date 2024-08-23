using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BitShifter.WebCrawler.Core
{
    public class PageRequester
    {
        private readonly WebCrawlerParameters _config;
        private readonly WebContentExtractor _contentExtractor;
        private readonly CookieContainer _cookieContainer = new CookieContainer();
        private HttpClientHandler _httpClientHandler;
        private HttpClient _httpClient;

        public PageRequester(WebCrawlerParameters config, WebContentExtractor contentExtractor, HttpClient httpClient = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _contentExtractor = contentExtractor ?? throw new ArgumentNullException(nameof(contentExtractor));

            if (_config.HttpServicePointConnectionLimit > 0)
                ServicePointManager.DefaultConnectionLimit = _config.HttpServicePointConnectionLimit;

            _httpClient = httpClient;
        }

        /// <summary>
        /// Make an http web request to the url and download its content
        /// </summary>
        public virtual CrawledPage MakeRequest(Uri uri)
        {
            return MakeRequest(uri, (x) => new CrawlDecision { Allow = true });
        }

        /// <summary>
        /// Make an http web request to the url and download its content based on the param func decision
        /// </summary>
        public virtual CrawledPage MakeRequest(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            if (_httpClient == null)
            {
                _httpClientHandler = BuildHttpClientHandler(uri);
                _httpClient = BuildHttpClient(_httpClientHandler);
            }

            var crawledPage = new CrawledPage(uri);
            HttpResponseMessage response = null;
            try
            {
                crawledPage.RequestStarted = DateTime.Now;
                using (var requestMessage = BuildHttpRequestMessage(uri))
                {
                    response = _httpClient.SendAsync(requestMessage, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
                }

                var statusCode = Convert.ToInt32(response.StatusCode);
                if (statusCode < 200 || statusCode > 399)
                    throw new HttpRequestException($"Server response was unsuccessful, returned [http {statusCode}]");
            }
            catch (HttpRequestException hre)
            {
                crawledPage.HttpRequestException = hre;
                //Log.Debug("Error occurred requesting url [{0}] {@Exception}", uri.AbsoluteUri, hre);
            }
            catch (TaskCanceledException ex)
            {
                crawledPage.HttpRequestException = new HttpRequestException("Request timeout occurred", ex);//https://stackoverflow.com/questions/10547895/how-can-i-tell-when-httpclient-has-timed-out
               
                Console.WriteLine("Error calling url" + uri.AbsoluteUri + crawledPage.HttpRequestException.ToString());
            }
            catch (Exception e)
            {
                crawledPage.HttpRequestException = new HttpRequestException("Unknown error occurred", e);
                Console.WriteLine("Error calling url" + uri.AbsoluteUri + crawledPage.HttpRequestException.ToString());
            }
            finally
            {
                crawledPage.HttpRequestMessage = response?.RequestMessage;
                crawledPage.RequestCompleted = DateTime.Now;
                crawledPage.HttpResponseMessage = response;
                crawledPage.HttpClientHandler = _httpClientHandler;

                try
                {
                    if (response != null)
                    {
                        var shouldDownloadContentDecision = shouldDownloadContent(crawledPage);
                        if (shouldDownloadContentDecision.Allow)
                        {
                            crawledPage.DownloadContentStarted = DateTime.Now;
                            crawledPage.Content = _contentExtractor.GetContentAsync(response).ConfigureAwait(false).GetAwaiter().GetResult();

                            crawledPage.DownloadContentCompleted = DateTime.Now;
                        }
                        else
                        {
                            //Log.Debug("Links on page [{0}] not crawled, [{1}]", crawledPage.Uri.AbsoluteUri, shouldDownloadContentDecision.Reason);
                        }
                    }
                }
                catch (Exception e)
                {
                    //Log.Debug("Error occurred finalizing requesting url [{0}] {@Exception}", uri.AbsoluteUri, e);
                }
            }

            return crawledPage;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _httpClientHandler?.Dispose();
        }


        protected virtual HttpRequestMessage BuildHttpRequestMessage(Uri uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);

            request.Version = GetEquivalentHttpProtocolVersion();

            return request;
        }

        protected virtual HttpClient BuildHttpClient(HttpClientHandler clientHandler)
        {
            var httpClient = new HttpClient(clientHandler);

            httpClient.DefaultRequestHeaders.Add("User-Agent", _config.UserAgentString);
            httpClient.DefaultRequestHeaders.Add("Accept", "*/*");

            if (_config.HttpRequestTimeoutInSeconds > 0)
                httpClient.Timeout = TimeSpan.FromSeconds(_config.HttpRequestTimeoutInSeconds);

            //if (_config.IsAlwaysLogin)
            //{
            //    var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(_config.LoginUser + ":" + _config.LoginPassword));
            //    httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials);
            //}

            return httpClient;
        }

        protected virtual HttpClientHandler BuildHttpClientHandler(Uri rootUri)
        {
            if (rootUri == null)
                throw new ArgumentNullException(nameof(rootUri));

            var httpClientHandler = new HttpClientHandler
            {
                MaxAutomaticRedirections = _config.HttpRequestMaxAutoRedirects,
                UseDefaultCredentials = false
            };

            if (_config.IsHttpRequestAutomaticDecompressionEnabled)
                httpClientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            if (_config.HttpRequestMaxAutoRedirects > 0)
                httpClientHandler.AllowAutoRedirect = _config.IsHttpRequestAutoRedirectsEnabled;

            if (_config.IsSendingCookiesEnabled)
            {
                httpClientHandler.CookieContainer = _cookieContainer;
                httpClientHandler.UseCookies = true;
            }

            if (!_config.IsSslCertificateValidationEnabled)
                httpClientHandler.ServerCertificateCustomValidationCallback +=
                    (sender, certificate, chain, sslPolicyErrors) => true;

            //if (false && rootUri != null)
            //{
            //    //Added to handle redirects clearing auth headers which result in 401...
            //    //https://stackoverflow.com/questions/13159589/how-to-handle-authenticatication-with-httpwebrequest-allowautoredirect
            //    var cache = new CredentialCache();
            //    cache.Add(new Uri($"http://{rootUri.Host}"), "Basic", new NetworkCredential(_config.LoginUser, _config.LoginPassword));
            //    cache.Add(new Uri($"https://{rootUri.Host}"), "Basic", new NetworkCredential(_config.LoginUser, _config.LoginPassword));

            //    httpClientHandler.Credentials = cache;
            //}

            return httpClientHandler;
        }


        private Version GetEquivalentHttpProtocolVersion()
        {
            //if (_config.HttpProtocolVersion == HttpProtocolVersion.Version10)
            //    return HttpVersion.Version10;

            return HttpVersion.Version11;
        }

    }
}
