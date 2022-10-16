using Microsoft.Xna.Framework;
using System.Linq;

namespace Torlando.SquadTracker.Helpers
{
    internal static class RandomColorGenerator
    {
        public static Color Generate(string name)
        {
            var hash = 0;
            for (var i = 0; i < name.Length; ++i)
                hash = ((int)name.ElementAt(i)) + ((hash << 5) - hash);

            var r = (byte)((hash >> (0 * 8)) & 0xFF);
            var g = (byte)((hash >> (1 * 8)) & 0xFF);
            var b = (byte)((hash >> (2 * 8)) & 0xFF);

            var color = new Color((int)r, (int)g, (int)b, 255);
            return color;
        }
    }
}
