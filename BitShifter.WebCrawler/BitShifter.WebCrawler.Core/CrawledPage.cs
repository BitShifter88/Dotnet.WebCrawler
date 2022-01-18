using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace BitShifter.WebCrawler.Core
{
    public class CrawledPage : PageToCrawl
    {
        HtmlParser _angleSharpHtmlParser;

        readonly Lazy<IHtmlDocument> _angleSharpHtmlDocument;

        public CrawledPage(Uri uri)
            : base(uri)
        {
            _angleSharpHtmlDocument = new Lazy<IHtmlDocument>(InitializeAngleSharpHtmlParser);

            Content = new PageContent();
        }

        /// <summary>
        /// Lazy loaded AngleSharp IHtmlDocument (https://github.com/AngleSharp/AngleSharp) that can be used to retrieve/modify html elements on the crawled page.
        /// </summary>
        public virtual IHtmlDocument AngleSharpHtmlDocument => _angleSharpHtmlDocument.Value;

        /// <summary>
        /// Web request sent to the server.
        /// </summary>
        public HttpRequestMessage HttpRequestMessage { get; set; }

        /// <summary>
        /// Web response from the server.
        /// </summary>
        public HttpResponseMessage HttpResponseMessage { get; set; }

        /// <summary>
        /// The request exception that occurred during the request
        /// </summary>
        public HttpRequestException HttpRequestException { get; set; }

        /// <summary>
        /// The HttpClientHandler that was used to make the request to server
        /// </summary>
        public HttpClientHandler HttpClientHandler { get; set; }

        public override string ToString()
        {
            if (HttpResponseMessage == null)
                return Uri.AbsoluteUri;

            return $"{Uri.AbsoluteUri}[{Convert.ToInt32(HttpResponseMessage.StatusCode)}]";
        }

        /// <summary>
        /// Links parsed from page. This value is set by the WebCrawler.SchedulePageLinks() method only If the "ShouldCrawlPageLinks" rules return true or if the IsForcedLinkParsingEnabled config value is set to true.
        /// </summary>
        public IEnumerable<HyperLink> ParsedLinks { get; set; }

        /// <summary>
        /// The content of page request
        /// </summary>
        public PageContent Content { get; set; }

        /// <summary>
        /// A datetime of when the http request started
        /// </summary>
        public DateTime RequestStarted { get; set; }

        /// <summary>
        /// A datetime of when the http request completed
        /// </summary>
        public DateTime RequestCompleted { get; set; }

        /// <summary>
        /// A datetime of when the page content download started, this may be null if downloading the content was disallowed by the CrawlDecisionMaker or the inline delegate ShouldDownloadPageContent
        /// </summary>
        public DateTime? DownloadContentStarted { get; set; }

        /// <summary>
        /// A datetime of when the page content download completed, this may be null if downloading the content was disallowed by the CrawlDecisionMaker or the inline delegate ShouldDownloadPageContent
        /// </summary>
        public DateTime? DownloadContentCompleted { get; set; }

        /// <summary>
        /// The page that this pagee was redirected to
        /// </summary>
        public PageToCrawl RedirectedTo { get; set; }

        /// <summary>
        /// Time it took from RequestStarted to RequestCompleted in milliseconds
        /// </summary>
        public double Elapsed => (RequestCompleted - RequestStarted).TotalMilliseconds;


        private IHtmlDocument InitializeAngleSharpHtmlParser()
        {
            if (_angleSharpHtmlParser == null)
                _angleSharpHtmlParser = new HtmlParser();

            IHtmlDocument document;
            try
            {
                document = _angleSharpHtmlParser.ParseDocument(Content.Text);
            }
            catch (Exception e)
            {
                document = _angleSharpHtmlParser.ParseDocument("");
            }

            return document;
        }
    }

    public class PageContent
    {
        public PageContent()
        {
            Text = "";
        }

        /// <summary>
        /// The raw data bytes taken from the web response
        /// </summary>
        public byte[] Bytes { get; set; }

        /// <summary>
        /// String representation of the charset/encoding
        /// </summary>
        public string Charset { get; set; }

        /// <summary>
        /// The encoding of the web response
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// The raw text taken from the web response
        /// </summary>
        public string Text { get; set; }
    }

    public class HyperLink : IEquatable<HyperLink>
    {
        public string RawHrefValue { get; set; }

        public string RawHrefText { get; set; }

        public Uri HrefValue { get; set; }

        public override int GetHashCode()
        {
            return HrefValue != null ? HrefValue.AbsoluteUri.GetHashCode() : base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as HyperLink);
        }

        public bool Equals(HyperLink other)
        {
            return
                this.HrefValue != null && other.HrefValue != null ?
                    this.HrefValue.AbsoluteUri == other.HrefValue.AbsoluteUri :
                    this.RawHrefValue == other.RawHrefValue;
        }
    }
}
