using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BitShifter.WebCrawler.Core
{
    public class PageToCrawl
    {
        public Uri Uri { get; set; }

        public int DomainDepth { get; set; }

        public PageToCrawl(Uri uri)
        {
            Uri = uri;
        }

        public void Save(BinaryWriter bw)
        {

       
            bw.Write(Uri.ToString());
            bw.Write(DomainDepth);
        }

        public static PageToCrawl Load(BinaryReader br)
        {
            string uri = br.ReadString();
            int domainDepth = br.ReadInt32();
            PageToCrawl page = new PageToCrawl(new Uri(uri));
            page.DomainDepth = domainDepth;
            return page;
        }
    }
}
