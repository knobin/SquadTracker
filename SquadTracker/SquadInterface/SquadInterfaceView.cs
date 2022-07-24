using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Torlando.SquadTracker.RolesScreen;

namespace Torlando.SquadTracker.SquadInterface
{
    internal class SquadInterfaceView : Container
    {
        public SquadInterfaceView(PlayerIconsManager playerIconsManager, ICollection<Role> roles, AsyncTexture2D squad)
        {
            this.Location = new Point(0, 0);
            this.Size = new Point(600, 600);
            this.Visible = true;
            this.Padding = Thickness.Zero;

            _iconsManager = playerIconsManager;
            _roles = roles;
            _activeBackgroundColor = _bgColor;
            _tileLoadedTexture = squad;
            GenerateResizeArrow(_resizeArrowSize.X, _resizeArrowSize.Y, Color.LightGray);
            
            _errorMessage = new Label() 
            { 
                Font = GameService.Content.DefaultFont18, 
                Parent = this, 
                Visible = false,
                Location = new Point(10, 10)
            };
        }

        public bool EnableMoving = false;
        private Point _dragStart = new Point(0, 0);
        private bool _dragResizing = false;
        private bool _dragMoving = false;

        public string SelfAccountName { get; set; }
        private readonly ICollection<Role> _roles;

        private readonly Color _bgColor = new Color(0, 0, 0, 25);
        private readonly Color _hoverColor = new Color(5, 5, 5, 175);
        private readonly Color _subgroupHoverColor = new Color(0, 0, 0, 100);
        private readonly Color _subgroupColor1 = new Color(16, 16, 16, 90);
        private readonly Color _subgroupColor2 = new Color(20, 20, 20, 90);
        private Color _activeBackgroundColor;

        private readonly AsyncTexture2D _tileLoadedTexture;
        private readonly PlayerIconsManager _iconsManager;

        private Point _tileSize = new Point(87, 52);
        private readonly Point _tileMaxSize = new Point(87, 52);
        private readonly Point _tileMinSize = new Point(28, 28);
        private readonly int _tileTextThreshold = 35;
        private uint _tilesPerRow = 5;
        private const uint _tilesSplitPoint = 5;
        private readonly uint _tileSpacing = 5;

        private Point _minSize = new Point(40, 80);

        private readonly List<SquadInterfaceTile> _tiles = new List<SquadInterfaceTile>();
        private readonly List<SquadInterfaceSubgroup> _subgroups = new List<SquadInterfaceSubgroup>();

        private Texture2D _resizeArrowTexture;
        private Point _resizeArrowSize = new Point(20, 20);
        private bool _paintResizeArrow = false;

        private readonly Label _errorMessage;

        private void GenerateResizeArrow(int width, int height, Color color)
        {
            _resizeArrowTexture = new Texture2D(GameService.Graphics.GraphicsDevice, width, height);

            Color[] data = new Color[width * height];
            int x = width - 1;
            for (int i = 0; i < data.Length; ++i)
            {
                int col = i % width;
                if (col >= x)
                {
                    if (col == x)
                        x--;
                    data[i] = color;
                    data[i].A = 25;
                }
            }

            _resizeArrowTexture.SetData(data);
        }

        public void ShowErrorMessage(string message)
        {
            _errorMessage.Text = message;
            var strSize = _errorMessage.Font.MeasureString(_errorMessage.Text);
            int strWidth = (int)strSize.Width + 10;
            int strHeight = (int)strSize.Height + 10;
            _errorMessage.Size = new Point(strWidth, strHeight);

            _errorMessage.Visible = true;
        }

        public void HideErrorMessage()
        {
            _errorMessage.Visible = false;
            _errorMessage.Text = "";
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(Point.Zero, Size), _activeBackgroundColor);

            if (_paintResizeArrow)
            {
                Vector2 arrow = new Vector2(Location.X + Size.X - _resizeArrowSize.X, Location.Y + Size.Y - _resizeArrowSize.Y);
                spriteBatch.Draw(_resizeArrowTexture, arrow, Color.White);
            }
        }

        protected override void OnResized(ResizedEventArgs e)
        {
            Point size = Size;

            size.X = (size.X < _minSize.X) ? _minSize.X : size.X;
            size.Y = (size.Y < _minSize.Y) ? _minSize.Y : size.Y;

            if (size != Size)
                Size = size;

            UpdateTilePositions();

            base.OnResized(e);
        }

        private bool InResizeArrowBounds(Point pos)
        {
            return ((pos.X >= (Location.X + Size.X - _resizeArrowSize.X)) && ((Location.X + Size.X) >= pos.X) &&
                    (pos.Y >= (Location.Y + Size.Y - _resizeArrowSize.Y)) && ((Location.Y + Size.Y) >= pos.Y));
        }

