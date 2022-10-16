using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Microsoft.Xna.Framework;
using System.Linq;
using System.Collections.Generic;
using Torlando.SquadTracker.RolesScreen;
using Blish_HUD;
using Torlando.SquadTracker.SquadPanel;
using Torlando.SquadTracker.SquadInterface;

namespace Torlando.SquadTracker.SearchPanel
{
    internal class SearchPanelView : View<SearchPanelPresenter>
    {
        #region Controls
        
        private PlayerDisplayPanel _squadMembersPanel;
        private Dictionary<string, PlayerDisplay> _playerDisplays = new Dictionary<string, PlayerDisplay>();
        private readonly IEnumerable<Role> _roles;

        #endregion

        private static readonly Logger Logger = Logger.GetLogger<Module>();
        
        public delegate void RoleRemovedHandler(string accountName, Role role);
        public event RoleRemovedHandler OnRoleRemoved;

        public SearchPanelView(ICollection<Role> roles)
        {
            _roles = roles;
        }

        protected override void Build(Container buildPanel)
        {
            _squadMembersPanel = new PlayerDisplayPanel()
            {
                Parent = buildPanel,
                Location = new Point(buildPanel.ContentRegion.Left, buildPanel.ContentRegion.Top),
                Size = new Point(buildPanel.ContentRegion.Width, buildPanel.ContentRegion.Height), //
                Title = "Search result"
            };
        }
        
        protected override void Unload()
        {
            Logger.Info("Unloading SearchPanelView");
            Clear();

            _squadMembersPanel.Clear();
            _squadMembersPanel.Parent = null;
            _squadMembersPanel.Dispose();
        }

        public void Sort(Dictionary<string, int> order)
        {
            if (_squadMembersPanel.Visible)
                _squadMembersPanel.SortChildren<PlayerDisplay>(SquadPlayerSort.Compare);
        }

        public bool Exists(string accountName)
        {
            return _playerDisplays.ContainsKey(accountName);
        }

        public void DisplayPlayer(Player playerModel, AsyncTexture2D icon, IEnumerable<Role> roles)
        {
            var otherCharacters = playerModel.KnownCharacters.Except(new[] { playerModel.CurrentCharacter }).ToList();

            var playerDisplay = new PlayerDisplay(roles)
            {
                Parent = _squadMembersPanel,
                AccountName = playerModel.AccountName,
                CharacterName = (playerModel.CurrentCharacter != null) ? playerModel.CurrentCharacter.Name : "",
                Profession = (playerModel.CurrentCharacter != null) ? playerModel.CurrentCharacter.Profession : 0,
                Specialization = (playerModel.CurrentCharacter != null) ? playerModel.CurrentCharacter.Specialization : 0,
                Role = playerModel.Role,
                IsSelf = playerModel.IsSelf,
                IsInInstance = playerModel.IsInInstance,
                Subgroup = playerModel.Subgroup,
                Icon = icon,
                BasicTooltipText = OtherCharactersToString(otherCharacters)
            };

            var currentRoles = playerModel.Roles.OrderBy(role => role.Name.ToLowerInvariant());
            playerDisplay.UpdateRoles(_roles, currentRoles);

            // playerModel.OnRoleUpdated += OnRoleUpdate;
            playerDisplay.OnRoleRemove += OnDisplayRoleRemoved;

            playerDisplay.RoleDropdown.ValueChanged += (o, e) => UpdateSelectedRoles(playerModel, e, 0);

            _playerDisplays.Add(playerModel.AccountName, playerDisplay);

            _squadMembersPanel.BasicTooltipText = "";
        }

        public void RemovePlayer(string accountName)
        {
            if(!_playerDisplays.TryGetValue(accountName, out var display)) return;

            display.Parent = null;
            _playerDisplays.Remove(accountName);
        }

        public void Clear()
        {
            _squadMembersPanel.Clear();

            var keys = new List<string>(_playerDisplays.Keys);
            foreach (var key in keys)
            {
                _playerDisplays[key].Parent = null;
                _playerDisplays[key].OnRoleRemove -= OnDisplayRoleRemoved;
                _playerDisplays[key].Dispose();
                _playerDisplays[key] = null;
            }
            
            _playerDisplays.Clear();
        }

        public void OnRoleUpdate(Player player)
        {
            if (!_playerDisplays.TryGetValue(player.AccountName, out var display)) return;

            var roles = player.Roles.OrderBy(role => role.Name.ToLowerInvariant());
            display.UpdateRoles(_roles, roles);
        }

        private void OnDisplayRoleRemoved(PlayerDisplay display, Role role)
        {
            OnRoleRemoved?.Invoke(display.AccountName, role);
        }

        public void UpdatePlayer(Player playerModel, AsyncTexture2D icon, IEnumerable<Role> roles, List<string> assignedRoles)
        {
            if(!_playerDisplays.TryGetValue(playerModel.AccountName, out var display)) return;

            display.CharacterName = (playerModel.CurrentCharacter != null) ? playerModel.CurrentCharacter.Name : "";
            display.Icon = icon;
            display.Subgroup = playerModel.Subgroup;
            display.Profession = (playerModel.CurrentCharacter != null) ? playerModel.CurrentCharacter.Profession : 0;
            display.Specialization = (playerModel.CurrentCharacter != null) ? playerModel.CurrentCharacter.Specialization : 0;
            display.Role = playerModel.Role;
            display.IsSelf = playerModel.IsSelf;
            display.IsInInstance = playerModel.IsInInstance;

            var otherCharacters = playerModel.KnownCharacters.Except(new[] { playerModel.CurrentCharacter }).ToList();
            display.BasicTooltipText = OtherCharactersToString(otherCharacters);
        }

        private void UpdateSelectedRoles(Player playerModel, ValueChangedEventArgs e, int index)
        {
            var role = e.CurrentValue;
            // var accountName = playerModel.AccountName;
            // Presenter.UpdateSelectedRoles(accountName, role, index);

            var selectedRole = _roles.FirstOrDefault(r => r.Name.Equals(role));
            Logger.Info("Selected role: {}, from {}, str {}", selectedRole, index, role);
            playerModel.AddRole(selectedRole);
        }

        public void SetPlayerIcon(Player playerModel, AsyncTexture2D icon)
        {
            if (!_playerDisplays.TryGetValue(playerModel.AccountName, out var display)) return;
            display.Icon = icon;
        }

        private static string OtherCharactersToString(IReadOnlyCollection<Character> characters)
        {
            if (characters.Count == 0) return string.Empty;

            var charactersList = string.Join("\n",
                characters
                    .OrderBy(character => character.Name)
                    .Select(character =>
                        $"- {character.Name} ({Specialization.GetEliteName(character.Specialization, character.Profession)})"
                    )
            );

            return $"Other characters:\n{charactersList}";
        }
    }
}
