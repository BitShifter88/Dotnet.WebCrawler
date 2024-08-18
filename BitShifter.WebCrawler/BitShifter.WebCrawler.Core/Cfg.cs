using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitShifter.WebCrawler.Core
{
    public class Cfg
    {
        public string[] Sites { get; set; }
        public HashSet<string> Hostes { get; set; } = new HashSet<string>();

        public void Init()
        {
            foreach (var site in Sites)
            {
                Hostes.Add(new Uri(site).Host);
            }
        }

        public bool IsPartOfSitesToScrape(Uri site)
        {
            return Hostes.Contains(site.Host);
        }
    }
}
