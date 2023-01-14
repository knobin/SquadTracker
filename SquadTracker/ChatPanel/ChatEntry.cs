using System.Collections.Generic;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Torlando.SquadTracker.RolesScreen;
using Torlando.SquadTracker.SquadPanel;

namespace Torlando.SquadTracker.ChatPanel
{
    public class ChatEntry : Container
    {
        private Label _timestamp;
        private ChatUserLabel _name;
        private Label _text;

        private readonly Color _partyTextColor = new Color(183, 216, 249, 255);
        private readonly Color _squadTextColor = new Color(199, 249, 232, 255);

        public ChatEntry(SquadManager squadManager, ICollection<Role> roles, string account, string character, byte subgroup, string timestamp, string message)
        {
            var stamp = timestamp.Substring(timestamp.LastIndexOf('T') + 1, 5);

            _timestamp = new Label()
            {
                Parent = this,
                Text = " [" + stamp + "] ",
                ShowShadow = true,
                StrokeText = true,
                ShadowColor = Color.Black,
                TextColor = Color.Gray,
                Location = new Point(0, 0),
                AutoSizeWidth = true
            };
            
            _name = new ChatUserLabel(squadManager, roles, account, character, subgroup)
            {
                Parent = this,
                Location = new Point(_timestamp.Size.X + 5, 0),
            };
            
            _text = new Label()
            {
                Parent = this,
                Text = message,
                ShowShadow = true,
                StrokeText = true,
                ShadowColor = Color.Black,
                TextColor = (subgroup == 255) ? _squadTextColor : _partyTextColor,
                Location = new Point(_name.Location.X + _name.Size.X + 5, 0),
                AutoSizeWidth = true
            };

            Size = new Point(_text.Location.X + _text.Size.X, _timestamp.Size.Y);
        }

        protected override void DisposeControl()
        {
            _timestamp.Parent = null;
            _name.Parent = null;
            _text.Parent = null;
            
            _timestamp.Dispose();
            _name.Dispose();
            _text.Dispose();

            _timestamp = null;
            _name = null;
            _text = null;
            
            base.DisposeControl();
        }
    }
}