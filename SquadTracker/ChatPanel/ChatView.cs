using System.Collections.Generic;
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
        private List<ChatEntry> _entries = new List<ChatEntry>();
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
                Title = "ChatView"
            };
        }

        protected override void Unload()
        {
            Logger.Info("Unloading ChatView");
        }

        public void DisplayChatMessage(SquadManager squadManager, ICollection<Role> roles, string account, string character, byte subgroup, string timestamp, string message)
        {
            var msg = new ChatEntry(squadManager, roles, account, character, subgroup, timestamp, message)
            {
                Parent = _mainPanel
            };
        }
    }
}
