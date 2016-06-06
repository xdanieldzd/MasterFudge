using System.Threading.Tasks;

using MasterFudge.Emulation;

namespace MasterFudge
{
    public class TaskWrapper
    {
        Task task;
        bool isStopping;

        public TaskWrapper() { }

        public void Start(BaseUnit emulator)
        {
            if (task != null) Stop();

            isStopping = false;
            task = new Task(() =>
            {
                while (!isStopping)
                    emulator.Execute();
            }, TaskCreationOptions.LongRunning);
            task.Start();
        }

        public void Stop()
        {
            isStopping = true;
            task.Wait(100);
        }
    }
}
