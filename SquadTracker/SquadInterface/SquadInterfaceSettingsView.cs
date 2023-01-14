using System;
using System.Collections.Generic;
using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Torlando.SquadTracker.SquadInterface
{
    public class SquadInterfaceSettingsView : Panel
    {
        private readonly Label _name;
        private readonly List<ColorSettingsEntry> _colorEntries;

        public Color HoverColor;
        public Color BgColor;
        public Color BorderColor;

        private int _offset;

        public SquadInterfaceSettingsView(string name, Color hoverColor, Color bgColor, Color borderColor) : base()
        {
            HoverColor = hoverColor;
            BgColor = bgColor;
            BorderColor = borderColor;
            CanScroll = false;
            
            _colorEntries = new List<ColorSettingsEntry>();
            
            _name = new Label()
            {
                Parent = this,
                Text = name + ":",
                ShowShadow = true,
                StrokeText = true,
                ShadowColor = Color.Black,
                TextColor = Color.White,
                Location = new Point(10, 10),
                AutoSizeWidth = true,
                AutoSizeHeight = true
            };

            CalculateSize();
        }

        protected override void DisposeControl()
        {
            _name.Parent = null;
            _name.Dispose();
            
            foreach (var entry in _colorEntries)
            {
                entry.Parent = null;
                entry.Dispose();
            }

            _colorEntries.Clear();
            
            base.DisposeControl();
        }
        
        private void CalculateSize()
        {
            var size = new Point(_name.Size.X, _name.Size.Y);
            
            foreach (var entry in _colorEntries)
            {
                if (entry.Size.X > size.X)
                    size.X = entry.Size.X;

                size.Y += entry.Size.Y + 10;
            }

            size.X += 20;
            size.Y += 20;
            
            Size = size;
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
        
        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, bounds, MouseOver ? HoverColor : BgColor);
            PaintBorder(spriteBatch, bounds, BorderColor, 1);
        }

        public IEnumerable<ColorSettingsEntry> GetColorEntries()
        {
            return _colorEntries;
        }

        public void AddColorEntry(string name, Color defaultColor, Action<Color> onColorChange)
        {
            var colorSetting = new ColorSettingsEntry(name, defaultColor, onColorChange)
            {
                Parent = this,
                Location = new Point(0,0)
            };
            _colorEntries.Add(colorSetting);

            if (colorSetting.Offset > _offset)
                _offset = colorSetting.Offset;

            ApplyOffsets();
            CalculateSize();
            CalculateTopDownPositions();
        }

        private void ApplyOffsets()
        {
            foreach (var entry in _colorEntries)
                entry.SetOffset(_offset);
        }

        private void CalculateTopDownPositions()
        {
            var y = _name.Size.Y + 20;
            foreach (var entry in _colorEntries)
            {
                var pos = entry.Location;
                pos.Y = y;
                pos.X = 10;
                y += entry.Size.Y + 10;
                entry.Location = pos;
            }
        }
    }
}