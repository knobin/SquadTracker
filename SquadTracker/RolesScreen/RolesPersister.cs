﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Blish_HUD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Torlando.SquadTracker.Helpers;

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

    public static class RoleIconCreator
    {
        public static Texture2D GenerateIcon(string name, int width = 32, int height = 32)
        {
            var icon = new Texture2D(GameService.Graphics.GraphicsDevice, width, height);
            var data = new Color[width * height];

            var color = RandomColorGenerator.Generate(name);
            for (var i = 0; i < data.Length; ++i)
                data[i] = color;

            icon.SetData(data);
            return icon;
        }
    }
    
}