using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Microsoft.Xna.Framework;
using Torlando.SquadTracker.RolesScreen;
using Torlando.SquadTracker.SquadPanel;

namespace Torlando.SquadTracker.ChatPanel
{
    internal class ChatView : View<ChatPresenter>
    {
        #region Controls

        private Panel _mainPanel;
        private readonly List<ChatEntry> _entries = new List<ChatEntry>();
        private static readonly Logger Logger = Logger.GetLogger<Module>();

        #endregion

        protected override void Build(Container buildPanel)
        {
            Logger.Info("Building ChatView");
            
            // Main container
            _mainPanel = new FlowPanel
            {
                Parent = buildPanel,
                FlowDirection = ControlFlowDirection.TopToBottom,
                CanScroll = true,
                ShowBorder = true,
                Location = new Point(buildPanel.ContentRegion.Left, buildPanel.ContentRegion.Top),
                Size = new Point(buildPanel.ContentRegion.Width, buildPanel.ContentRegion.Height),
                Title = "Squad Chat"
            };
        }

        protected override void Unload()
        {
            Logger.Info("Unloading ChatView");

            foreach (var entry in _entries)
            {
                entry.Parent = null;
                entry.Dispose();
            }
            
            _entries.Clear();

            _mainPanel.Parent = null;
            _mainPanel.Dispose();
        }

        public void DisplayChatMessage(SquadManager squadManager, ICollection<Role> roles, string account, string character, byte subgroup, string timestamp, string message)
        {
            var msg = new ChatEntry(squadManager, roles, account, character, subgroup, timestamp, message)
            {
                Parent = _mainPanel
            };
            _entries.Add(msg);
            CalculateTopDownPositions();
        }
        
        public int Count()
        {
            return _entries.Count;
        }
        
        public void RemoveFirst()
        {
            if (_entries.Count == 0) return;

            _entries.ElementAt(0).Parent = null;
            _entries.ElementAt(0).Dispose();
            _entries.RemoveAt(0);
        }

        private void CalculateTopDownPositions()
        {
            var y = 0;
            foreach (var label in _entries)
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
            for (var i = 0; i < _entries.Count; ++i)
            {
                var label = _entries.ElementAt(_entries.Count - i - 1);
                var pos = label.Location;
                pos.Y = y;
                y += label.Size.Y;
                label.Location = pos;
            }
        }
    }
}
