using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Microsoft.Xna.Framework;

namespace Torlando.SquadTracker.LogPanel
{
    internal class LogView : View<LogPresenter>
    {
        #region Controls

        private Panel _mainPanel;
        private List<Label> _logs = new List<Label>();
        private static readonly Logger Logger = Logger.GetLogger<Module>();

        #endregion

        protected override void Build(Container buildPanel)
        {
            Logger.Info("Building LogView");
            
            // Main container
            _mainPanel = new Panel
            {
                Parent = buildPanel,
                CanScroll = true,
                ShowBorder = true,
                Location = new Point(buildPanel.ContentRegion.Left, buildPanel.ContentRegion.Top),
                Size = new Point(buildPanel.ContentRegion.Width, buildPanel.ContentRegion.Height),
                Title = "LogView"
            };
            
        }

        protected override void Unload()
        {
            Logger.Info("Unloading LogView");
            
            foreach (var label in _logs) 
                label.Dispose();
            
            _logs.Clear();
        }
        
        public int Count()
        {
            return _logs.Count;
        }
        
        public void Pop()
        {
            if (_logs.Count == 0) return;

            _logs.ElementAt(0).Parent = null;
            _logs.ElementAt(0).Dispose();
            _logs.RemoveAt(0);
        }

        public void Push(string message)
        {
            var log = new Label()
            {
                Parent = _mainPanel,
                Text = " " + message,
                ShowShadow = true,
                StrokeText = true,
                ShadowColor = Color.Black,
                Size = new Point(_mainPanel.ContentRegion.Width, 25)
            };
            _logs.Add(log);
            CalculateBottomUpPositions();
        }

        private void CalculateTopDownPositions()
        {
            var y = 0;
            foreach (var label in _logs)
            {
                var pos = label.Location;
                pos.Y = y;
                y += label.Size.Y;
                label.Location = pos;
            }
        }
        
        private void CalculateBottomUpPositions()
        {
            var y = 0;
            for (var i = 0; i < _logs.Count; ++i)
            {
                var label = _logs.ElementAt(_logs.Count - i - 1);
                var pos = label.Location;
                pos.Y = y;
                y += label.Size.Y;
                label.Location = pos;
            }
        }
    }
}
