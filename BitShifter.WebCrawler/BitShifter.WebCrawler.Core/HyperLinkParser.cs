using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using AngleSharp.Dom;

namespace BitShifter.WebCrawler.Core
{
    /// <summary>
    /// Parser that uses AngleSharp https://github.com/AngleSharp/AngleSharp to parse page links
    /// </summary>
    public class AngleSharpHyperlinkParser : HyperLinkParser
    {
        public AngleSharpHyperlinkParser()
        {
        }

        public AngleSharpHyperlinkParser(WebCrawlerParameters config, Func<string, string> cleanUrlFunc)
            : base(config, cleanUrlFunc)
        {

        }

        protected override string ParserType
        {
            get { return "AngleSharp"; }
        }

        protected override IEnumerable<HyperLink> GetRawHyperLinks(CrawledPage crawledPage)
        {
            if (HasRobotsNoFollow(crawledPage))
                return null;

            var hrefValues = crawledPage.AngleSharpHtmlDocument.QuerySelectorAll("a, area")
                .Where(e => !HasRelNoFollow(e))
                .Select(y => new HyperLink() { RawHrefValue = y.GetAttribute("href"), RawHrefText = y.Text() })
                .Where(e => !string.IsNullOrWhiteSpace(e.RawHrefValue));

            var canonicalHref = crawledPage.AngleSharpHtmlDocument
                .QuerySelectorAll("link")
                .Where(e => HasRelCanonicalPointingToDifferentUrl(e, crawledPage.Uri.ToString()))
                .Select(e => new HyperLink() { RawHrefValue = e.GetAttribute("href"), RawHrefText = e.Text() });

            List<HyperLink> hyperLinks = new List<HyperLink>();
            if (crawledPage.Uri.ToString().Contains("sitemap"))
            {
                try
                {
                    var test2 = crawledPage.Content.Text;
                    XDocument doc = XDocument.Parse(test2);
                    ForEachElement(doc.Root, element =>
                    {
                        if (element.Name.LocalName != "loc")
                            return;

                        if (string.IsNullOrEmpty(element.Value))
                            return;

                        hyperLinks.Add(new HyperLink() { HrefValue = new Uri(element.Value), RawHrefText = element.Value, RawHrefValue = element.Value });
                    });
                }
                catch (Exception e)
                {

                }
            }

            return hrefValues.Concat(canonicalHref).Concat(hyperLinks);
            }

            private void ForEachElement(XElement root, Action<XElement> action)
            {
                foreach (var element in root.Elements())
                {
                    action(element);
                    ForEachElement(element, action);
                }
            }

        protected override string GetBaseHrefValue(CrawledPage crawledPage)
        {
            var baseTag = crawledPage.AngleSharpHtmlDocument.QuerySelector("base");
            if (baseTag == null)
                return "";

            var baseTagValue = baseTag.Attributes["href"];
            if (baseTagValue == null)
                return "";

            return baseTagValue.Value.Trim();
        }

        protected override string GetMetaRobotsValue(CrawledPage crawledPage)
        {
            var robotsMeta = crawledPage.AngleSharpHtmlDocument
                .QuerySelectorAll("meta[name]")
                .FirstOrDefault(d => d.GetAttribute("name").ToLowerInvariant() == "robots");

            if (robotsMeta == null)
                return "";

            return robotsMeta.GetAttribute("content");
        }

        protected virtual bool HasRelCanonicalPointingToDifferentUrl(IElement e, string orginalUrl)
        {
            return e.HasAttribute("rel") && !string.IsNullOrWhiteSpace(e.GetAttribute("rel")) &&
                    string.Equals(e.GetAttribute("rel"), "canonical", StringComparison.OrdinalIgnoreCase) &&
                    e.HasAttribute("href") && !string.IsNullOrWhiteSpace(e.GetAttribute("href")) &&
                    !string.Equals(e.GetAttribute("href"), orginalUrl, StringComparison.OrdinalIgnoreCase);
        }

        protected virtual bool HasRelNoFollow(IElement e)
        {
            return false && (e.HasAttribute("rel") && e.GetAttribute("rel").ToLower().Trim() == "nofollow");
        }
    }


    public abstract class HyperLinkParser
    {
        protected WebCrawlerParameters Config;
        protected Func<string, string> CleanUrlFunc;

        protected HyperLinkParser()
            : this(new WebCrawlerParameters(), null)
        {

        }

        protected HyperLinkParser(WebCrawlerParameters config, Func<string, string> cleanUrlFunc)
        {
            Config = config;
            CleanUrlFunc = cleanUrlFunc;
        }

