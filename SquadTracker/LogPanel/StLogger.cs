using System;
using System.Collections.Generic;
using System.Linq;

namespace Torlando.SquadTracker.LogPanel
{
    public class StLogger
    {
        private readonly List<string> _logs = new List<string>();
        public static int Limit = 100;
        
        public delegate void LogHandler(string message);
        public event LogHandler OnLog;

        public void Info(string message)
        {
            if (_logs.Count >=Limit)
                _logs.RemoveAt(0);
            
            _logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            OnLog?.Invoke(_logs.Last());
        }

        public void Info(string message, params object[] args)
        {
            Info(string.Format(message, args));
        }

        public List<string> Logs()
        {
            return _logs;
        }

        public void Clear()
        {
            _logs.Clear();
        }
    }
}