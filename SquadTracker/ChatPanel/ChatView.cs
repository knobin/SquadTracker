using System;
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
        private StandardButton _clearButton;
        private readonly List<ChatEntry> _entries = new List<ChatEntry>();
        private static readonly Logger Logger = Logger.GetLogger<Module>();

        public Action OnClearClick;

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
            _clearButton = new StandardButton
            {
                Parent = buildPanel,
                Text = "Clear",
                Location = new Point(_mainPanel.Right - 135, _mainPanel.Top + 5)
            };
            _clearButton.Click += OnClearClicked;
        }

        protected override void Unload()
        {
            Logger.Info("Unloading ChatView");

            Clear();
            
            _clearButton.Click -= OnClearClicked;
            
            _clearButton.Parent = null;
            _clearButton.Dispose();

            _mainPanel.Parent = null;
            _mainPanel.Dispose();
        }

        private void OnClearClicked(object sender, System.EventArgs e)
        {
            Clear();
            OnClearClick?.Invoke();
        }

        private void Clear()
        {
            foreach (var entry in _entries)
            {
                entry.Parent = null;
                entry.Dispose();
            }
            
            _entries.Clear();
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
