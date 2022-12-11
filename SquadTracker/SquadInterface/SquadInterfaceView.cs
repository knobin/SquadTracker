using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.IdentityModel.Tokens;
using MonoGame.Extended.BitmapFonts;
using Torlando.SquadTracker.RolesScreen;

namespace Torlando.SquadTracker.SquadInterface
{
    internal class SquadInterfaceView : Container
    {
        public SquadInterfaceView(PlayerIconsManager playerIconsManager, ICollection<Role> roles, AsyncTexture2D squad)
        {
            Location = new Point(0, 0);
            Size = new Point(600, 600);
            Visible = true;
            Padding = Thickness.Zero;

            _iconsManager = playerIconsManager;
            _roles = roles;
            _tileLoadedTexture = squad;
            GenerateResizeArrow(_resizeArrowSize.X, _resizeArrowSize.Y, Color.LightGray);
            
            _errorMessage = new Label() 
            {
                Font = GameService.Content.DefaultFont18, 
                Parent = this, 
                Visible = false,
                Location = new Point(10, 10)
            };
            
            _font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, FontSize, ContentService.FontStyle.Regular);
            
            _searchbar = new TextBox()
            {
                Parent = this,
                Size = new Point(Size.X / 2, (int)(Size.Y * 0.8)),
                Location = new Point(0, 0),
                PlaceholderText = "Search",
                HideBackground = true
            };
            
            ResizeBar();
            
            _searchbar.TextChanged += OnSearchInput;
        }

        public bool EnableMoving = false;
        private Point _dragStart = new Point(0, 0);
        private bool _dragResizing;
        private bool _dragMoving;

        private readonly ICollection<Role> _roles;

        private readonly Color _bgColor = new Color(0, 0, 0, 25);
        private readonly Color _barColor = new Color(18, 20, 22, 125);
        private readonly Color _hoverColor = new Color(5, 5, 5, 75);
        private readonly Color _borderColor = new Color(6, 7, 8, 180);
        private readonly Color _subgroupHoverColor = new Color(0, 0, 0, 100);
        private readonly Color _subgroupColor1 = new Color(20, 20, 20, 50);
        private readonly Color _subgroupColor2 = new Color(30, 30, 30, 50);
        private readonly Color _searchBackgroundColor = new Color(20, 20, 20, 50);
        private readonly Color _searchFocusedColor = new Color(15, 15, 15, 225);
        private readonly Color _searchHoverColor = new Color(25, 25, 25, 175);

        private readonly AsyncTexture2D _tileLoadedTexture;
        private readonly PlayerIconsManager _iconsManager;

        private Point _tileSize = new Point(87, 52);
        private readonly Point _tileMaxSize = new Point(87, 52);
        private readonly Point _tileMinSize = new Point(28, 28);
        private const int TileTextThreshold = 35;
        private const uint TilesSplitPoint = 5;
        private const uint TileSpacing = 5;
        private uint _tilesPerRow = 5;

        private Point _minSize = new Point(40, 80);
        private uint _barHeight;
        private const uint BarHeightBig = 42;
        private const uint BarHeightSmall = 34;

        private readonly List<SquadInterfaceTile> _tiles = new List<SquadInterfaceTile>();
        private readonly List<SquadInterfaceSubgroup> _subgroups = new List<SquadInterfaceSubgroup>();

        private AsyncTexture2D _resizeArrowTexture;
        private readonly Point _resizeArrowSize = new Point(20, 20);
        
        private readonly Label _errorMessage;
        
        private readonly BitmapFont _font;
        private const ContentService.FontSize FontSize = ContentService.FontSize.Size18;
        
        private readonly TextBox _searchbar;
        private string _filterInput = "";
        private string _filterMatchCountStr = "";
        private int _filterMatchCountStrWidth = 0;
        private int _filterMatchCountStrHeight = 0;