        /// <summary>
        /// Parses html to extract hyperlinks, converts each into an absolute url
        /// </summary>
        public virtual IEnumerable<HyperLink> GetLinks(CrawledPage crawledPage)
        {
            CheckParams(crawledPage);

            var timer = Stopwatch.StartNew();

            var links = GetHyperLinks(crawledPage, GetRawHyperLinks(crawledPage));

            timer.Stop();
            //Log.Debug("{0} parsed links from [{1}] in [{2}] milliseconds", ParserType, crawledPage.Uri, timer.ElapsedMilliseconds);

            return links;
        }

        #region Abstract

        protected abstract string ParserType { get; }

        protected abstract IEnumerable<HyperLink> GetRawHyperLinks(CrawledPage crawledPage);

        protected abstract string GetBaseHrefValue(CrawledPage crawledPage);

        protected abstract string GetMetaRobotsValue(CrawledPage crawledPage);

        #endregion

        protected virtual void CheckParams(CrawledPage crawledPage)
        {
            if (crawledPage == null)
                throw new ArgumentNullException("crawledPage");
        }

        protected virtual IEnumerable<HyperLink> GetHyperLinks(CrawledPage crawledPage, IEnumerable<HyperLink> rawLinks)
        {
            var finalList = new List<HyperLink>();
            if (rawLinks == null || !rawLinks.Any())
                return finalList;

            //Use the uri of the page that actually responded to the request instead of crawledPage.Uri (Issue 82).
            var uriToUse = crawledPage.HttpRequestMessage.RequestUri ?? crawledPage.Uri;

            //If html base tag exists use it instead of page uri for relative links
            var baseHref = GetBaseHrefValue(crawledPage);
            if (!string.IsNullOrEmpty(baseHref))
            {
                if (baseHref.StartsWith("//"))
                    baseHref = crawledPage.Uri.Scheme + ":" + baseHref;
                else if (baseHref.StartsWith("/"))
                    // '/' points to the root of the filesystem when running on Linux, and is as such
                    // considered an absolute URI
                    baseHref = uriToUse.GetLeftPart(UriPartial.Authority) + baseHref;

                if (Uri.TryCreate(uriToUse, baseHref, out Uri baseUri))
                    uriToUse = baseUri;
            }

            foreach (var rawLink in rawLinks)
            {
                try
                {
                    // Remove the url fragment part of the url if needed.
                    // This is the part after the # and is often not useful.
                    var href = false
                        ? rawLink.RawHrefValue
                        : rawLink.RawHrefValue.Split('#')[0];

                    var uriValueToUse = (CleanUrlFunc != null) ? new Uri(CleanUrlFunc(new Uri(uriToUse, href).AbsoluteUri)) : new Uri(uriToUse, href);

                    //rawLink is copied and setting its value directly is not reflected in the collection, must create another object
                    finalList.Add(
                        new HyperLink
                        {
                            RawHrefValue = rawLink.RawHrefValue,
                            RawHrefText = rawLink.RawHrefText,
                            HrefValue = uriValueToUse
                        });
                }
                catch (Exception e)
                {
                    //Log.Debug("Could not parse link [{0}] on page [{1}] {@Exception}", rawLink.RawHrefValue, crawledPage.Uri, e);
                }
            }

            return finalList.Distinct();
        }

        protected virtual bool HasRobotsNoFollow(CrawledPage crawledPage)
        {
            //X-Robots-Tag http header
            if (false)
            {
                //IEnumerable<string> xRobotsTagHeaderValues;
                //if (!crawledPage.HttpResponseMessage.Headers.TryGetValues("X-Robots-Tag", out xRobotsTagHeaderValues))
                //    return false;

                //var xRobotsTagHeader = xRobotsTagHeaderValues.ElementAt(0);
                //if (xRobotsTagHeader != null &&
                //    (xRobotsTagHeader.ToLower().Contains("nofollow") ||
                //     xRobotsTagHeader.ToLower().Contains("none")))
                //{
                //    //Log.Information("Http header X-Robots-Tag nofollow detected on uri [{0}], will not crawl links on this page.", crawledPage.Uri);
                //    return true;
                //}
            }

            //Meta robots tag
            if (false)
            {
                var robotsMeta = GetMetaRobotsValue(crawledPage);
                if (robotsMeta != null &&
                    (robotsMeta.ToLower().Contains("nofollow") ||
                     robotsMeta.ToLower().Contains("none")))
                {
                    //Log.Information("Meta Robots nofollow tag detected on uri [{0}], will not crawl links on this page.", crawledPage.Uri);
                    return true;
                }

            }

            return false;
        }
    }
}
