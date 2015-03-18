using System;
using System.Collections.Generic;
using System.Threading;

namespace ThreadPoolLibrary
{
    /// <summary>
    /// Customer thread pool class
    /// </summary>
    public class ThreadPool{

        public int LifeTime { get; set; }
        private uint _maxThreads;

        public uint MaxThreads
        {
            get
            {
                return _maxThreads;
            }
            set
            {
                if (value<MinThreads || value==0)
                {
                    throw new ThreadPoolParameterException();
                }
                _maxThreads = value;
            }
        }

        private uint _minThreads;

        public uint MinThreads
        {
            get
            {
                return _minThreads;
            }
            set
            {
                if (value > MaxThreads)
                {
                    throw new ThreadPoolParameterException();
                }
                _minThreads = value;
            }
        }

        public ILogger Logger { get; set; }

        private Queue<PooledTask> _tasks = new Queue<PooledTask>();
        private List<WorkerThread> _workers = new List<WorkerThread>();
        private ManagerThread _manager;

        public ThreadPool(uint min, uint max, int lifetime)
        {
            if(min>max || max == 0)
                throw new ThreadPoolParameterException();
            LifeTime = lifetime;
            MaxThreads = max;
            MinThreads = min;
        }
        public void Run()
        {
            _manager = new ManagerThread(this);
            _manager.Run();
            InitializeMinWorkers();
        }

        public void Stop()
        {
            _manager.Stop();
            foreach (var worker in _workers)
            {
                worker.Stop();

            }
            //ToDo
            // Need to find the better logic for thread pool terminating
            var spin = 0;
            while (_workers.Count > 0)
            {
                if (_workers[0].Status == ThreadStatus.Died)
                {
                    _workers.RemoveAt(0);
                }
                else
                {
                    if (spin > 1000)
                    {
                        _workers[0].Terminate();
                        _workers.RemoveAt(0);
                    }
                }
                if (_workers.Count > 0 && spin%_workers.Count == 0)
                {
                    Thread.Yield();
                    Thread.Sleep(100);
                }
                spin++;
            }
        }

        public void QueueTask(ITask task, Action<ITask> callback = null)
        {
            if (task != null)
            {
                var pooledTask = new PooledTask(task, callback);
                lock (_tasks)
                {
                    _tasks.Enqueue(pooledTask);
                }
                if (_manager != null)
                {
                    _manager.WakeThread();
                }
            }
        }
        private void InitializeMinWorkers()
        {
            for (var i = 0; i < MinThreads; i++)
            {
                var thread = new WorkerThread(this) {Status = ThreadStatus.Ready, Task = null};
                lock (_workers)
                {
                    _workers.Add(thread);
                }
                thread.Run();
            }
        }
        private bool TryToDie(WorkerThread thread)
        {
            var result = false;
            lock (_workers)
            {
                if (thread.Status != ThreadStatus.Ready)
                {
                    if (_workers.Count > MinThreads)
                    {
                        if (_tasks.Count > 0)
                        {
                            _manager.WakeThread();
                        }
                        else
                        {
                            Log("Stopping thread "+ Thread.CurrentThread.ManagedThreadId.ToString());
                            _workers.Remove(thread);
                            result = true;
                        }
                    }
                }

            }
            return result;
        }

        public void Log(string msg)
        {
            var logger = Logger;
            if (logger!=null)
            {
                logger.Log(msg);
            }
        }
        /// <summary>
        /// Abstraction of thread in the pool.
        /// Can be waked up, terminated, stopped.
        /// Different type of thread in pool must implement own logic
        /// </summary>
        abstract class PooledTask
        {
            public ITask Task { get; private set; }
            public Action<ITask> Callback  { get; private set; }

            public PooledTask(ITask task, Action<ITask> callback = null)
            {
                Task = task;
                Callback = callback;
            }

            public void RunCallback()
            {
                var callback = Callback;
                if (callback != null)
                {
                    callback.Invoke(Task);
                }
            }
        }
        enum ThreadStatus
        {
            Ready,
            Sleep,
            Work,
            Died
        }
        abstract class  PooledThread
        {
            protected ThreadPool _pool;
            protected readonly AutoResetEvent RestEvent = new AutoResetEvent(false);
            protected bool IsRunned;
            protected Thread Thread;

            protected PooledThread(ThreadPool pool)
            {
                _pool = pool;
            }

            public void Run()
            {
                Thread = new Thread(ThreadStartFunct);
                Thread.Start(this);
                IsRunned = true;
            }

            public void Stop()
            {
                IsRunned = false;
                WakeThread();
            }

            public void Terminate()
            {
                var thread = Thread;
                if (thread != null && thread.IsAlive)
                {
                    thread.Abort();
                }
            }
            private static void ThreadStartFunct(Object obj)
            {
                var self = obj as PooledThread;
                if (self != null)
                {
                    self.ThreadMain();
                }
            }

            protected abstract void ThreadMain();

            public void WakeThread()
            {
                RestEvent.Set();
            }
        }

        class ManagerThread :PooledThread
        {
            public ManagerThread(ThreadPool pool):base(pool) { }
            protected override void ThreadMain()
            {
                _pool.Log("Sheduler thread started " + Thread.CurrentThread.ManagedThreadId.ToString());
                do
                {
                    lock(_pool._tasks)
                    { 
                    var count = _pool._tasks.Count;
                        for (int i = 0; i < count; i++)
                        {
                            var task = _pool._tasks.Peek();
                            bool isManaged = false;
                            lock (_pool._workers)
                            {
                                foreach (var worker in _pool._workers)
                                {
                                    if (worker.Status == ThreadStatus.Sleep)
                                    {
                                        worker.Task = task;
                                        worker.Status = ThreadStatus.Ready;
                                        _pool._tasks.Dequeue();
                                        worker.WakeThread();
                                        isManaged = true;
                                        break;
                                    }
                                }
                                if (!isManaged && _pool._workers.Count < _pool.MaxThreads)
                                {
                                    var thread = new WorkerThread(_pool) {Status = ThreadStatus.Ready, Task = task};
                                    _pool._tasks.Dequeue();
                                    thread.Run();
                                    _pool._workers.Add(thread);
                                }
                            }
                        }
                    }
                    RestEvent.WaitOne();
                } while (IsRunned);
                _pool.Log("Sheduler thread died " + Thread.CurrentThread.ManagedThreadId.ToString());
           }
        }

        class WorkerThread : PooledThread
        {
            public PooledTask Task { get; set; }
            public ThreadStatus Status { get; set; }
            public WorkerThread(ThreadPool pool) : base(pool) { }
            protected override void ThreadMain()
            {
                _pool.Log("Worker thread started " + Thread.CurrentThread.ManagedThreadId.ToString());
                bool isDied = false;
                do
                {
                    Status = ThreadStatus.Work;
                    var task = Task;
                    if (task != null)
                    {
                        try
                        {
                            _pool.Log("Executing task started");
                            task.Task.Execute();
                            _pool.Log("Executing task finished");
                            task.RunCallback();
                            _pool.Log("Callback runned");
                            
                        }
                        catch (Exception)
                        {
                            //do nothing
                        }
                        Task = null;
                    }
                    Status = ThreadStatus.Sleep;
                    _pool._manager.WakeThread();
                    if (!RestEvent.WaitOne(_pool.LifeTime))
                    {
                        isDied = _pool.TryToDie(this);
                    }
                } while (IsRunned && !isDied);
                _pool.Log("Worker thread died " + Thread.CurrentThread.ManagedThreadId.ToString());
                Status = ThreadStatus.Died;
            }
        }
    }
}
