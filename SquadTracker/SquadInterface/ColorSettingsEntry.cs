using System;
using System.Linq;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;

namespace Torlando.SquadTracker.SquadInterface
{
    public class HexEntry : Container
    {
        private readonly Label _name;
        private readonly TextBox _textBox;
        
        private readonly Action<byte> _onValidNumeric;

        public HexEntry(string entry, byte hex, Action<byte> onValidValue) : base()
        {
            _onValidNumeric = onValidValue;
            _name = new Label()
            {
                Parent = this,
                Text = entry + ":",
                ShowShadow = true,
                StrokeText = true,
                ShadowColor = Color.Black,
                TextColor = Color.White,
                Location = new Point(0, 0),
                AutoSizeWidth = true
            };
            
            _textBox = new TextBox()
            {
                Parent = this,
                Text = hex.ToString(),
                Location = new Point(_name.Size.X + 2, 0),
                Size = new Point(42, _name.Size.Y + 2)
            };
            
            Size = new Point(_name.Size.X + _textBox.Size.X, _textBox.Size.Y);

            _textBox.TextChanged += TextChanged;

            _textBox.BasicTooltipText = "Accepted values [0, 255]";
        }
        
        protected override void DisposeControl()
        {
            _textBox.TextChanged -= TextChanged;
            
            _name.Parent = null;
            _name.Dispose();
            
            _textBox.Parent = null;
            _textBox.Dispose();
            
            base.DisposeControl();
        }

        private static int ToNumeric(string value)
        {
            if (int.TryParse(value, out var n))
                return n;
            return -1;
        }
        
        private static bool ValidNumeric(int value)
        {
            return (value >= 0 && value < 256);
        }

        private void TextChanged(object sender, System.EventArgs e)
        {
            var num = ToNumeric(_textBox.Text);
            if (num == -1) return;
            if (!ValidNumeric(num)) return;
            
            _onValidNumeric?.Invoke((byte)num);
        }
    }
    
    
    public class ColorSettingsEntry : Container
    {
        private readonly Label _nameLabel;
        public string Name { get; }
        public Color Color { get; private set; }
        public int Offset { get; private set; }

        private readonly HexEntry _r;
        private readonly HexEntry _g;
        private readonly HexEntry _b;
        private readonly HexEntry _a;

        private readonly Action<Color> _onColorChanged;

        public ColorSettingsEntry(string name, Color color, Action<Color> onColorChanged) : base()
        {
            Color = color;
            _onColorChanged = onColorChanged;
            Name = name;

            _nameLabel = new Label()
            {
                Parent = this,
                Text = name + ": ",
                ShowShadow = true,
                StrokeText = true,
                ShadowColor = Color.Black,
                TextColor = Color.White,
                Location = new Point(0, 0),
                AutoSizeWidth = true
            };

            const int offset = 10;
            var pos = new Point(_nameLabel.Size.X + offset, 0);
            Offset = pos.X;
            _r = new HexEntry("r", color.R, (value) => ColorChanged('r', value))
            {
                Parent = this,
                Location = pos
            };
            pos.X += _r.Size.X + offset;
            _g = new HexEntry("g", color.G, (value) => ColorChanged('g', value))
            {
                Parent = this,
                Location = pos
            };
            pos.X += _g.Size.X + offset;
            _b = new HexEntry("b", color.B, (value) => ColorChanged('b', value))
            {
                Parent = this,
                Location = pos
            };
            pos.X += _b.Size.X + offset;
            _a = new HexEntry("a", color.A, (value) => ColorChanged('a', value))
            {
                Parent = this,
                Location = pos
            };
            pos.X += _a.Size.X;
            
            Size = new Point(pos.X, _nameLabel.Size.Y);
        }

        public void SetOffset(int newOffset)
        {
            Offset = newOffset;
            const int offset = 10;
            var pos = new Point(newOffset, 0);
            
            _r.Location = pos;
            pos.X += _r.Size.X + offset;
            
            _g.Location = pos;
            pos.X += _g.Size.X + offset;
            
            _b.Location = pos;
            pos.X += _b.Size.X + offset;
            
            _a.Location = pos;
            pos.X += _a.Size.X;
            
            Size = new Point(pos.X, _nameLabel.Size.Y);
        }

        private void ColorChanged(char entry, byte value)
        {
            var color = Color;
            
            switch (entry)
            {
                case 'r':
                    color.R = value;
                    break;
                case 'g':
                    color.G = value;
                    break;
                case 'b':
                    color.B = value;
                    break;
                case 'a':
                    color.A = value;
                    break;
            }

            if (color == Color) return;
            Color = color;
            _onColorChanged?.Invoke(color);
        }

        protected override void DisposeControl()
        {
            _nameLabel.Parent = null;
            _nameLabel.Dispose();
            
            _r.Parent = null;
            _r.Dispose();
            
            _g.Parent = null;
            _g.Dispose();
            
            _b.Parent = null;
            _b.Dispose();
            
            _a.Parent = null;
            _a.Dispose();

            base.DisposeControl();
        }
    }
}