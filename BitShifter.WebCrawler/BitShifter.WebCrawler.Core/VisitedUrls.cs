using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace BitShifter.WebCrawler.Core
{
    class VisitedUrls
    {
        HashSet<long> _visitedUrls = new HashSet<long>();
        private object _lock = new object();

        public bool Add(Uri uri)
        {

        }
    }
}
