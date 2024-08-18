using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Nodes;
using Elastic.Clients.Elasticsearch.TransformManagement;
using Elastic.Transport;
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
        //VisitedUrls _visitedUrls;
        bool _paused;

        ElasticSearchInterface _db;

        Cfg _settings;

        public WebCrawlerEngine(WebCrawlerParameters parm, Cfg settings)
        {
            _settings = settings;
            _db = new ElasticSearchInterface(settings);
            _pageQueue = new PageQueue(_db);
            _scheduler = new Scheduler(parm.WorkerThreads);
            _linkParser = new AngleSharpHyperlinkParser();
            _pageRequester = new PageRequester(parm, new WebContentExtractor());
            //_visitedUrls = new VisitedUrls();

        }

        public void Initialize()
        {
            //    client.Indices.CreateAsync("newsai", index => index.Mappings(m => m
            //                                                            .Properties<ArticleEntity>(p => p
            //                                                                .Keyword(k => k.Name)

            //    var searchResponse = client.SearchAsync<string>(s => s
            //    .Query(q => q
            //        .Match(m => m
            //            .Field(f => f.Length)
            //            .Query("your search text")
            //        )
            //    )
            //).Result;
        }

        public void AddUri(Uri uri)
        {
            _pageQueue.Add(new PageToCrawl(uri));
            //_visitedUrls.Add(uri);
        }

        public void Load(string file)
        {
            using (BinaryReader br = new BinaryReader(new FileStream(file, FileMode.Open)))
            {
                //_visitedUrls.Load(br);
                _pageQueue.Load(br);

               // Console.WriteLine($"Loaded! visited urls: {_visitedUrls.Count()}. Queue: {_pageQueue.GetCount()}");
            }
        }

        public void Save(string file)
        {
            Pause();

            //Console.WriteLine($"Saving... visited urls: {_visitedUrls.Count()}. Queue: {_pageQueue.GetCount()}");
            using (BinaryWriter bw = new BinaryWriter(new FileStream(file, FileMode.Create)))
            {
                //_visitedUrls.Save(bw);
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
            CrawledPage crawledPage = _pageRequester.MakeRequest(page.Uri);
            if (crawledPage.HttpRequestException != null)
            {
                return;
            }
            crawledPage.ParsedLinks = _linkParser.GetLinks(crawledPage);

            foreach (var link in crawledPage.ParsedLinks)
            {
                if (_db.HasBeenCrawled(link.HrefValue.ToString()))
                    continue;

                _pageQueue.Add(new PageToCrawl(link.HrefValue));
            }

            _db.AddCrawledUrl(page.Uri.ToString());


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
