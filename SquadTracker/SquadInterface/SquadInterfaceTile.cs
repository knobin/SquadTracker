using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System.Collections.Generic;
using System.Linq;
using Torlando.SquadTracker.Helpers;
using Torlando.SquadTracker.RolesScreen;

namespace Torlando.SquadTracker.SquadInterface
{
    internal class SquadInterfaceTile : Control
    {
        public SquadInterfaceTile(Player player, AsyncTexture2D foregroundTexture, int displayHeightThreshold, ICollection<Role> roles)
        {
            _font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, _fontSize, ContentService.FontStyle.Regular);
            
            this.Visible = true;
            this.Player = player;
            this.Size = new Point(1, 1);

            ForegroundTexture = foregroundTexture;
            DisplayHeightThreshold = displayHeightThreshold;
            _roles = roles;

            _player.OnRoleUpdated += OnPlayerRoleUpdate;
            
            OnPlayerRoleUpdate(_player);
            // UpdateInformation();
            // SetDisplayName();
            // SetTooltipText(_player);
        }

        protected override void DisposeControl()
        {
            _player.OnRoleUpdated -= OnPlayerRoleUpdate;
            _player = null;
            base.DisposeControl();
        }

        public Player Player { get { return _player; } private set { _player = value;
            UpdateInformation();
        } }
        private Player _player;

        private string _displayName;
        private int DisplayHeightThreshold { get; set; }

        private AsyncTexture2D ForegroundTexture { get; set; } = null;
        public AsyncTexture2D Icon { get; set; } = null;
        private AsyncTexture2D RoleIcon1 { get; set; } = null;
        private AsyncTexture2D RoleIcon2 { get; set; } = null;

        private Color ForegroundColor = new Color(35, 35, 35, 55);
        private Color BorderColorInner = new Color(0, 0, 0, 0);
        private Color BorderColorOuter = new Color(0, 0, 0, 255);
        public bool DisplayInnerBorderColor { get; set; } = true;
        private uint BorderThickness { get; set; } = 1;

        private readonly ICollection<Role> _roles;
        
        private bool _isUpdatingMenu = false;

        private void OnPlayerRoleUpdate(Player player)
        {
            UpdateInformation();
        }
        
        public void UpdateInformation()
        {
            UpdateRoleInformation();
            UpdateDisplayedInformation();
        }
        
        private void UpdateRoleInformation()
        {
            RoleIcon1 = null;
            RoleIcon2 = null;

            BorderColorInner = new Color(0, 0, 0, 0);

            var assignedRoles = Player.Roles.OrderBy(role => role.Name.ToLowerInvariant()).ToList();
            if (assignedRoles.Count > 0)
            {
                RoleIcon1 = assignedRoles.ElementAt(0).Icon;
                
                if (DisplayInnerBorderColor)
                    BorderColorInner = RandomColorGenerator.Generate(assignedRoles.ElementAt(0).Name);
                
                if (assignedRoles.Count > 1)
                    RoleIcon2 = assignedRoles.ElementAt(1).Icon;
            }
            
            if (Player.IsSelf || Player.Role == 0)
                BorderColorOuter = new Color(211, 211, 211, 255);

            if (Player.Role == 1)
                BorderColorOuter = new Color(180, 180, 180, 255);

            if (Player.Tag != null)
                BorderColorOuter = RandomColorGenerator.Generate(Player.Tag);

            // Tell parent subgroup to reorder if necessary.  
            if (Parent is SquadInterfaceSubgroup sub)
                sub.UpdateTilePositions();
        }
        
        private void UpdateDisplayedInformation()
        {
            if (_player == null) return;
            
            SetDisplayName(); 
            SetTooltipText(_player);
        }

        protected override void OnResized(ResizedEventArgs e)
        {
            SetDisplayName();
            base.OnResized(e);
        }

