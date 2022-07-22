using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using Torlando.SquadTracker.RolesScreen;

namespace Torlando.SquadTracker.SquadInterface
{
    internal class SquadInterfaceTile : Control
    {
        public SquadInterfaceTile(Player player, bool self, AsyncTexture2D foregroundTexture, int displayHeightThreshold, ICollection<Role> roles)
        {
            this.Visible = true;
            this.Player = player;
            this.Size = new Point(1, 1);

            IsSelf = self;
            ForegroundTexture = foregroundTexture;
            DisplayHeightThreshold = displayHeightThreshold;
            _roles = roles;

            this.Player.OnRoleUpdated += SetTooltipText;

            SetDisplayName();
            SetTooltipText();
        }

        public Player Player { get { return _player; } set { _player = value; SetDisplayName(); SetTooltipText(); } }
        public bool IsSelf { get; set; } = false;
        private Player _player;

        private string _displayName;
        public int DisplayHeightThreshold { get; set; }

        public AsyncTexture2D ForegroundTexture { get; set; } = null;
        public AsyncTexture2D Icon { get; set; } = null;
        private AsyncTexture2D RoleIcon1 { get; set; } = null;
        private AsyncTexture2D RoleIcon2 { get; set; } = null;

        private Color ForegroundColor = new Color(35, 35, 35, 55);
        private Color BorderColor = new Color(0, 0, 0, 255);
        private Color BorderColorSelf = new Color(211, 211, 211, 255);
        public uint BorderThickness { get; set; } = 1;

        private readonly ICollection<Role> _roles;

        protected override void OnResized(ResizedEventArgs e)
        {
            SetDisplayName();
            base.OnResized(e);
        }

        public ContentService.FontSize FontSize = ContentService.FontSize.Size14;
        private BitmapFont _font;

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            if (Player.IsInInstance)
                spriteBatch.DrawOnCtrl(this, ForegroundTexture, bounds);
            else
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, bounds, ForegroundColor);

            int spacing = bounds.Height / 20;
            int usedHeight = 0;

            if (bounds.Height > DisplayHeightThreshold)
            {
                _font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, FontSize, ContentService.FontStyle.Regular);

                var strSize = _font.MeasureString(_displayName);
                int strWidth = (int)strSize.Width;

                Rectangle rect = new Rectangle()
                {
                    X = ((bounds.Width - strWidth) / 2),
                    Y = spacing,
                    Width = strWidth,
                    Height = (int)strSize.Height
                };

                usedHeight = rect.Y + rect.Height;

