//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;

//namespace BitShifter.WebCrawler.Core
//{
//    public class VisitedUrls
//    {
//        HashSet<long> _visitedUrls = new HashSet<long>();
//        private object _lock = new object();

//        public void Add(Uri uri)
//        {
//            lock (_lock)
//            {
//                _visitedUrls.Add(uri.GetHashCode());
//            }
//        }

//        public int Count()
//        {
//            lock (_lock)
//            {
//                return _visitedUrls.Count;
//            }
//        }

//        public bool Contains(Uri uri)
//        {
//            lock (_lock)
//            {
//                return _visitedUrls.Contains(uri.GetHashCode());
//            }
//        }

//        public void Save(BinaryWriter bw)
//        {
//            bw.Write(_visitedUrls.Count);
//            foreach (long url in _visitedUrls)
//            {
//                bw.Write(url);
//            }
//        }

//        public void Load(BinaryReader br)
//        {
//            _visitedUrls.Clear();
//            int count = br.ReadInt32();
//            for (int i = 0; i < count; i++)
//            {
//                long url = br.ReadInt64();
//                _visitedUrls.Add(url);
//            }
//        }
//    }
//}
