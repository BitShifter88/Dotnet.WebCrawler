using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitShifter.WebCrawler.Core
{
    internal class ArticleEntity
    {
        public string Url { get; set; }
        public string SourceDomain { get; set; }
        public string TitlePage { get; set; }
        public string TitleRss { get; set; }
        public string LocalPath { get; set; }
        public string FileName { get; set; }
        public string Ancestor { get; set; }
        public string Descendant { get; set; }
        public string Version { get; set; }
        public string DateDownload { get; set; }
        public string DateModify { get; set; }
        public string DatePublish { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Text { get; set; }
        public string Authors { get; set; }
        public string ImageUrl { get; set; }
        public string Language { get; set; }
        public string[] Topics { get; set; }
        public string[] Bias { get; set; }
    }
}
