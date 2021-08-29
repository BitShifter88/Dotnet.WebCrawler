using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BitShifter.WebCrawler.Core
{
    class Scheduler
    {
        public SchedulerStatus Status { get; private set; } =  SchedulerStatus.Running;

        int _workerThreads;
        List<Thread> _threads = new List<Thread>();

        public Scheduler(int workerThreads)
        {
            _workerThreads = workerThreads;

            for (int i = 0; i < _workerThreads; i++)
            {
                Thread t = new Thread(new ThreadStart(DoWork));
                t.Start();
                _threads.Add(t);
            }
        }

        private void DoWork()
        {
            while(Status != SchedulerStatus.Stoping)
            {

            }
        }
    }

    enum SchedulerStatus
    {
        Running,
        Paused,
        Stoping,
        Stoped,
    }
}