                spriteBatch.DrawStringOnCtrl(this, _displayName, _font, rect, Color.White);
            }

            if (RoleIcon1 != null || RoleIcon2 != null || Icon != null)
            {
                bool onlyicon = !(bounds.Height > DisplayHeightThreshold);

                int icond = bounds.Height - usedHeight - spacing - spacing;
                int rolescale = 8; // power of 2.
                int roled = icond - rolescale;

                int totalX = 0;
                totalX += (Icon != null && Player.IsInInstance) ? icond : 0;
                totalX += (!onlyicon && RoleIcon1 != null) ? roled : 0;
                totalX += (!onlyicon && RoleIcon2 != null) ? roled : 0;

                int xcount = 1;
                xcount += (Icon != null) ? 1 : 0;
                xcount += (!onlyicon && RoleIcon1 != null) ? 1 : 0;
                xcount += (!onlyicon && RoleIcon2 != null) ? 1 : 0;

                int xspacing = (bounds.Width - totalX) / (xcount);

                // Icon.
                Rectangle irect = new Rectangle()
                {
                    X = 0,
                    Y = usedHeight + spacing,
                    Width = 0,
                    Height = 0
                };

                if (Icon != null && Player.IsInInstance)
                {
                    irect.X += xspacing;
                    irect.Width = icond;
                    irect.Height = icond;
                    spriteBatch.DrawOnCtrl(this, Icon, irect);
                    irect.X += icond;
                }

                irect.Width = roled;
                irect.Height = roled;
                irect.Y += (rolescale / 2);

                if (!onlyicon && RoleIcon1 != null)
                {
                    irect.X += xspacing;
                    spriteBatch.DrawOnCtrl(this, RoleIcon1, irect);
                    irect.X += roled;
                }

                if (!onlyicon && RoleIcon2 != null)
                {
                    irect.X += xspacing;
                    spriteBatch.DrawOnCtrl(this, RoleIcon2, irect);
                }
            }

            if (IsSelf)
                PaintBorder(spriteBatch, bounds, BorderColorSelf);
            else
                PaintBorder(spriteBatch, bounds, BorderColor);
        }

        private void PaintBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color)
        {
            Rectangle rect = new Rectangle(Point.Zero, Point.Zero);

            // X axis.
            rect.Width = bounds.Width;
            rect.Height = (int)BorderThickness;
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, rect, color);
            rect.Y = bounds.Height - (int)BorderThickness;
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, rect, color);

            // Y axis.
            rect.Width = (int)BorderThickness;
            rect.Height = bounds.Height - (2 * (int)BorderThickness);
            rect.Y = (int)BorderThickness;
            rect.X = 0;
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, rect, color);
            rect.X = bounds.Width - (int)BorderThickness;
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, rect, color);
        }

        private void SetDisplayName()
        {
            string name = Player.AccountName;
            if (Player.CurrentCharacter != null && Player.CurrentCharacter.Name != "")
                name = Player.CurrentCharacter.Name;

            _font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, FontSize, ContentService.FontStyle.Regular);

            int spacing = Size.X / 40;
            int maxWidth = Size.X - spacing - spacing - (int)BorderThickness - (int)BorderThickness;

            int count = name.Length;
            while (count > 0)
            {
                var strSize = _font.MeasureString(name);
                int strWidth = (int)strSize.Width;

                if (strWidth < maxWidth)
                    break;

                name = name.Remove(name.Length - 1, 1);
                --count;
            }

            _displayName = name;
        }

        private void SetTooltipText()
        {
            RoleIcon1 = null;
            RoleIcon2 = null;

            List<Role> assignedRoles = Player.Roles.OrderBy(role => role.Name.ToLowerInvariant()).ToList();
            if (assignedRoles.Count > 0)
            {
                RoleIcon1 = assignedRoles.ElementAt(0).Icon;
                if (assignedRoles.Count > 1)
                    RoleIcon2 = assignedRoles.ElementAt(1).Icon;
            }

            string text = Player.AccountName;

            text += " (";
            if (Player.Role == 0)
                text += "SquadLeader";
            else if (Player.Role == 1)
                text += "Lieutenant";
            else if (Player.Role == 2)
                text += "Member";
            else
                text += "none";
            text += ")";

            Character character = Player.CurrentCharacter;
            if (character != null && character.Name != "")
            {
                string elite = Specialization.GetEliteName(character.Specialization, character.Profession);
                string core = Specialization.GetCoreName(character.Profession);
                text += "\n\n" + character.Name + "\n" + core + " (" + elite + ")";
            }

            string roleStr = "";
            for (int i = 0; i < assignedRoles.Count; i++)
                roleStr += assignedRoles.ElementAt(i).Name + ((i != assignedRoles.Count - 1) ? ", " : "");
            text += "\n\nAssigned Roles: " + roleStr;

            BasicTooltipText = text;
        }

        private void OnContextMenuSelect(string name, bool isChecked)
        {
            var selectedRole = _roles.FirstOrDefault(role => role.Name.Equals(name));
            if (selectedRole != null)
            {
                if (isChecked)
                    Player.AddRole(selectedRole);
                else
                    Player.RemoveRole(selectedRole);
            }
        }

        private ContextMenuStrip _menu = null;
        protected override void OnRightMouseButtonPressed(MouseEventArgs e)
        {
            if (_menu != null)
                _menu.Dispose();

            _menu = new ContextMenuStrip();

            string name = (Player.CurrentCharacter != null) ? Player.CurrentCharacter.Name : Player.AccountName;
            List <ContextMenuStripItem> items = new List<ContextMenuStripItem>()
            {
                new ContextMenuStripItem(name)
            };

            var assignedRoles = Player.Roles;

            foreach (var role in _roles.OrderBy(role => role.Name.ToLowerInvariant()))
            {
                ContextMenuStripItem item = new ContextMenuStripItem(role.Name)
                {
                    CanCheck = true,
                    Checked = assignedRoles.Contains(role)
                };
                item.CheckedChanged += (sender, args) => {
                    OnContextMenuSelect(role.Name, item.Checked);
                };
                items.Add(item);
            }

            _menu.AddMenuItems(items);
            _menu.Show(Input.Mouse.Position);

            base.OnRightMouseButtonPressed(e);
        }
    }
}
