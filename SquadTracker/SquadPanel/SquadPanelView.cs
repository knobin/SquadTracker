using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Microsoft.Xna.Framework;
using System.Linq;
using System.Collections.Generic;
using Torlando.SquadTracker.RolesScreen;
using Blish_HUD;
using Torlando.SquadTracker.Constants;
using static Torlando.SquadTracker.SquadPanel.SquadPlayerSort;

namespace Torlando.SquadTracker.SquadPanel
{
    internal class SquadPanelView : View<SquadPanelPresenter>
    {
        #region Controls
        
        private FlowPanel _squadMembersPanel;
        private FlowPanel _formerSquadMembersPanel;
        private StandardButton _clearFormerSquadButton;
        private Dictionary<string, PlayerDisplay> _playerDisplays = new Dictionary<string, PlayerDisplay>();
        private readonly IEnumerable<Role> _roles;

        #endregion

        private static readonly Logger Logger = Logger.GetLogger<Module>();

        public SquadPanelView(ICollection<Role> roles)
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
                Size = new Point(buildPanel.ContentRegion.Width, 530), //
                Title = "Current Squad Members",
                ShowBorder = true,
                BasicTooltipText = "You loaded Blish HUD after starting Guild Wars 2. Please change maps to refresh."
            };
            _formerSquadMembersPanel = new FlowPanel
            {
                FlowDirection = ControlFlowDirection.LeftToRight,
                ControlPadding = new Vector2(8, 8),
                Parent = buildPanel,
                Location = new Point(buildPanel.ContentRegion.Left, _squadMembersPanel.Bottom + 10),
                CanScroll = true,
                Size = new Point(_squadMembersPanel.Width, 150),
                Title = "Former Squad Members",
                ShowBorder = true
            };
            _clearFormerSquadButton = new StandardButton
            {
                Parent = buildPanel,
                Text = "Clear",
                Location = new Point(_formerSquadMembersPanel.Right - 135, _formerSquadMembersPanel.Top + 5)
            };
            _clearFormerSquadButton.Click += delegate
            {
                Presenter.ClearFormerSquadMembers();
            };
        }

        private static int CompareBySubgroup(PlayerDisplay pd1, PlayerDisplay pd2)
        {
            Character c1 = (pd1.CharacterName != "") ? new Character(pd1.CharacterName, pd1.Profession, pd1.Specialization) : null;
            PlayerSortInfo p1 = new PlayerSortInfo(pd1.AccountName, c1, pd1.Subgroup, pd1.Role, pd1.IsSelf, pd1.IsInInstance);
            Character c2 = (pd2.CharacterName != "") ? new Character(pd2.CharacterName, pd2.Profession, pd2.Specialization) : null;
            PlayerSortInfo p2 = new PlayerSortInfo(pd2.AccountName, c2, pd2.Subgroup, pd2.Role, pd2.IsSelf, pd2.IsInInstance);
            return SquadPlayerSort.Compare(p1, p2);
        }

        private void Sort()
        {
            if (_squadMembersPanel.Visible)
                _squadMembersPanel.SortChildren<PlayerDisplay>(CompareBySubgroup);
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

            Sort();
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
            Sort();
        }

        private void UpdateSelectedRoles(Player playerModel, ValueChangedEventArgs e, int index)
        {
            var role = e.CurrentValue;
            var accountName = playerModel.AccountName;
            // Presenter.UpdateSelectedRoles(accountName, role, index);
            Sort();

            var selectedRole = _roles.FirstOrDefault(r => r.Name.Equals(role));
            Logger.Info("Selected role: {}, from {}, str {}", selectedRole, index, role);
            playerModel.AddRole(selectedRole);
        }

        public void SetPlayerIcon(Player playerModel, AsyncTexture2D icon)
        {
            if (!_playerDisplays.TryGetValue(playerModel.AccountName, out var display)) return;
            display.Icon = icon;
        }

        public void MovePlayerToFormerMembers(string accountName)
        {
            if (_playerDisplays.TryGetValue(accountName, out var display))
            { 
                display.Parent = _formerSquadMembersPanel;
            }
        }

        public void MoveFormerPlayerBackToSquad(Player playerModel, AsyncTexture2D icon)
        {
            if (!_playerDisplays.TryGetValue(playerModel.AccountName, out var display)) return;

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

            display.Parent = _squadMembersPanel;
            Sort();
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

        public void RemoveFormerMember(string accountName)
        {
            if (!_playerDisplays.TryGetValue(accountName, out var display)) return;

            _playerDisplays.Remove(accountName);
            display.Parent = null;
        }
    }
}