        private readonly ContentService.FontSize _fontSize = ContentService.FontSize.Size14;
        private BitmapFont _font;

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            if (Player.IsInInstance)
                spriteBatch.DrawOnCtrl(this, ForegroundTexture, bounds);
            else
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, bounds, ForegroundColor);

            var spacing = bounds.Height / 20;
            var usedHeight = 0;

            if (bounds.Height > DisplayHeightThreshold)
            {
                var strSize = _font.MeasureString(_displayName);
                var strWidth = (int)strSize.Width;

                var rect = new Rectangle()
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
                var onlyicon = !(bounds.Height > DisplayHeightThreshold);

                var icond = bounds.Height - usedHeight - spacing - spacing;
                var rolescale = 8; // power of 2.
                var roled = icond - rolescale;

                var totalX = 0;
                totalX += (Icon != null && Player.IsInInstance) ? icond : 0;
                totalX += (!onlyicon && RoleIcon1 != null) ? roled : 0;
                totalX += (!onlyicon && RoleIcon2 != null) ? roled : 0;

                var xcount = 1;
                xcount += (Icon != null) ? 1 : 0;
                xcount += (!onlyicon && RoleIcon1 != null) ? 1 : 0;
                xcount += (!onlyicon && RoleIcon2 != null) ? 1 : 0;

                var xspacing = (bounds.Width - totalX) / (xcount);

                // Icon.
                var irect = new Rectangle()
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

            if (DisplayInnerBorderColor)
                PaintBorder(spriteBatch, bounds, BorderColorInner, (int)(2*BorderThickness));
            
            if (BorderColorOuter.A > 0)
                PaintBorder(spriteBatch, bounds, BorderColorOuter, (int)BorderThickness);
        }

        public void UpdateContextMenu()
        {
            Menu.Dispose();
            Menu = null;
        }

        private void PaintBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color, int thickness)
        {
            var rect = new Rectangle(Point.Zero, Point.Zero);

            // X axis.
            rect.Width = bounds.Width;
            rect.Height = thickness;
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, rect, color);
            rect.Y = bounds.Height - thickness;
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, rect, color);

            // Y axis.
            rect.Width = thickness;
            rect.Height = bounds.Height - (2 * thickness);
            rect.Y = thickness;
            rect.X = 0;
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, rect, color);
            rect.X = bounds.Width - thickness;
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, rect, color);
        }

        private void SetDisplayName()
        {
            var name = Player.AccountName;
            if (Player.CurrentCharacter != null && Player.CurrentCharacter.Name != "")
                name = Player.CurrentCharacter.Name;

            var spacing = Size.X / 40;
            var maxWidth = Size.X - spacing - spacing - (int)BorderThickness - (int)BorderThickness;

            var count = name.Length;
            while (count > 0)
            {
                var strSize = _font.MeasureString(name);
                var strWidth = (int)strSize.Width;

                if (strWidth < maxWidth)
                    break;

                name = name.Remove(name.Length - 1, 1);
                --count;
            }

            _displayName = name;
        }

        private void SetTooltipText(Player player)
        {
            string text = Player.AccountName;

            text += " (";
            text += Player.Role switch
            {
                0 => "Squad Leader",
                1 => "Lieutenant",
                2 => "Member",
                3 => "Invited",
                4 => "Applied",
                _ => "none"
            };
            text += ")";

            var character = Player.CurrentCharacter;
            if (character != null && character.Name != "")
            {
                var elite = Specialization.GetEliteName(character.Specialization, character.Profession);
                var core = Specialization.GetCoreName(character.Profession);
                text += "\n\n" + character.Name + "\n" + core + " (" + elite + ")";
            }

            var assignedRoles = Player.Roles.OrderBy(role => role.Name.ToLowerInvariant()).ToList();
            if (assignedRoles.Count > 0)
            {
                var roleStr = "";
                for (var i = 0; i < assignedRoles.Count; i++)
                    roleStr += assignedRoles.ElementAt(i).Name + ((i != assignedRoles.Count - 1) ? ", " : "");
                text += "\n\nAssigned Roles: " + roleStr;
            }

            BasicTooltipText = text;
        }

        private void OnContextMenuSelect(string name, bool isChecked)
        {
            if (_isUpdatingMenu) return;
            
            var selectedRole = _roles.FirstOrDefault(role => role.Name.Equals(name));
            if (selectedRole == null) return;
            
            if (isChecked)
                Player.AddRole(selectedRole);
            else
                Player.RemoveRole(selectedRole);
        }

        protected override void OnRightMouseButtonPressed(MouseEventArgs e)
        {
            if (Menu == null)
            {
                Menu = CreateMenu();
            }
            else
            {
                UpdateMenu();
                Menu.Show(e.MousePosition);
            }
        }

        private ContextMenuStrip CreateMenu()
        {
            var menu = new ContextMenuStrip();

            var name = (Player.CurrentCharacter != null) ? Player.CurrentCharacter.Name : Player.AccountName;
            var items = new List<ContextMenuStripItem>()
            {
                new ContextMenuStripItem(name)
            };

            var assignedRoles = Player.Roles;

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
        
        private void UpdateMenu()
        {
            _isUpdatingMenu = true;
            
            foreach (var child in Menu.Children)
            {
                if (!(child is ContextMenuStripItem item)) continue;
                var hasRole = _roles.FirstOrDefault(role => role.Name.Equals(item.Text));
                if (hasRole == null) continue;
                item.Checked = Player.Roles.Contains(hasRole);
            }

            _isUpdatingMenu = false;
        }
    }
}
