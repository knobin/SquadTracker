using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace Torlando.SquadTracker.RolesScreen
{
    public static class RolesPersister
    {
        private const string ROLES_FILE_NAME = "roles.json";
        public static async Task<ObservableCollection<Role>> LoadRolesFromFileSystem(string directoryPath)
        {
            var rolesFilePath = Path.Combine(directoryPath, ROLES_FILE_NAME);

            ObservableCollection<Role> roles;
            if (!File.Exists(rolesFilePath))
            {
                roles = new ObservableCollection<Role>
                {
                    new Role ("Quickness") { IconPath = @"icons\quickness.png" },
                    new Role ("Alacrity")  { IconPath = @"icons\alacrity.png" },
                    new Role ("Heal")      { IconPath = @"icons\regeneration.png" },
                    new Role ("Power DPS") { IconPath = @"icons\power.png" },
                    new Role ("Condi DPS") { IconPath = @"icons\Condition_Damage.png" },
                };

                await SaveRoles(roles, rolesFilePath);
            }
            else
            {
                var loadedRoles = await LoadRoles(rolesFilePath);
                roles = new ObservableCollection<Role>(loadedRoles);
            }

            roles.CollectionChanged += async (o, e) => await SaveRoles(roles, rolesFilePath);

            return roles;
        }

        private static async Task<IEnumerable<Role>> LoadRoles(string filePath)
        {
            var jsonHopefully = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<IEnumerable<Role>>(jsonHopefully);
        }

        private static async Task SaveRoles(IEnumerable<Role> roles, string filePath)
        {
            #if DEBUG
            var json = JsonConvert.SerializeObject(roles, Formatting.Indented);
            #else
            var json = JsonConvert.SerializeObject(roles);
            #endif
            File.WriteAllText(filePath, json);
        }
    }

    public class RoleIconCreator
    {
        public static Texture2D GenerateIcon(string name, int width = 32, int height = 32)
        {
            Texture2D icon = new Texture2D(GameService.Graphics.GraphicsDevice, width, height);
            Color[] data = new Color[width * height];
            int hash = 0;
            for (int i = 0; i < name.Length; ++i)
                hash = ((int)name.ElementAt(i)) + ((hash << 5) - hash);

            byte r = (byte)((hash >> (0 * 8)) & 0xFF);
            byte g = (byte)((hash >> (1 * 8)) & 0xFF);
            byte b = (byte)((hash >> (2 * 8)) & 0xFF);

            for (int i = 0; i < data.Length; ++i)
            {
                data[i].R = r;
                data[i].G = g;
                data[i].B = b;
                data[i].A = 255;
            }

            icon.SetData(data);
            return icon;
        }
    }
    
}