//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;

//namespace BitShifter.WebCrawler.Core
//{
//    public class PageQueue
//    {
//        object _lock = new object();

//        ElasticSearchInterface _db;

//        public PageQueue(ElasticSearchInterface db)
//        {
//            _db = db;
//            //for (int i = 0; i < queueFragmentation; i++)
//            //{
//            //    _pageBuffer.Add(new Queue<PageToCrawl>());
//            //}
//        }

//        public void Add(PageToCrawl page)
//        {
//            _db.AddPageToCawl(page.Uri.ToString());
//        }

//        public int GetCount()
//        {
//            return -1;

//        }

//        public PageToCrawl RequestPage()
//        {
//            string url = _db.RequestPageToCrawl();
//            if (url == null)
//                return null;
//            return new PageToCrawl(new Uri(url));


//            //if (_pageBuffer.Count == 0)
//            //    return null;
//            //else
//            //{
//            //    var page = _pageBuffer.Dequeue();
//            //    return page;
//            //}
//        }

//        public void Load(BinaryReader br)
//        {
//            lock (_lock)
//            {
//                int count = br.ReadInt32();
//                for (int i = 0; i < count; i++)
//                {
//                    var page = PageToCrawl.Load(br);
//                    //_pageBuffer.Enqueue(page);
//                }
//            }
//        }

//        public void Save(BinaryWriter bw)
//        {
//            lock (_lock)
//            {
//                //bw.Write(_pageBuffer.Count);
//                //foreach (var item in _pageBuffer)
//                //{
//                //    item.Save(bw);
//                //}
//            }
//        }
//    }
//}
