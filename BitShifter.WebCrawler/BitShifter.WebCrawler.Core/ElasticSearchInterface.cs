using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Nodes;
using Elastic.Clients.Elasticsearch.TransformManagement;
using Elastic.Transport;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitShifter.WebCrawler.Core
{
    public class ElasticSearchInterface
    {
        //ElasticsearchClient _client;

        object _sqlLiteLock = new object();
        object _mySqlLock = new object();

        Cfg _settings;

        KeyValueStore _pagesToCrawl;
        KeyValueStore _pagesBeingCrawled;


        string connectionString = "Server=192.168.50.106;Database=newsai;uid=john;pwd=q";

        // Query to execute

        MySqlConnection connection;

        public ElasticSearchInterface(Cfg settings)
        {
            _settings = settings;

            //ElasticsearchClientSettings eSettings = new ElasticsearchClientSettings(new Uri("http://192.168.50.106:9200"))
            //           .DefaultIndex("newsai")
            //           .ServerCertificateValidationCallback(CertificateValidations.AllowAll);
            //_client = new ElasticsearchClient(eSettings);

            //var response = _client.Ping();

            //if (!_client.Indices.Exists("newsai").Exists)
            //{
            //    var createIndexResponse = _client.Indices.CreateAsync("newsai", c => c
            //        .Mappings(m => m
            //            .Properties<ArticleEntity>(p => p
            //                .Keyword(p => p.Url)
            //            //.Keyword(p => p.SourceDomain)
            //            //.Keyword(p => p.TitlePage)
            //            //.Keyword(p => p.TitleRss)
            //            //.Keyword(p => p.LocalPath)
            //            //.Keyword(p => p.FileName)
            //            //.Keyword(p => p.Ancestor)
            //            //.Keyword(p => p.Descendant)
            //            //.Keyword(p => p.Version)
            //            //.Date(p => p.DateDownload)
            //            //.Date(p => p.DateModify)
            //            //.Date(p => p.DatePublish)
            //            //.Text(p => p.Title)
            //            //.Text(p => p.Text)
            //            //.Text(p => p.Authors)
            //            //.Keyword(p => p.ImageUrl)
            //            //.Keyword(p => p.Language)
            //            //.Keyword(p => p.Topics)
            //            //.Keyword(p => p.Bias)
            //            )
            //        )
            //    ).Result;
            //}
            //if (!_client.Indices.Exists("newsai-pagestocrawl").Exists)
            //{
            //    var createIndexResponse = _client.Indices.CreateAsync("newsai-pagestocrawl", c => c
            //            .Mappings(m => m
            //                .Properties<ArticleEntity>(p => p
            //                    .Keyword(p => p.Url)
            //                )
            //            )
            //        ).Result;
            //}

            connection = new MySqlConnection(connectionString);
            connection.Open();

            _pagesToCrawl = new KeyValueStore("pagesToCrawl.sqlite", 100);
            _pagesBeingCrawled = new KeyValueStore("pagesBeingCrawled.sqlite", -1);
            var keys = _pagesBeingCrawled.GetAllKeys();
            Console.WriteLine($"Old cache contained {keys.Count}");
            foreach (var key in keys)
            {
                _pagesBeingCrawled.Delete(key);
                if (!_pagesToCrawl.Contains(key))
                {
                    _pagesToCrawl.Set(key, "1");
                }
            }
            Console.WriteLine($"restored");

        }

        public void AddPageToCawl(string url)
        {
            lock (_sqlLiteLock)
            {
                //if (_beingCrawled.Contains(url))
                //    return;

                if (!_settings.IsPartOfSitesToScrape(new Uri(url)))
                    return;

                if (_pagesToCrawl.Contains(url))
                {
                    //Console.WriteLine($"Url already exists: {url}");
                    return;
                }


                _pagesToCrawl.Set(url, "1");

                //var searchResponse = _client.SearchAsync<ArticleEntity>(s => s
                //                                                        .Index("newsai-pagestocrawl")
                //                                                        .Query(q => q
                //                                                            .Term(t => t.Field(f => f.Url).Value(url))
                //                                                        )
                //                                                        .Size(1)  // We don't need the actual documents, just the count
                //                                                    ).Result;

                //if (searchResponse.Hits.Count > 0 || HasBeenCrawled(url))
                //{
                //    Console.WriteLine($"Url already exists: {url}");
                //    return;
                //}

                //var article = new ArticleEntity
                //{
                //    Url = url,
                //};

                //var indexResponse = _client.IndexAsync(article, idx => idx.Index("newsai-pagestocrawl")).Result;

                //Console.WriteLine($"ADD: {url}");
            }
        }

        public string RequestPageToCrawl()
        {
            lock (_sqlLiteLock)
            {
                string pageToCrawl = _pagesToCrawl.GetByIndex(0);

                if (pageToCrawl == null)
                {
                    Console.WriteLine("No more pages to crawl");
                    return null;
                }

                _pagesToCrawl.Delete(pageToCrawl);

                _pagesBeingCrawled.Set(pageToCrawl, "1");

                return pageToCrawl;
                //var searchResponse = _client.SearchAsync<ArticleEntity>(s => s
                //        .Index("newsai-pagestocrawl")
                //        .Size(20)
                //        ).Result;

                //if (searchResponse.IsValidResponse && searchResponse.Hits.Count > 0)
                //{
                //    var firstEntity = GetRandomItem(searchResponse.Hits.ToList());
                //    var deleteResponse = _client.Delete<ArticleEntity>(firstEntity.Id, d => d.Index("newsai-pagestocrawl"));
                //    if (_beingCrawled.Contains(firstEntity.Source.Url))
                //        return null;

                //    _beingCrawled.Add(firstEntity.Source.Url);

                //    //Console.WriteLine("REQUESTED: " + firstEntity.Source.Url);

                //    return firstEntity.Source.Url;
                //}
                //else
                //{
                //    Console.WriteLine("No more pages to crawl");
                //    return null;
                //}
            }
        }

        private T GetRandomItem<T>(List<T> items)
        {
            // Create an instance of the Random class
            Random rand = new Random();

            // Generate a random index
            int index = rand.Next(items.Count);

            // Return the random item
            return items[index];
        }

        //public bool HasBeenCrawled(string url)
        //{
        //    lock (_lock)
        //    {
        //        var searchResponse = _client.SearchAsync<ArticleEntity>(s => s
        //                                                                .Index("newsai")
        //                                                                .Query(q => q
        //                                                                    .Term(t => t.Field(f => f.Url).Value(url))
        //                                                                )
        //                                                                .Size(1)  // We don't need the actual documents, just the count
        //                                                            ).Result;

        //        if (searchResponse.IsValidResponse && searchResponse.Hits.Count > 0)
        //        {
        //            return true;
        //        }
        //        return false;
        //    }
        //}

        public bool HasBeenCrawled(string url)
        {
            lock (_mySqlLock)
            {
                return HasBeenCrawledRaw(url);
            }
        }

        private bool HasBeenCrawledRaw(string url)
        {
            string query = "SELECT COUNT(*) FROM articles WHERE url = @url";
            try
            {

                // Create a command object
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    // Add parameter to the command
                    command.Parameters.AddWithValue("@url", url);

                    // Execute the query and get the result
                    int count = Convert.ToInt32(command.ExecuteScalar());

                    // Check if the URL exists
                    if (count > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (MySqlException ex)
            {
                // Handle any errors that might have occurred
                Console.WriteLine("An error occurred while connecting to the database: " + ex.Message);
                return false;
            }
        }

        public void AddCrawledUrl(string url)
        {
            lock (_mySqlLock)
            {
                if (_pagesBeingCrawled.Contains(url))
                {
                    _pagesBeingCrawled.Delete(url);
                }

                if (HasBeenCrawled(url))
                    return;

                string sql = "INSERT INTO articles (url) VALUES (@url)";

                using (MySqlCommand cmd = new MySqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@url", url);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        Console.WriteLine(url);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An error occurred: " + ex.Message);
                    }
                }

            }
        }

        //public void AddCrawledUrl(string url)
        //{
        //    lock (_lock)
        //    {
        //        if (_pagesBeingCrawled.Contains(url))
        //        {
        //            _pagesBeingCrawled.Delete(url);
        //        }
        //        if (HasBeenCrawled(url))
        //        {
        //            return;
        //        }

        //        var article = new ArticleEntity
        //        {
        //            Url = url,
        //            SourceDomain = "",
        //            TitlePage = "",
        //            TitleRss = "",
        //            LocalPath = "",
        //            FileName = "",
        //            Ancestor = "",
        //            Descendant = "",
        //            Version = "",
        //            DateDownload = null,
        //            DateModify = null,
        //            DatePublish = null,
        //            Title = "",
        //            Text = "",
        //            Authors = "",
        //            ImageUrl = "",
        //            Language = "",
        //            Topics = [],
        //            Bias = []
        //        };

        //        var indexResponse = _client.IndexAsync(article, idx => idx.Index("newsai")).Result;
        //        if (indexResponse.IsValidResponse)
        //        {
        //            Console.WriteLine($"Bad response from elasticsearch: {indexResponse.ElasticsearchServerError.Error}");
        //        }
        //        Console.WriteLine($"CRAWLED: {url}");
        //    }
        //}
    }
}
