using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MasterFudge
{
    public sealed class Logger
    {
        public delegate void LogUpdateHandler(object sender, LogEventArgs e);
        public event LogUpdateHandler OnLogUpdate;

        public event EventHandler OnLogCleared;

        List<LogEvent> loggedEvents;

        public Logger()
        {
            loggedEvents = new List<LogEvent>();
        }

        public void WriteEvent(string format, params object[] param)
        {
            WriteEvent(string.Format(format, param));
        }

        public void WriteEvent(string message)
        {
            LogEvent newEvent = new LogEvent(DateTime.Now, message);
            loggedEvents.Add(newEvent);

            OnLogUpdate?.Invoke(this, new LogEventArgs(newEvent.EventTime, newEvent.Message));
        }

        public void ClearEvents()
        {
            loggedEvents.Clear();

            OnLogCleared?.Invoke(this, EventArgs.Empty);
        }
    }

    class LogEvent
    {
        public DateTime EventTime { get; private set; }
        public string Message { get; private set; }

        public LogEvent(DateTime eventTime, string message)
        {
            EventTime = eventTime;
            Message = message;
        }
    }

    public class LogEventArgs : EventArgs
    {
        public DateTime EventTime { get; private set; }
        public string Message { get; private set; }

        public LogEventArgs(DateTime eventTime, string message)
        {
            EventTime = eventTime;
            Message = message;
        }
    }
}
