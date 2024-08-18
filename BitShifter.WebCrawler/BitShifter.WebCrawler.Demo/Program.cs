using AngleSharp.Io;
using BitShifter.WebCrawler.Core;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace BitShifter.WebCrawler.Demo
{
    class Program
    {
        const string File = "saveState.bin";

        static void Main(string[] args)
        {
            string cfgContent = System.IO.File.ReadAllText("settings.json");
            Cfg settings = JsonConvert.DeserializeObject<Cfg>(cfgContent);
            settings.Init();

            WebCrawlerEngine engine = new WebCrawlerEngine(new WebCrawlerParameters(), settings);
            engine.Initialize();

            foreach (var url in settings.Sites)
            {
                engine.AddUri(new Uri(url));
            }

            engine.OnPageProcessed = OnPagedProcessed;

            if (System.IO.File.Exists(File))
            {
                engine.Load(File);
            }


            Task.Run(() =>
            {
                engine.Start();
            });

            while (true)
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
            //Console.WriteLine(parm.Uri.ToString());
        }
    }
}
