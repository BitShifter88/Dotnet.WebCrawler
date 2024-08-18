using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;

namespace BitShifter.WebCrawler.Core
{
    class Scheduler
    {
        public SchedulerStatus Status { get; private set; } = SchedulerStatus.Running;
        public int Count { get { return _work.Count; } }

        int _workerThreads;
        List<WorkThread> _threads = new List<WorkThread>();
        Queue<Action> _work = new Queue<Action>();

        object _lock = new object();

        bool _pausing;

        public Scheduler(int workerThreads)
        {
            _workerThreads = workerThreads;

            for (int i = 0; i < _workerThreads; i++)
            {
                WorkThread workThread = new WorkThread();
                Thread t = new Thread(() => DoWork(workThread));
                workThread.Thread = t;
                t.Start();
                _threads.Add(workThread);
            }
        }

        public void Pause()
        {
            Console.WriteLine("Pausing...");
            _pausing = true;

            while (!IsPaused())
            {
                Console.WriteLine(_work.Count);
                Console.WriteLine(_threads.Where(i => i.IsWorking == true).Count());
                Thread.Sleep(1000);
            }

            Console.WriteLine("Paused!");
        }

        public bool IsPaused()
        {
            int working = _threads.Where(i => i.IsWorking == true).Count();
            if (_threads.Where(i => i.IsWorking == true).Count() == 0 && _work.Count == 0)
                return true;
            else
                return false;
        }

        public void Resume()
        {
            _pausing = false;
        }

        private void DoWork(WorkThread workThread)
        {
            while (Status != SchedulerStatus.Stoping)
            {
                Action work = GetWork();
                if (work != null)
                {
                    workThread.IsWorking = true;

                    work();
                }
                else
                    Thread.Sleep(1);
                workThread.IsWorking = false;
            }
        }

        private Action GetWork()
        {
            lock (_lock)
            {
                if (_work.Count == 0)
                    return null;
                else
                    return _work.Dequeue();
            }
        }

        public void EnqueueWork(Action work)
        {
            lock (_lock)
            {
                _work.Enqueue(work);
            }
        }
    }

    class WorkThread
    {
        public Thread Thread { get; set; }
        public bool IsWorking { get; set; }
    }


    enum SchedulerStatus
    {
        Running,
        Pausing,
        Paused,
        Stoping,
        Stoped,
    }
}
