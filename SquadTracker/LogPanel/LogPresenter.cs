using System.Linq;
using Blish_HUD;
using Blish_HUD.Graphics.UI;
using Microsoft.Xna.Framework.Graphics;

namespace Torlando.SquadTracker.LogPanel
{
    internal class LogPresenter : Presenter<LogView, object>
    {
        private readonly StLogger _stLogger;
        private static readonly Logger Logger = Logger.GetLogger<Module>();
        
        public LogPresenter(LogView view, StLogger stLogger) : base(view, null)
        {
            _stLogger = stLogger;
        }

        protected override void UpdateView()
        {
            Logger.Info("Updating LogPresenter");

            var logs = _stLogger.Logs().ToList();
            foreach (var t in logs)
                AddLog(t);
            
            logs.Clear();

            View.OnClearClick = () =>
            {
                Module.StLogger.Clear();
                Module.StLogger.Info("Cleared StLogger.");
            };
            _stLogger.OnLog += AddLog;
        }

        protected override void Unload()
        {
            Logger.Info("Unloading LogPresenter");
            
            _stLogger.OnLog -= AddLog;
        }

        private void AddLog(string message)
        {
            if (View.Count() >= StLogger.Limit)
            {
                var diff = View.Count() - StLogger.Limit + 1;
                for (var i = 0; i < diff; ++i)
                    View.Pop();
            }

            View.Push(message);
        }
    }
}
