using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Microsoft.Xna.Framework;
using System.Linq;
using System.Collections.Generic;
using Torlando.SquadTracker.RolesScreen;
using Blish_HUD;
using Torlando.SquadTracker.SquadInterface;

namespace Torlando.SquadTracker.SquadPanel
{
    internal class SquadPanelView : View<SquadPanelPresenter>
    {
        #region Controls
        
        private PlayerDisplayPanel _squadMembersPanel;
        private PlayerDisplayPanel _formerSquadMembersPanel;
        private Label _bridgeNotConnected;
        private StandardButton _clearFormerSquadButton;
        private readonly Dictionary<string, PlayerDisplay> _playerDisplays = new Dictionary<string, PlayerDisplay>();
        private readonly IEnumerable<Role> _roles;
        private static readonly Logger Logger = Logger.GetLogger<Module>();

        public delegate void RoleRemovedHandler(string accountName, Role role);
        public event RoleRemovedHandler OnRoleRemoved;

        #endregion

        public SquadPanelView(ICollection<Role> roles)
        {
            _roles = roles;
        }

        protected override void Build(Container buildPanel)
        {
            Logger.Info("SquadPanelView Building");
            
            _squadMembersPanel = new PlayerDisplayPanel()
            {
                Parent = buildPanel,
                Location = new Point(buildPanel.ContentRegion.Left, buildPanel.ContentRegion.Top),
                Size = new Point(buildPanel.ContentRegion.Width, 530), //
                Title = "Current Squad Members"
            };
            _formerSquadMembersPanel = new PlayerDisplayPanel()
            {
                Parent = buildPanel,
                Location = new Point(buildPanel.ContentRegion.Left, _squadMembersPanel.Bottom + 10),
                Size = new Point(_squadMembersPanel.Width, 150),
                Title = "Former Squad Members"
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
            _bridgeNotConnected = new Label()
            {
                Visible = false,
                Parent = buildPanel,
                Font = GameService.Content.DefaultFont18,
                Location = new Point(10, 10)
            };
        }

        protected override void Unload()
        {
            Logger.Info("Unloading SquadPanelView");
            Clear();

            _squadMembersPanel.Clear();
            _squadMembersPanel.Parent = null;
            _squadMembersPanel.Dispose();

            _formerSquadMembersPanel.Clear();
            _formerSquadMembersPanel.Parent = null;
            _formerSquadMembersPanel.Dispose();

            _clearFormerSquadButton.Parent = null;
            _clearFormerSquadButton.Dispose();
            
            _bridgeNotConnected.Parent = null;
            _bridgeNotConnected.Dispose();
        }

        public void ShowErrorMessage(string message)
        {
            _bridgeNotConnected.Text = message;
            var strSize = _bridgeNotConnected.Font.MeasureString(_bridgeNotConnected.Text);
            var strWidth = (int)strSize.Width + 10;
            var strHeight = (int)strSize.Height + 10;
            _bridgeNotConnected.Size = new Point(strWidth, strHeight);

            _bridgeNotConnected.Visible = true;
            _squadMembersPanel.Visible = false;
            _formerSquadMembersPanel.Visible = false;
            _clearFormerSquadButton.Visible = false;
        }

        public void HideErrorMessage()
        {
            _bridgeNotConnected.Visible = false;
            _squadMembersPanel.Visible = true;
            _formerSquadMembersPanel.Visible = true;
            _clearFormerSquadButton.Visible = true;

            _bridgeNotConnected.Text = "";
        }

        private static int Compare(PlayerDisplay pd1, PlayerDisplay pd2)
        {
            return SquadPlayerSort.Compare(pd1, pd2);
        }
        
        public bool Exists(string accountName)
        {
            return _playerDisplays.ContainsKey(accountName);
        }

        public void Sort()
        {
            if (_squadMembersPanel.Visible)
                _squadMembersPanel.SortChildren<PlayerDisplay>(Compare);
        }
        
        public void PlayerDisplayVisible(string accountName, bool visible)
        {
            if (!_playerDisplays.TryGetValue(accountName, out var display)) return;
            display.Visible = visible;
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

        public void Clear()
        {
            _squadMembersPanel.Clear();
            _formerSquadMembersPanel.Clear();

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
            display.OnRoleRemove -= OnDisplayRoleRemoved;
            display.Dispose();
        }
    }
}