        private void GenerateResizeArrow(int width, int height, Color color)
        {
            var texture = new Texture2D(GameService.Graphics.GraphicsDevice, width, height);

            var data = new Color[width * height];
            var x = width - 1;
            for (var i = 0; i < data.Length; ++i)
            {
                var col = i % width;
                if (col < x) continue;
                
                if (col == x)
                    x--;
                data[i] = color;
                data[i].A = 25;
            }

            texture.SetData(data);
            _resizeArrowTexture = texture;
        }

        public void ShowErrorMessage(string message)
        {
            _errorMessage.Text = message;
            var strSize = _errorMessage.Font.MeasureString(_errorMessage.Text);
            var strWidth = (int)strSize.Width + 10;
            var strHeight = (int)strSize.Height + 10;
            _errorMessage.Size = new Point(strWidth, strHeight);

            _errorMessage.Visible = true;
        }

        public void HideErrorMessage()
        {
            _errorMessage.Visible = false;
            _errorMessage.Text = "";
        }

        public void OnRoleCollectionUpdate()
        {
            var tiles = _tiles.ToList();
            foreach (var tile in tiles)
                tile.UpdateContextMenu();
        }

        private void OnSearchInput(object sender, System.EventArgs e)
        {
            Filter(_searchbar.Text);
        }
        
        private void Filter(string searchText)
        {
            var searchInput = searchText.ToLowerInvariant();

            if (searchInput.Length > 0)
            {
                List<SquadInterfaceTile> matches = null;
                
                var input = searchInput.TrimStart(' ');
                if (input[0] != '/')
                    matches = GeneralSearch(ref input);
                else
                {
                    input = input.TrimStart('/');
                    input = input.TrimStart(' ');
                    if (input.Length > 0)
                    {
                        var cmd = input[0];
                        input = input.TrimStart(cmd);
                        input = input.TrimStart(' ');
                        if (input.Length > 0)
                            matches = TargetedSearch(cmd, ref input);
                        else
                        {
                            foreach (var t in _tiles)
                                t.Opacity = 1.0f;
                            _filterInput = "";
                            _filterMatchCountStr = "";
                        }
                            
                    }
                    
                }

                if (matches != null)
                {
                    // Do stuff with tiles here.
                    foreach (var t in _tiles)
                        t.Opacity = matches.Contains(t) ? 1.0f : 0.4f;
                    _filterInput = searchInput;
                    _filterMatchCountStr = matches.Count.ToString() + "/";
                    var strSize = _font.MeasureString(_filterMatchCountStr);
                    _filterMatchCountStrWidth = (int)strSize.Width;
                    _filterMatchCountStrHeight = (int)strSize.Height;
                }
            }
            else
            {
                // Reset tiles here.
                foreach (var t in _tiles)
                    t.Opacity = 1.0f;
                _filterInput = "";
                _filterMatchCountStr = "";
            }
        }

        private void TilesModifiedApplyFilter()
        {
            Filter(_filterInput);
        }
        
        private List<SquadInterfaceTile> GeneralSearch(ref string input)
        {
            var matches = new List<SquadInterfaceTile>();

            foreach (var t in _tiles)
            {
                var match = MatchAccountName(t.Player, ref input) |
                            MatchCharacterName(t.Player, ref input) |
                            MatchBoon(t.Player, ref input);
                if (match > 0)
                    matches.Add(t);
            }

            return matches;
        }

        private List<SquadInterfaceTile> TargetedSearch(char c, ref string input)
        {
            var matches = new List<SquadInterfaceTile>();
            var cmd = char.ToLower(c);
            
            foreach (var t in _tiles)
            {
                var match = cmd switch
                {
                    'a' => MatchAccountName(t.Player, ref input),
                    'c' => MatchCharacterName(t.Player, ref input),
                    'b' => MatchBoon(t.Player, ref input),
                    _ => 0
                };
                if (match > 0)
                    matches.Add(t);
            }
            
            return matches;
        }
        
        private static int MatchAccountName(Player player, ref string input)
        {
            return player.AccountName.ToLowerInvariant().Contains(input) ? input.Length : 0;
        }
        
