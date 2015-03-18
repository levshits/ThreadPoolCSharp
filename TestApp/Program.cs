using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using ThreadPoolLibrary;
using ThreadPool = ThreadPoolLibrary.ThreadPool;


namespace TestApp
{
    class Program: ILogger
    {
        private static ThreadPool pool;
        static void Main(string[] args)
        {
            pool  = new ThreadPool(3, 16, 1000);
            pool.Logger = new Program();
            pool.Run();
            for (int i = 0; i < 100000000; i++)
            {
                Task task = new Task();
                pool.QueueTask(task, Task.AsyncCallback);
            }
            Console.ReadLine();
            Console.WriteLine(pool.ToString());
            for (int i = 0; i < 100000000; i++)
            {
                Task task = new Task();
                pool.QueueTask(task, Task.AsyncCallback);
            }
            Console.ReadLine();
            pool.Stop();
        }

        public void Log(string msg)
        {
            Console.WriteLine("  "+msg);
        }
    }
}
