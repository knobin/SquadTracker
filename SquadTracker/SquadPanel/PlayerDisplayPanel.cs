using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

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
            List<Control> list = _children.ToList();
            _children = new ControlCollection<Control>();

            for (int i = 0; i < list.Count; ++i)
            {
                list[i].Parent = null;
            }
        }
    }
}