        private static int MatchCharacterName(Player player, ref string input)
        {
            if (player.CurrentCharacter == null)
                return 0;
            
            return player.CurrentCharacter.Name.ToLowerInvariant().Contains(input) ? input.Length : 0;
        }
        
        private static int MatchBoon(Player player, ref string input)
        {
            var assignedRoles = player.Roles.OrderBy(role => role.Name.ToLowerInvariant()).ToList();

            foreach (var t in assignedRoles)
                if (t.Name.ToLowerInvariant().Contains(input))
                    return input.Length;

            return 0;
        }
        
        private void PaintBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color, int thickness)
        {
            var rect = new Rectangle(bounds.Location, Point.Zero);

            // X axis.
            rect.Width = bounds.Width;
            rect.Height = thickness;
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, rect, color);
            rect.Y = bounds.Location.Y + bounds.Height - thickness;
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, rect, color);

            // Y axis.
            rect.Width = thickness;
            rect.Height = bounds.Height - (2 * thickness);
            rect.Y = bounds.Location.Y + thickness;
            rect.X = bounds.Location.X;
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, rect, color);
            rect.X = bounds.Location.X + bounds.Width - thickness;
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, rect, color);
        }

        private void PaintBar(SpriteBatch spriteBatch, Rectangle bounds)
        {
            bounds.Height -= 1;
            
            // Background.
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, bounds, _barColor);
            
            // Squad size.
            var strSize = _font.MeasureString(_tiles.Count.ToString());
            const int textWidth = (int)(TileSpacing * 10);
            const int paddingX = (int)(TileSpacing * 2);
            var rect = new Rectangle(bounds.Location, new Point((int)strSize.Width, (int)strSize.Height));
            rect.X = textWidth - paddingX;
            rect.Y = (bounds.Height - (int)strSize.Height) / 2;
            spriteBatch.DrawStringOnCtrl(this, _tiles.Count.ToString(), _font, rect, Color.White);
            
            // Search.
            if (!_filterMatchCountStr.IsNullOrEmpty())
            {
                rect.X -= _filterMatchCountStrWidth;
                rect.Y = (bounds.Height - _filterMatchCountStrHeight) / 2;
                spriteBatch.DrawStringOnCtrl(this, _filterMatchCountStr, _font, rect, Color.White);
            }
            if (_searchbar.Focused)
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(_searchbar.Location, _searchbar.Size), _searchFocusedColor);
            else if (_searchbar.MouseOver)
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(_searchbar.Location, _searchbar.Size), _searchHoverColor);
            else
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(_searchbar.Location, _searchbar.Size), _searchBackgroundColor);
            var inputBounds = new Rectangle(_searchbar.Location, _searchbar.Size);
            PaintBorder(spriteBatch, inputBounds, _borderColor, 1);

            // Line.
            var size = new Point(Size.X, 1);
            var pos = new Point(0, bounds.Location.Y + bounds.Height);
            var lineBounds = new Rectangle(pos, size);
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, lineBounds, _borderColor);
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, bounds, MouseOver ? _hoverColor : _bgColor);

            var barSize = new Point(Size.X, (int)_barHeight);
            var barBounds = new Rectangle(Point.Zero, barSize);
            PaintBar(spriteBatch, barBounds);

            if (!MouseOver) return;
            var arrow = new Vector2(Location.X + Size.X - _resizeArrowSize.X, Location.Y + Size.Y - _resizeArrowSize.Y);
            spriteBatch.Draw(_resizeArrowTexture, arrow, Color.White);
        }

        public override void PaintAfterChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            PaintBorder(spriteBatch, bounds, _borderColor, 1);
        }

        public void OnBoonPrioritizeChange()
        {
            UpdateTilePositions();
        }

        protected override void OnResized(ResizedEventArgs e)
        {
            var size = Size;

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

        public void TileColorPreference(bool preference)
        {
            var tiles = _tiles.ToList();
            foreach (var tile in tiles)
                tile.DisplayInnerBorderColor = preference;
            tiles.Clear();
        }

        protected override void OnLeftMouseButtonPressed(MouseEventArgs e)
        {
            var inResizeBounds = InResizeArrowBounds(Input.Mouse.Position);
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

        public override void UpdateContainer(GameTime gameTime)
        {
            if (!(_dragMoving || _dragResizing))
                return;
            
            var windowSize = GameService.Graphics.SpriteScreen.Size;
            var pointer = Input.Mouse.Position;

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

        public void Clear()
        {
            var tiles = _tiles.ToList();
            for (var i = 0; i < tiles.Count; ++i)
            {
                tiles[i].Parent = null;
                tiles[i].Dispose();
                tiles[i] = null;
            }

            tiles.Clear();
            _tiles.Clear();

            var subgroups = _subgroups.ToList();
            for (var i = 0; i < subgroups.Count; ++i)
            {
                subgroups[i].Parent = null;
                subgroups[i].Dispose();
                subgroups[i] = null;
            }
            
            subgroups.Clear();
            _subgroups.Clear();

            _filterInput = "";
        }

        public void Add(Player player)
        {
            if (_tiles.Find(t => t.Player.AccountName == player.AccountName) != null) return;

            var character = player.CurrentCharacter;
            var icon = (character != null) ? _iconsManager.GetSpecializationIcon(character.Profession, character.Specialization) : null;

            var subgroup = _subgroups.Find(s => s.Number == player.Subgroup);
            if (subgroup == null)
            {
                var color = (player.Subgroup % 2 == 0) ? _subgroupColor1 : _subgroupColor2;
                subgroup = new SquadInterfaceSubgroup(player.Subgroup, color, _subgroupHoverColor, _roles) { Parent = this };
                _subgroups.Add(subgroup);
            }

            var tile = new SquadInterfaceTile(player, _tileLoadedTexture, TileTextThreshold, _roles)
            {
                Icon = icon,
                Parent = subgroup
            };

            _tiles.Add(tile);
            UpdateTilePositions();
            TilesModifiedApplyFilter();
        }

        public void Remove(string accountName)
        {
            var index = _tiles.FindIndex(t => t.Player.AccountName == accountName);
            if (index == -1) return;
        
            _tiles[index].Parent = null;

            var subgroup = _subgroups.Find(s => s.Number == _tiles[index].Player.Subgroup);
            if (subgroup.Children.Count == 0)
            {
                subgroup.Parent = null;
                subgroup.Dispose();
                _subgroups.Remove(subgroup);
            }

            _tiles.RemoveAt(index);
            UpdateTilePositions();
            TilesModifiedApplyFilter();
        }

        public void Update(Player player)
        {
            var index = _tiles.FindIndex(t => t.Player.AccountName == player.AccountName);
            if (index == -1) return;
            
            var character = player.CurrentCharacter;
            var icon = (character != null) ? _iconsManager.GetSpecializationIcon(character.Profession, character.Specialization) : null;

            if (_tiles[index].Parent is SquadInterfaceSubgroup currentSubgroup)
            {
                if (currentSubgroup.Number != player.Subgroup)
                {
                    var newSubgroup = _subgroups.Find(s2 => s2.Number == player.Subgroup);

                    if (newSubgroup == null)
                    {
                        var color = (player.Subgroup % 2 == 0) ? _subgroupColor1 : _subgroupColor2;
                        newSubgroup = new SquadInterfaceSubgroup(player.Subgroup, color, _subgroupHoverColor, _roles) { Parent = this };
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

            // _tiles[index].Player = player;
            _tiles[index].Icon = icon;
            _tiles[index].UpdateInformation();

            UpdateTilePositions();
            TilesModifiedApplyFilter();
        }

        private void GenerateSizes()
        {
            if (_children.Count == 0) return;

            var subgroups = _children.OfType<SquadInterfaceSubgroup>().ToList();
            if (subgroups.Count == 0) return;

            uint maxSubCount = 1;

            foreach (var t in subgroups.Where(t => t.Children.Count > maxSubCount))
            {
                maxSubCount = (uint)t.Children.Count;
            }

            var tilesPerRow = (maxSubCount > TilesSplitPoint) ? _tilesPerRow : maxSubCount;

            var availX = (int)(Size.X - (TileSpacing * 10)) - (int)(TileSpacing * (tilesPerRow - 1));
            var tileX = (int)(availX / tilesPerRow);

            if (maxSubCount > TilesSplitPoint)
            {
                if (tileX > _tileMaxSize.X)
                {
                    if (maxSubCount > _tilesPerRow)
                    {
                        tilesPerRow = ++_tilesPerRow;
                        availX = (int)(Size.X - (TileSpacing * 10)) - (int)(TileSpacing * (tilesPerRow - 1));
                        tileX = (int)(availX / tilesPerRow);
                    }
                }
                else
                {
                    var underMax = (int)(_tileMaxSize.X * (_tilesPerRow - 1));
                    var current = (int)(tileX * _tilesPerRow);

                    if (underMax > current && _tilesPerRow > TilesSplitPoint)
                    {
                        tilesPerRow = --_tilesPerRow;
                        availX = (int)(Size.X - (TileSpacing * 10)) - (int)(TileSpacing * (tilesPerRow - 1));
                        tileX = (int)(availX / tilesPerRow);
                    }
                }
            }

            if (tileX > _tileMaxSize.X)
                tileX = _tileMaxSize.X;

            if (tileX < _tileMinSize.X)
                tileX = _tileMinSize.X;
            _tileSize.X = tileX;

            var tileXp = ((tileX - _tileMinSize.X) / (double)(_tileMaxSize.X - _tileMinSize.X));
            _tileSize.Y = _tileMinSize.Y + (int)(tileXp * (_tileMaxSize.Y - _tileMinSize.Y));

            _minSize.X = (int)(TileSpacing * 10) + (int)(_tileMinSize.X * tilesPerRow) + (int)(TileSpacing * (tilesPerRow - 1));

            subgroups.Clear();
        }

        private void ResizeBar()
        {
            _barHeight = _tileSize.Y > TileTextThreshold ? BarHeightBig : BarHeightSmall;
            
            const int textWidth = (int)(TileSpacing * 10);
            const int paddingX = (int)(TileSpacing * 2);
            var strSize = _font.MeasureString(_tiles.Count.ToString());
            const int offset = (int)(TileSpacing * 2);
            var x = textWidth - paddingX + (int)strSize.Width + offset;
            var h = (int)(_barHeight * 0.75);

            var endPosX = textWidth - paddingX + (_tileSize.X * (int)_tilesPerRow) +
                          (int)(TileSpacing * (_tilesPerRow - 1));

           if (endPosX >= Size.X || _tiles.Count < _tilesPerRow)
                endPosX = Size.X - offset;

            _searchbar.Location = new Point(x, (int)((_barHeight - h)/2));
            _searchbar.Size = new Point(endPosX - x, h);
        }

        private void UpdateTilePositions()
        {
            if (_children.Count == 0) return;

            var subgroups = _children.OfType<SquadInterfaceSubgroup>().OrderBy(o => o.Number).ToList();
            if (subgroups.Count == 0) return;
            
            GenerateSizes();
            ResizeBar();
            
            var point = new Point(0, (int)_barHeight);

            const int textWidth = (int)(TileSpacing * 10);
            const int paddingX = (int)(TileSpacing * 2);
            const int paddingY = (int)(TileSpacing);

            for (var i = 0; i < subgroups.Count; i++)
            {
                subgroups[i].ForegroundColor = (i % 2 == 0) ? _subgroupColor1 : _subgroupColor2;
                subgroups[i].UpdateTileSizes(Size.X, textWidth - paddingX, paddingY, _tileSize, _tilesPerRow, TileSpacing);
                subgroups[i].Location = point;
                point.Y += subgroups[i].Size.Y;
            }

            subgroups.Clear();
        }
    }
}
