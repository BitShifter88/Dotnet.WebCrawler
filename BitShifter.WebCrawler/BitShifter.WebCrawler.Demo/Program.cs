using BitShifter.WebCrawler.Core;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BitShifter.WebCrawler.Demo
{
    class Program
    {
        const string File = "saveState.bin";

        static void Main(string[] args)
        {
            WebCrawlerEngine engine = new WebCrawlerEngine(new WebCrawlerParameters());
            engine.AddUri(new Uri("https://ekstrabladet.dk"));
            engine.OnPageProcessed = OnPagedProcessed;

            if (System.IO.File.Exists(File))
                engine.Load(File);

            Task.Run(() =>
            {
                engine.Start();
            });

            while(true)
            {
                string cmd = Console.ReadLine();

                if (cmd == "save")
                {
                    engine.Save(File);
                }
                if (cmd == "resume")
                {
                    engine.Resume();
                }
                if (cmd == "pause")
                {
                    engine.Pause();
                }
            }
        }

        private static void OnPagedProcessed(PageProcessedParm parm)
        {
            Console.WriteLine(parm.Uri.ToString());
        }
    }
}
