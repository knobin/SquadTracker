using Blish_HUD.Graphics.UI;
using System.Linq;
using System.Collections.Generic;
using Torlando.SquadTracker.RolesScreen;
using Blish_HUD;
using Torlando.SquadTracker.SquadInterface;

namespace Torlando.SquadTracker.SquadPanel
{
    internal class SquadPanelPresenter : Presenter<SquadPanelView, object>
    {
        private readonly PlayersManager _playersManager;
        private readonly SquadManager _squadManager;
        private readonly PlayerIconsManager _iconsManager;
        private readonly IEnumerable<Role> _roles;

        private readonly Squad _squad;
        private static readonly Logger Logger = Logger.GetLogger<Module>();

        public SquadPanelPresenter(
            SquadPanelView view,
            PlayersManager playersManager,
            SquadManager squadManager,
            PlayerIconsManager iconsManager,
            IEnumerable<Role> roles
        ) : base (view, null)
        {
            _playersManager = playersManager;
            _squadManager = squadManager;
            _iconsManager = iconsManager;
            _roles = roles;

            _squad = _squadManager.GetSquad();
        }

        private static int PlayerCompareHelper(Player player1, Player player2)
        {
            return SquadPlayerSort.Compare(player1, player2, Module.PrioritizeBoonsWhenSorting.Value);
        }

        protected override void UpdateView()
        {
            Logger.Info("Updating SquadPanelPresenter");

            var squad = _squad.CurrentMembers.ToList();
            squad.Sort(PlayerCompareHelper);
            
            for (var i = 0; i < squad.Count; ++i)
            {
                AddPlayer(squad[i], false);
                squad[i] = null;
            }

            squad.Clear();

            foreach (var formerMember in _squad.FormerMembers.ToList())
            {
                AddPlayer(formerMember, false);
                View.MovePlayerToFormerMembers(formerMember.AccountName);
            }

            _squadManager.PlayerJoinedSquad += AddPlayer;
            _playersManager.CharacterChangedSpecialization += ChangeCharacterSpecialization;
            _squadManager.PlayerLeftSquad += RemovePlayer;
            _squadManager.PlayerUpdateSquad += UpdatePlayer;
            _squadManager.ClearSquad += ClearPlayers;

            _squadManager.BridgeError += OnBridgeError;
            _squadManager.BridgeConnected += OnBridgeConnected;

            if (!_squadManager.IsBridgeConnected())
                OnBridgeError(_squadManager.LastBridgeError());

            View.OnRoleRemoved += OnPlayerDisplayRoleRemoved;
        }

        protected override void Unload()
        {
            Logger.Info("Unloading SquadPanelPresenter");

            // To allow for garbage collection.
            _squadManager.PlayerJoinedSquad -= AddPlayer;
            _playersManager.CharacterChangedSpecialization -= ChangeCharacterSpecialization;
            _squadManager.PlayerLeftSquad -= RemovePlayer;
            _squadManager.PlayerUpdateSquad -= UpdatePlayer;
            _squadManager.ClearSquad -= ClearPlayers;

            _squadManager.BridgeConnected -= OnBridgeConnected;
            _squadManager.BridgeError -= OnBridgeError;
            
            View.OnRoleRemoved -= OnPlayerDisplayRoleRemoved;
        }

        private void OnBridgeConnected()
        {
            View.HideErrorMessage();
        }

        private void OnBridgeError(string message)
        {
            View.ShowErrorMessage(message);
        }

        private void ClearPlayers()
        {
            View.Clear();
        }

        private void AddPlayer(Player player, bool isReturning)
        {
            Character character = player.CurrentCharacter;
            var icon = (character != null) ? _iconsManager.GetSpecializationIcon(character.Profession, character.Specialization) : null;

            if (isReturning)
            {
                View.MoveFormerPlayerBackToSquad(player, icon);
            }
            else
            {
                View.DisplayPlayer(player, icon, _roles);
            }

            View.Sort();

            player.OnRoleUpdated += OnRoleUpdate;
        }

        private void OnRoleUpdate(Player player)
        {
            View.OnRoleUpdate(player);
        }

        private void OnPlayerDisplayRoleRemoved(string accountName, Role role)
        {
            var player = _squad.CurrentMembers.FirstOrDefault(p => p.AccountName == accountName);
            if (player == null) player = _squad.FormerMembers.FirstOrDefault(p => p.AccountName == accountName);
            if (player == null) return;
            
            player.RemoveRole(role);
        }

        private void UpdatePlayer(Player player)
        {
            var character = player.CurrentCharacter;
            var icon = (character != null) ? _iconsManager.GetSpecializationIcon(character.Profession, character.Specialization) : null;

            View.UpdatePlayer(player, icon, _roles, _squad.GetRoles(player.AccountName));
            View.Sort();
        }

        private void ChangeCharacterSpecialization(Character character)
        {
            var icon = (character != null) ? _iconsManager.GetSpecializationIcon(character.Profession, character.Specialization) : null;
            View.SetPlayerIcon(character.Player, icon);
            View.Sort();
        }

        private void RemovePlayer(string accountName)
        {
            var player = _squad.FormerMembers.FirstOrDefault(p => p.AccountName == accountName);
            if (player == null) return;

            View.MovePlayerToFormerMembers(accountName);
            player.OnRoleUpdated -= OnRoleUpdate;
        }

        public void ClearFormerSquadMembers()
        {
            var formerMembers = _squad.FormerMembers;
            _squad.ClearFormerMembers();

            foreach (var formerMember in formerMembers)
            {
                _playersManager.RemovePlayer(formerMember);
                View.RemoveFormerMember(formerMember.AccountName);
            }
        }

        public void UpdateSelectedRoles(string accountName, string role, int index)
        {
            _squad.SetRole(accountName, role, index);
        }
    }
}
