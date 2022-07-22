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
        
        private FlowPanel _squadMembersPanel;
        private Dictionary<string, PlayerDisplay> _playerDisplays = new Dictionary<string, PlayerDisplay>();
        private readonly IEnumerable<Role> _roles;

        #endregion

        private static readonly Logger Logger = Logger.GetLogger<Module>();

        public SearchPanelView(ICollection<Role> roles)
        {
            _roles = roles;
        }

        protected override void Build(Container buildPanel)
        {
            _squadMembersPanel = new FlowPanel
            {
                FlowDirection = ControlFlowDirection.LeftToRight,
                ControlPadding = new Vector2(8, 8),
                Parent = buildPanel,
                Location = new Point(buildPanel.ContentRegion.Left, buildPanel.ContentRegion.Top),
                CanScroll = true,
                Size = new Point(buildPanel.ContentRegion.Width, buildPanel.ContentRegion.Height), //
                Title = "Search result",
                ShowBorder = true
            };
        }

        public void Sort(Dictionary<string, int> order)
        {
            _squadMembersPanel.SortChildren((PlayerDisplay pd1, PlayerDisplay pd2) =>
            {
                int cmp = order[pd1.AccountName].CompareTo(order[pd2.AccountName]);

                if (cmp == 0)
                {
                    Character c1 = (pd1.CharacterName != "") ? new Character(pd1.CharacterName, pd1.Profession, pd1.Specialization) : null;
                    SquadPlayerSort.PlayerSortInfo p1 = new SquadPlayerSort.PlayerSortInfo(pd1.AccountName, c1, pd1.Subgroup, pd1.Role, pd1.IsSelf, pd1.IsInInstance);
                    Character c2 = (pd2.CharacterName != "") ? new Character(pd2.CharacterName, pd2.Profession, pd2.Specialization) : null;
                    SquadPlayerSort.PlayerSortInfo p2 = new SquadPlayerSort.PlayerSortInfo(pd2.AccountName, c2, pd2.Subgroup, pd2.Role, pd2.IsSelf, pd2.IsInInstance);
                    return SquadPlayerSort.Compare(p1, p2);
                }

                return cmp;
            });
        }

        public bool Exists(string accountName)
        {
            return _playerDisplays.ContainsKey(accountName);
        }

        public List<PlayerDisplay> PlayerDisplays()
        {
            return _squadMembersPanel.Children.Cast<PlayerDisplay>().ToList();
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
                BasicTooltipText = OtherCharactersToString(otherCharacters),
            };

            var currentRoles = playerModel.Roles.OrderBy(role => role.Name.ToLowerInvariant());
            playerDisplay.UpdateRoles(_roles, currentRoles);

            playerModel.OnRoleUpdated += () => OnRoleUpdate(playerDisplay, playerModel);
            playerDisplay.OnRoleRemove += (Role role) => OnRoleRemoved(playerDisplay, playerModel, role);

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

        private void OnRoleUpdate(PlayerDisplay pd, Player player)
        {
            var roles = player.Roles.OrderBy(role => role.Name.ToLowerInvariant());
            pd.UpdateRoles(_roles, roles);
        }

        private void OnRoleRemoved(PlayerDisplay pd, Player player, Role role)
        {
            player.RemoveRole(role);
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
            var accountName = playerModel.AccountName;
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
