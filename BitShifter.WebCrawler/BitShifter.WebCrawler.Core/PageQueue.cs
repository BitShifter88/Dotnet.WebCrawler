using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BitShifter.WebCrawler.Core
{
    public class PageQueue
    {
        Queue<PageToCrawl> _pageBuffer = new Queue<PageToCrawl>();

        object _lock = new object();

        public PageQueue()
        {
            //for (int i = 0; i < queueFragmentation; i++)
            //{
            //    _pageBuffer.Add(new Queue<PageToCrawl>());
            //}
        }

        public void Add(PageToCrawl page)
        {
            lock (_lock)
            {
                _pageBuffer.Enqueue(page);

            }
        }

        public int GetCount()
        {
            lock (_lock)
            {
                return _pageBuffer.Count;
            }
        }

        public PageToCrawl RequestPage()
        {
            lock (_lock)
            {

                if (_pageBuffer.Count == 0)
                    return null;
                else
                {
                    var page = _pageBuffer.Dequeue();
                    return page;
                }

            }
        }

        public void Load(BinaryReader br)
        {
            lock (_lock)
            {
                int count = br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var page = PageToCrawl.Load(br);
                    _pageBuffer.Enqueue(page);
                }
            }
        }

        public void Save(BinaryWriter bw)
        {
            lock (_lock)
            {
                bw.Write(_pageBuffer.Count);
                foreach (var item in _pageBuffer)
                {
                    item.Save(bw);
                }
            }
        }
    }
}
