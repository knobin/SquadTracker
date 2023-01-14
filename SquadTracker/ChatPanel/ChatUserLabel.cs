using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Torlando.SquadTracker.RolesScreen;
using Torlando.SquadTracker.SquadPanel;

namespace Torlando.SquadTracker.ChatPanel
{
    public class ChatUserLabel : Label 
    {
        private readonly string _account;
        private readonly string _displayName;
        private readonly SquadManager _squadManager;
        private readonly ICollection<Role> _roles; 
    
        private readonly Color _partyUserColor = new Color(66, 183, 249, 255);
        private readonly Color _squadUserColor = new Color(199, 249, 100, 255);
        private readonly Color _cachedColor;
        
        private bool _isUpdatingMenu = false;

        public ChatUserLabel(SquadManager squadManager, ICollection<Role> roles, string account, string character, byte subgroup) : base()
        {
            _account = account.TrimStart(':');
            _squadManager = squadManager;
            _roles = roles;
            
            _displayName = (string.IsNullOrEmpty(character)) ? account : character;
            _cachedColor = (subgroup == 255) ? _squadUserColor : _partyUserColor;

            Text = _displayName + ":";
            ShowShadow = true;
            StrokeText = true;
            ShadowColor = Color.Black;
            TextColor = (subgroup == 255) ? _squadUserColor : _partyUserColor;
            AutoSizeWidth = true;

            SetTooltipText(GetPlayer());
        }

        protected override void OnMouseEntered(MouseEventArgs e)
        {
            SetTooltipText(GetPlayer());
            TextColor = Color.White;
            base.OnMouseEntered(e);
        }

        protected override void OnMouseLeft(MouseEventArgs e)
        {
            TextColor = _cachedColor;
            base.OnMouseLeft(e);
        }
        
        protected override void OnRightMouseButtonPressed(MouseEventArgs e)
        {
            var player = GetPlayer();
            if (player == null) return;
            
            if (Menu == null)
            {
                Menu = CreateMenu(player);
            }
            else
            {
                UpdateMenu(player);
                Menu.Show(e.MousePosition);
            }
        }

        private Player GetPlayer()
        {
            var squad = _squadManager.GetSquad();
            var player = squad.CurrentMembers.FirstOrDefault(s => s.AccountName == _account) ?? squad.FormerMembers.FirstOrDefault(s => s.AccountName == _account);
            return player;
        }

        private ContextMenuStrip CreateMenu(Player player)
        {
            var menu = new ContextMenuStrip();
            
            var name = _displayName;
            if (_displayName != _account)
                name = _displayName + " (" + _account + ")";
            
            var items = new List<ContextMenuStripItem>()
            {
                new ContextMenuStripItem(name)
            };

            var assignedRoles = player.Roles;

            foreach (var role in _roles.OrderBy(role => role.Name.ToLowerInvariant()))
            {
                var item = new ContextMenuStripItem(role.Name)
                {
                    CanCheck = true,
                    Checked = assignedRoles.Contains(role)
                };
                item.CheckedChanged += (sender, args) => {
                    OnContextMenuSelect(role.Name, item.Checked);
                };
                items.Add(item);
            }

            menu.AddMenuItems(items);
            menu.Show(Input.Mouse.Position);

            return menu;
        }
        
        private void UpdateMenu(Player player)
        {
            _isUpdatingMenu = true;
            
            foreach (var child in Menu.Children)
            {
                if (!(child is ContextMenuStripItem item)) continue;
                var hasRole = _roles.FirstOrDefault(role => role.Name.Equals(item.Text));
                if (hasRole == null) continue;
                item.Checked = player.Roles.Contains(hasRole);
            }

            _isUpdatingMenu = false;
        }
        
        private void OnContextMenuSelect(string name, bool isChecked)
        {
            if (_isUpdatingMenu) return;
            
            var selectedRole = _roles.FirstOrDefault(role => role.Name.Equals(name));
            if (selectedRole == null) return;

            var player = GetPlayer();
            if (player == null) return;
            
            if (isChecked)
                player.AddRole(selectedRole);
            else
                player.RemoveRole(selectedRole);
        }
        
        private void SetTooltipText(Player player)
        {
            if (player == null) return;
            
            var text = player.AccountName;

            text += " (";
            text += player.Role switch
            {
                0 => "Squad Leader",
                1 => "Lieutenant",
                2 => "Member",
                3 => "Invited",
                4 => "Applied",
                _ => "none"
            };
            text += ") | Joined: " + GetTimeJoinedString(player);

            if (player.JoinTime == 0)
                text += " (est)";

            var character = player.CurrentCharacter;
            if (character != null && character.Name != "")
            {
                var elite = Specialization.GetEliteName(character.Specialization, character.Profession);
                var core = Specialization.GetCoreName(character.Profession);
                text += "\n\n" + character.Name + "\n" + core + " (" + elite + ")";
            }

            var assignedRoles = player.Roles.OrderBy(role => role.Name.ToLowerInvariant()).ToList();
            if (assignedRoles.Count > 0)
            {
                var roleStr = "";
                for (var i = 0; i < assignedRoles.Count; i++)
                    roleStr += assignedRoles.ElementAt(i).Name + ((i != assignedRoles.Count - 1) ? ", " : "");
                text += "\n\nAssigned Roles: " + roleStr;
            }

            BasicTooltipText = text;
        }
        
        private static string GetTimeJoinedString(Player player)
        {
            return player.JoinTime != 0 ? UnixTimeStampToDateTime(player.JoinTime).ToString("HH:mm:ss") : DateTime.Now.ToString("HH:mm:ss");
        }
        
        private static DateTime UnixTimeStampToDateTime(double timestamp)
        {
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddSeconds(timestamp).ToLocalTime();
            return dt;
        }
    }
}