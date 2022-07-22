using System;
using System.Threading;
using System.Collections.Concurrent;

namespace Torlando.SquadTracker.SearchPanel
{
    internal class TaskQueue
    {
        private class ThreadData
        {
            public bool Run { get; set; } = false;
            public ConcurrentQueue<Action> Queue = new ConcurrentQueue<Action>();
            public ManualResetEvent CV = new ManualResetEvent(false);
        }

        private Thread _t;
        private ThreadData _tData = new ThreadData();

        public void Enqueue(Action task)
        {
            _tData.Queue.Enqueue(task);
            _tData.CV.Set();
        }

        public void Start()
        {
            _tData.Run = true;
            _t = new Thread(ThreadMain);
            _t.Start(_tData);
        }

        public void Stop()
        {
            _tData.Run = false;
            Enqueue(() => { });
            _t.Join();
        }

        private static void ThreadMain(Object parameterData)
        {
            ThreadData tData = (ThreadData)parameterData;
            while (tData.Run)
            {
                while (tData.Queue.Count == 0)
                    tData.CV.WaitOne();

                Action task;
                if (tData.Queue.TryDequeue(out task))
                    task.Invoke();
            }
        }
    }
}