        protected override void OnLeftMouseButtonPressed(MouseEventArgs e)
        {
            bool inResizeBounds = InResizeArrowBounds(Input.Mouse.Position);
            if (inResizeBounds || EnableMoving)
                _dragStart = Input.Mouse.Position;

            _dragMoving = EnableMoving && !inResizeBounds;
            _dragResizing = inResizeBounds;

            base.OnLeftMouseButtonPressed(e);
        }
        protected override void OnLeftMouseButtonReleased(MouseEventArgs e)
        {
            if (_dragMoving)
            {
                _dragMoving = false;
                Module.SquadInterfaceLocation.Value = this.Location;
            }

            if (_dragResizing)
            {
                _dragResizing = false;
                Module.SquadInterfaceSize.Value = this.Size;
            }

            base.OnLeftMouseButtonPressed(e);
        }

        protected override void OnMouseEntered(MouseEventArgs e)
        {
            _paintResizeArrow = true;
            _activeBackgroundColor = _hoverColor;

            base.OnMouseEntered(e);
        }

        protected override void OnMouseLeft(MouseEventArgs e)
        {
            _paintResizeArrow = false;
            _activeBackgroundColor = _bgColor;

            base.OnMouseLeft(e);
        }

        public override void UpdateContainer(GameTime gameTime)
        {
            if (_dragMoving || _dragResizing)
            {
                Point windowSize = GameService.Graphics.SpriteScreen.Size;
                Point pointer = Input.Mouse.Position;

                if (_dragMoving)
                {
                    if (pointer.X > 0 && pointer.Y > 0 &&
                        pointer.X < windowSize.X && pointer.Y < windowSize.Y)
                    {
                        if (_dragStart != pointer)
                        {
                            var nOffset = pointer - _dragStart;
                            Location += nOffset;
                            UpdateTilePositions();
                        }
                    }
                    else
                    {
                        _dragMoving = false;
                        Module.SquadInterfaceLocation.Value = this.Location;
                    }
                }
                else if (_dragResizing)
                {
                    if (pointer.X > 0 && pointer.Y > 0 &&
                        pointer.X < windowSize.X && pointer.Y < windowSize.Y)
                    {
                        var nOffset = pointer - _dragStart;

                        if (nOffset.X > 0 && pointer.X < (Location.X + Size.X))
                            nOffset.X = 0;
                        if (nOffset.Y > 0 && pointer.Y < (Location.Y + Size.Y))
                            nOffset.Y = 0;

                        if (nOffset.X < 0 && pointer.X > (Location.X + Size.X))
                            nOffset.X = 0;
                        if (nOffset.Y < 0 && pointer.Y > (Location.Y + Size.Y))
                            nOffset.Y = 0;

                        if ((Size.X + nOffset.X >= _minSize.X) && (Size.Y + nOffset.Y >= _minSize.Y))
                        {
                            Size += nOffset;
                            _activeBackgroundColor = _hoverColor;
                            UpdateTilePositions();
                        }
                    }
                    else
                    {
                        _dragResizing = false;
                        Module.SquadInterfaceSize.Value = this.Size;
                    }
                }

                _dragStart = Input.Mouse.Position;
            }
        }

        public void Clear()
        {
            List<SquadInterfaceTile> tiles = _tiles.ToList();
            for (int i = 0; i < tiles.Count; ++i)
            {
                tiles[i].Parent = null;
                tiles[i].Dispose();
            }

            List<SquadInterfaceSubgroup> subgroups = _subgroups.ToList();
            for (int i = 0; i < subgroups.Count; ++i)
            {
                subgroups[i].Parent = null;
                subgroups[i].Dispose();
            }

            _tiles.Clear();
            _subgroups.Clear();
        }

        public void Add(Player player)
        {
            if (_tiles.Find(t => t.Player.AccountName == player.AccountName) != null) return;

            Character character = player.CurrentCharacter;
            var icon = (character != null) ? _iconsManager.GetSpecializationIcon(character.Profession, character.Specialization) : null;

            SquadInterfaceSubgroup subgroup = _subgroups.Find(s => s.Number == player.Subgroup);
            if (subgroup == null)
            {
                Color color = (player.Subgroup % 2 == 0) ? _subgroupColor1 : _subgroupColor2;
                subgroup = new SquadInterfaceSubgroup(player.Subgroup, color, _subgroupHoverColor) { Parent = this };
                _subgroups.Add(subgroup);
            }

            SquadInterfaceTile tile = new SquadInterfaceTile(player, player.AccountName == SelfAccountName, _tileLoadedTexture, _tileTextThreshold, _roles)
            {
                Icon = icon,
                Parent = subgroup
            };

            _tiles.Add(tile);
            UpdateTilePositions();
        }

        public void Remove(string accountName)
        {
            var index = _tiles.FindIndex(t => t.Player.AccountName == accountName);
            if (index != -1)
            {
                _tiles[index].Parent = null;

                SquadInterfaceSubgroup subgroup = _subgroups.Find(s => s.Number == _tiles[index].Player.Subgroup);
                if (subgroup.Children.Count == 0)
                {
                    subgroup.Parent = null;
                    _subgroups.Remove(subgroup);
                }

                _tiles.RemoveAt(index);
                UpdateTilePositions();
            }
        }

