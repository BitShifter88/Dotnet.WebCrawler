﻿using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BitShifter.WebCrawler.Core
{
    public class WebContentExtractor
    {
        public virtual async Task<PageContent> GetContentAsync(HttpResponseMessage response)
        {
            var pageContent = new PageContent
            {
                Bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false),
            };
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            pageContent.Charset = GetCharset(response.Content.Headers, contentText);
            pageContent.Encoding = GetEncoding(pageContent.Charset);

            if (response.Content.Headers.ContentEncoding.Contains("gzip"))
            {
                // Decompress the response stream
                using (var stream = response.Content.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult())
                using (var decompressedStream = new GZipStream(stream, CompressionMode.Decompress))
                using (var reader = new StreamReader(decompressedStream))
                {
                    pageContent.Text = reader.ReadToEndAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                }
            }
            else
            {
                var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using (StreamReader sr = new StreamReader(contentStream, pageContent.Encoding))
                {
                    pageContent.Text = sr.ReadToEnd();
                }
            }


            return pageContent;
        }

        protected virtual string GetCharset(HttpContentHeaders headers, string body)
        {
            var charset = GetCharsetFromHeaders(headers);
            if (charset == null)
            {
                charset = GetCharsetFromBody(body);
            }

            return CleanCharset(charset);
        }

        protected virtual string GetCharsetFromHeaders(HttpContentHeaders headers)
        {
            string charset = null;
            if (headers.TryGetValues("content-type", out var ctypes))
            {
                var ctype = ctypes.ElementAt(0);
                var ind = ctype.IndexOf("charset=", StringComparison.CurrentCultureIgnoreCase);
                if (ind != -1)
                    charset = ctype.Substring(ind + 8);
            }
            return charset;
        }

        protected virtual string GetCharsetFromBody(string body)
        {
            string charset = null;
            if (body != null)
            {
                //find expression from : http://stackoverflow.com/questions/3458217/how-to-use-regular-expression-to-match-the-charset-string-in-html
                var match = Regex.Match(body, @"<meta(?!\s*(?:name|value)\s*=)(?:[^>]*?content\s*=[\s""']*)?([^>]*?)[\s""';]*charset\s*=[\s""']*([^\s""'/>]*)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    charset = string.IsNullOrWhiteSpace(match.Groups[2].Value) ? null : match.Groups[2].Value;
                }
            }

            return charset;
        }

        protected virtual Encoding GetEncoding(string charset)
        {
            var e = Encoding.UTF8;

            if (charset == null || charset.Trim() == string.Empty)
                return e;

            try
            {
                e = Encoding.GetEncoding(charset);
            }
            catch
            {
                //Log.Warning("Could not get Encoding for charset string [0]", charset);
            }

            return e;
        }

        protected virtual string CleanCharset(string charset)
        {
            //TODO temporary hack, this needs to be a configurable value
            if (charset == "cp1251") //Russian, Bulgarian, Serbian cyrillic
                charset = "windows-1251";

            return charset;
        }

        public virtual void Dispose()
        {
            // Nothing to do
        }
    }
}
