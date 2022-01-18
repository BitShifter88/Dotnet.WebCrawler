using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace BitShifter.WebCrawler.Core
{
    public class WebCrawlerEngine
    {
        public Action<PageProcessedParm> OnPageProcessed { get; set; }

        PageQueue _pageQueue;
        Scheduler _scheduler;
        AngleSharpHyperlinkParser _linkParser;
        PageRequester _pageRequester;
        VisitedUrls _visitedUrls;
        bool _paused;

        public WebCrawlerEngine(WebCrawlerParameters parm)
        {
            _pageQueue = new PageQueue();
            _scheduler = new Scheduler(parm.WorkerThreads);
            _linkParser = new AngleSharpHyperlinkParser();
            _pageRequester = new PageRequester(parm, new WebContentExtractor());
            _visitedUrls = new VisitedUrls();
        }


        public void AddUri(Uri uri)
        {
            _pageQueue.Add(new PageToCrawl(uri));
            _visitedUrls.Add(uri);
        }

        public void Load(string file)
        {
            using (BinaryReader br = new BinaryReader(new FileStream(file, FileMode.Open)))
            {
                _visitedUrls.Load(br);
                _pageQueue.Load(br);

                Console.WriteLine($"Loaded! visited urls: {_visitedUrls.Count()}. Queue: {_pageQueue.GetCount()}");
            }
        }

        public void Save(string file)
        {
            Pause();

            Console.WriteLine($"Saving... visited urls: {_visitedUrls.Count()}. Queue: {_pageQueue.GetCount()}");
            using (BinaryWriter bw = new BinaryWriter(new FileStream(file, FileMode.Create)))
            {
                _visitedUrls.Save(bw);
                _pageQueue.Save(bw);
            }
            Console.WriteLine("Saved!");
        }

        public void Resume()
        {
            _paused = false;
            _scheduler.Resume();
        }

        public void Pause()
        {
            _paused = true;
            _scheduler.Pause();
        }

        public void Start()
        {
            while (true)
            {
                while (_paused)
                    Thread.Sleep(1);

                if (_scheduler.Count > 100)
                {
                    Thread.Sleep(0);
                    continue;
                }
                PageToCrawl page = _pageQueue.RequestPage();
                if (page != null)
                {
                    _scheduler.EnqueueWork(() => { ProcessPage(page); });
                }
                else
                    Thread.Sleep(0);
            }
        }

        private void ProcessPage(PageToCrawl page)
        {
            _visitedUrls.Add(page.Uri);
            CrawledPage crawledPage = _pageRequester.MakeRequest(page.Uri);
            crawledPage.ParsedLinks = _linkParser.GetLinks(crawledPage);

            foreach (var link in crawledPage.ParsedLinks)
            {
                if (_visitedUrls.Contains(link.HrefValue) ||
                    !link.HrefValue.ToString().StartsWith("http"))
                    continue;

                _pageQueue.Add(new PageToCrawl(link.HrefValue));
            }

            if (OnPageProcessed != null)
                OnPageProcessed(new PageProcessedParm(page.Uri));
        }
    }

    public class PageProcessedParm
    {
        public Uri Uri { get; set; }
        public PageProcessedParm(Uri uri)
        {
            Uri = uri;
        }
    }
}