        public void Update(Player player)
        {
            var index = _tiles.FindIndex(t => t.Player.AccountName == player.AccountName);
            if (index != -1)
            {
                Character character = player.CurrentCharacter;
                var icon = (character != null) ? _iconsManager.GetSpecializationIcon(character.Profession, character.Specialization) : null;

                SquadInterfaceSubgroup currentSubgroup = _tiles[index].Parent as SquadInterfaceSubgroup;

                if (currentSubgroup != null)
                {
                    if (currentSubgroup.Number != player.Subgroup)
                    {
                        SquadInterfaceSubgroup newSubgroup = _subgroups.Find(s2 => s2.Number == player.Subgroup);

                        if (newSubgroup == null)
                        {
                            Color color = (player.Subgroup % 2 == 0) ? _subgroupColor1 : _subgroupColor2;
                            newSubgroup = new SquadInterfaceSubgroup(player.Subgroup, color, _subgroupHoverColor) { Parent = this };
                            _subgroups.Add(newSubgroup);
                        }

                        _tiles[index].Parent = newSubgroup;

                        if (currentSubgroup.Children.Count == 0)
                        {
                            currentSubgroup.Parent = null;
                            _subgroups.Remove(currentSubgroup);
                        }
                    }
                }

                _tiles[index].Player = player;
                _tiles[index].Icon = icon;

                UpdateTilePositions();
            }
        }

        private void GenerateSizes()
        {
            if (_children.Count == 0) return;

            List<SquadInterfaceSubgroup> subgroups = _children.OfType<SquadInterfaceSubgroup>().ToList();
            if (subgroups.Count == 0) return;

            uint maxSubCount = 1;

            for (int i = 0; i < subgroups.Count; i++)
            {
                if (subgroups[i].Children.Count > maxSubCount)
                    maxSubCount = (uint)subgroups[i].Children.Count;
            }

            uint tilesPerRow = (maxSubCount > _tilesSplitPoint) ? _tilesPerRow : maxSubCount;

            int availX = (int)(Size.X - (_tileSpacing * 10)) - (int)(_tileSpacing * (tilesPerRow - 1));
            int tileX = (int)(availX / tilesPerRow);

            if (maxSubCount > _tilesSplitPoint)
            {
                if (tileX > _tileMaxSize.X)
                {
                    if (maxSubCount > _tilesPerRow)
                    {
                        tilesPerRow = ++_tilesPerRow;
                        availX = (int)(Size.X - (_tileSpacing * 10)) - (int)(_tileSpacing * (tilesPerRow - 1));
                        tileX = (int)(availX / tilesPerRow);
                    }
                }
                else
                {
                    int underMax = (int)(_tileMaxSize.X * (_tilesPerRow - 1));
                    int current = (int)(tileX * _tilesPerRow);

                    if (underMax > current && _tilesPerRow > _tilesSplitPoint)
                    {
                        tilesPerRow = --_tilesPerRow;
                        availX = (int)(Size.X - (_tileSpacing * 10)) - (int)(_tileSpacing * (tilesPerRow - 1));
                        tileX = (int)(availX / tilesPerRow);
                    }
                }
            }

            if (tileX > _tileMaxSize.X)
                tileX = _tileMaxSize.X;

            if (tileX < _tileMinSize.X)
                tileX = _tileMinSize.X;
            _tileSize.X = tileX;

            double tileXP = ((double)(tileX - _tileMinSize.X) / (double)(_tileMaxSize.X - _tileMinSize.X));
            _tileSize.Y = _tileMinSize.Y + (int)(tileXP * (double)(_tileMaxSize.Y - _tileMinSize.Y));

            _minSize.X = (int)(_tileSpacing * 10) + (int)(_tileMinSize.X * tilesPerRow) + (int)(_tileSpacing * (tilesPerRow - 1));
        }

        private void UpdateTilePositions()
        {
            if (_children.Count == 0) return;

            List<SquadInterfaceSubgroup> subgroups = _children.OfType<SquadInterfaceSubgroup>().OrderBy(o => o.Number).ToList();
            if (subgroups.Count == 0) return;

            GenerateSizes();

            Point point = new Point(0, 0);

            int textWidth = (int)(_tileSpacing * 10);
            int xpadding = (int)(_tileSpacing * 2);
            int ypadding = (int)(_tileSpacing * 2);

            for (int i = 0; i < subgroups.Count; i++)
            {
                Color color = (i % 2 == 0) ? _subgroupColor1 : _subgroupColor2;
                subgroups[i].ForegroundColor = color;
                subgroups[i].UpdateTileSizes(Size.X, textWidth - xpadding, ypadding, _tileSize, _tilesPerRow, _tileSpacing);
                subgroups[i].Location = point;
                point.Y += subgroups[i].Size.Y;
            }
        }
    }
}
