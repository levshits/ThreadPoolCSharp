using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThreadPoolLibrary;

namespace TestApp
{
    class Task: ITask
    {
        public int secret { get; set; }

        public Task()
        {
            secret = GetHashCode();
        }

        public static void AsyncCallback(ITask task)
        {
            var t = task as Task;
            if (t != null)
            {
                Console.WriteLine("     callback " + t.secret);
            }
        }
        public void Execute()
        {
            Random random = new Random(DateTime.Now.Millisecond);
            Console.WriteLine(random.NextDouble());
            Thread.Sleep(10);
        }
    }
}
