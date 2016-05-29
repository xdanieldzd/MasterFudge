using System;
using System.Threading.Tasks;

namespace MasterFudge
{
    public class TaskWrapper
    {
        Task task;
        bool isStopping;

        public TaskWrapper() { }

        public void Start(Action action)
        {
            if (task != null) Stop();

            isStopping = false;
            task = new Task(() => { while (!isStopping) action(); });
            task.Start();
        }

        public void Stop()
        {
            isStopping = true;
            task.Wait(100);
        }
    }
}
