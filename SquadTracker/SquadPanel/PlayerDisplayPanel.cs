using Blish_HUD.Controls;
using Microsoft.Xna.Framework;

namespace Torlando.SquadTracker.SquadPanel
{
    internal class PlayerDisplayPanel : FlowPanel
    {
        public PlayerDisplayPanel() : base()
        {
            FlowDirection = ControlFlowDirection.LeftToRight;
            ControlPadding = new Vector2(8, 8);
            CanScroll = true;
            ShowBorder = true;
        }

        public void Clear()
        {
            var list = _children.ToList();
            _children.Clear();

            for (var i = 0; i < list.Count; ++i)
            {
                list[i].Parent = null;
                list[i].Dispose();
                list[i] = null;
            }

            // list.Clear();
        }
    }
}
