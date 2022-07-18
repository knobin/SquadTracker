using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System.Collections.Generic;
using System.Linq;
using Torlando.SquadTracker.SquadPanel;

namespace Torlando.SquadTracker.SquadInterface
{
    internal class SquadInterfaceSubgroup : Container
    {
        public uint Number { get; }

        private bool _isMouseOver = false;
        public Color ForegroundColor { get; set; }
        private Color _hoverColor;
        private Rectangle _numberTextRect = new Rectangle();

        public ContentService.FontSize FontSize { get; set; } = ContentService.FontSize.Size18;
        private BitmapFont _font;

        public SquadInterfaceSubgroup(uint subgroupNumber, Color bgColor, Color hoverColor)
        {
            this.Visible = true;
            this.Size = new Point(1, 1);

            this.Number = subgroupNumber;
            ForegroundColor = bgColor;
            _hoverColor = hoverColor;
        }

        private static int Compare(SquadInterfaceTile t1, SquadInterfaceTile t2)
        {
            return SquadPlayerSort.Compare(t1.Player, t2.Player);
        }

        public void UpdateTileSizes(int width, int textWidth, int ypadding, Point tileSize, uint rowCount, uint tileSpacing)
        {
            _font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, FontSize, ContentService.FontStyle.Regular);
            
            var numberTextSize = _font.MeasureString(Number.ToString());
            _numberTextRect.Width = (int)numberTextSize.Width;
            _numberTextRect.Height = (int)numberTextSize.Height;
            _numberTextRect.X = (textWidth - _numberTextRect.Width) / 2;

            Point start = new Point(textWidth, ypadding);

            List<SquadInterfaceTile> tiles = _children.OfType<SquadInterfaceTile>().ToList();
            tiles.Sort(Compare);

            Point point = new Point(start.X, start.Y);
            Point size = new Point(width, 0);
            int currentRowCount = 1;

            for (int i = 0; i < tiles.Count; i++)
            {
                tiles[i].Size = tileSize;
                tiles[i].Location = point;

                size.Y = (point.Y + tileSize.Y > size.Y) ? point.Y + tileSize.Y : size.Y;

                point.X += tileSize.X + (int)tileSpacing;

                if (++currentRowCount > rowCount)
                {
                    point.X = start.X;
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
            _font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, FontSize, ContentService.FontStyle.Regular);

            if (_isMouseOver)
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(Point.Zero, Size), _hoverColor);
            else
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(Point.Zero, Size), ForegroundColor);

            spriteBatch.DrawStringOnCtrl(this, Number.ToString(), _font, _numberTextRect, Color.White);
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
    }
}
