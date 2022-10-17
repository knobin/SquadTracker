using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System.Collections.Generic;
using System.Linq;
using Torlando.SquadTracker.RolesScreen;

namespace Torlando.SquadTracker.SquadInterface
{
    internal class SquadInterfaceSubgroup : Container
    {
        public uint Number { get; }

        private bool _isMouseOver = false;
        public Color ForegroundColor { get; set; }
        private readonly Color _hoverColor;
        private Rectangle _numberTextRect = new Rectangle();
        
        private BitmapFont _font;
        private readonly ContentService.FontSize _fontSize = ContentService.FontSize.Size18;

        private readonly ICollection<Role> _roles;

        private bool _isUpdatingMenu = false;

        public SquadInterfaceSubgroup(uint subgroupNumber, Color bgColor, Color hoverColor, ICollection<Role> roles)
        {
            this.Visible = true;
            this.Size = new Point(1, 1);

            this.Number = subgroupNumber;
            ForegroundColor = bgColor;
            _hoverColor = hoverColor;
            _roles = roles;
            
            _font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, _fontSize, ContentService.FontStyle.Regular);
        }

        private static int Compare(SquadInterfaceTile t1, SquadInterfaceTile t2)
        {
            return SquadPlayerSort.Compare(t1.Player, t2.Player);
        }

        public void UpdateTileSizes(int width, int textWidth, int ypadding, Point tileSize, uint rowCount, uint tileSpacing)
        {   
            var numberTextSize = _font.MeasureString(Number.ToString());
            _numberTextRect.Width = (int)numberTextSize.Width;
            _numberTextRect.Height = (int)numberTextSize.Height;
            _numberTextRect.X = (textWidth - _numberTextRect.Width) / 2;

            var tiles = _children.OfType<SquadInterfaceTile>().ToList();
            tiles.Sort(Compare);

            var point = new Point(textWidth, ypadding);
            var size = new Point(width, 0);
            int currentRowCount = 1;

            for (int i = 0; i < tiles.Count; i++)
            {
                tiles[i].Size = tileSize;
                tiles[i].Location = point;

                size.Y = (point.Y + tileSize.Y > size.Y) ? point.Y + tileSize.Y : size.Y;

                point.X += tileSize.X + (int)tileSpacing;

                if (++currentRowCount > rowCount)
                {
                    point.X = textWidth;
                    point.Y += tileSize.Y + (int)tileSpacing;
                    currentRowCount = 1;
                }
            }

            size.Y += ypadding;
            _numberTextRect.Y = (size.Y - (int)numberTextSize.Height) / 2;
            Size = size;
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            _font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, _fontSize, ContentService.FontStyle.Regular);

            if (_isMouseOver)
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(Point.Zero, Size), _hoverColor);
            else
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(Point.Zero, Size), ForegroundColor);

            spriteBatch.DrawStringOnCtrl(this, Number.ToString(), _font, _numberTextRect, Color.White);
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

        protected override void OnMouseEntered(MouseEventArgs e)
        {
            _isMouseOver = true;

            base.OnMouseEntered(e);
        }

        protected override void OnMouseLeft(MouseEventArgs e)
        {
            _isMouseOver = false;

            base.OnMouseLeft(e);
        }

        private ContextMenuStrip CreateMenu()
        {
            var menu = new ContextMenuStrip();

            var name = "Subgroup " + Number;
            var items = new List<ContextMenuStripItem>()
            {
                new ContextMenuStripItem(name)
            };

            foreach (var role in _roles.OrderBy(role => role.Name.ToLowerInvariant()))
            {
                var item = new ContextMenuStripItem(role.Name)
                {
                    CanCheck = true,
                    Checked = AllTilesHaveRole(role)
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
        
        private void OnContextMenuSelect(string name, bool isChecked)
        {
            if (_isUpdatingMenu) return;
            
            var selectedRole = _roles.FirstOrDefault(role => role.Name.Equals(name));
            if (selectedRole == null) return;

            foreach (var child in _children.ToList())
            {
                if (!(child is SquadInterfaceTile tile)) continue;
                
                if (isChecked)
                    tile.Player.AddRole(selectedRole);
                else
                    tile.Player.RemoveRole(selectedRole);
            }
        }

        private bool AllTilesHaveRole(Role role)
        {
            foreach (var child in _children.ToList())
            {
                if (!(child is SquadInterfaceTile tile)) continue;

                if (!tile.Player.Roles.Contains(role))
                    return false;
            }
                
            return true;
        }

        private void UpdateMenu()
        {
            _isUpdatingMenu = true;
            
            foreach (var child in Menu.Children)
            {
                if (!(child is ContextMenuStripItem item)) continue;
                var hasRole = _roles.FirstOrDefault(role => role.Name.Equals(item.Text));
                if (hasRole == null) continue;
                item.Checked = AllTilesHaveRole(hasRole);
            }

            _isUpdatingMenu = false;
        }
    }
}
