using System.Linq;
using Torlando.SquadTracker.SquadInterface;

namespace Torlando.SquadTracker.SquadPanel
{
    class SquadManager
    {
        public delegate void PlayerJoinedSquadHandler(Player player, bool isReturning);
        public delegate void PlayerUpdateSquadHandler(Player player);
        public delegate void PlayerLeftSquadHandler(string accountName);

        public event PlayerJoinedSquadHandler PlayerJoinedSquad;
        public event PlayerLeftSquadHandler PlayerLeftSquad;
        public event PlayerUpdateSquadHandler PlayerUpdateSquad;

        private readonly PlayersManager _playersManager;

        private readonly Squad _squad;
        private readonly SquadInterfaceView _squadInterfaceView;

        public SquadManager(PlayersManager playersManager, SquadInterfaceView squadInterfaceView)
        {
            _playersManager = playersManager;
            _squadInterfaceView = squadInterfaceView;

            _squad = new Squad();

            var players = _playersManager.GetPlayers();
            foreach (var player in players.Where(p => p.IsInInstance))
            {
                _squad.CurrentMembers.Add(player);
            }

            _playersManager.PlayerJoinedInstance += OnPlayerJoinedInstance;
            _playersManager.PlayerLeftInstance += OnPlayerLeftInstance;
            _playersManager.PlayerUpdated += OnPlayerUpdate;
            _playersManager.SelfUpdated += OnSelfUpdate;
        }

        public Squad GetSquad()
        {
            return _squad;
        }

        private void OnSelfUpdate(string accountName)
        {
            _squadInterfaceView.SelfAccountName = accountName;
        }

        private void OnPlayerJoinedInstance(Player newPlayer)
        {
            _squad.CurrentMembers.Add(newPlayer);

            var isReturning = false;
            if (_squad.FormerMembers.Contains(newPlayer))
            {
                isReturning = true;
                _squad.FormerMembers.Remove(newPlayer);
            }

            _squadInterfaceView.Add(newPlayer);
            PlayerJoinedSquad?.Invoke(newPlayer, isReturning);
        }

        private void OnPlayerLeftInstance(string accountName)
        {
            var player = _squad.CurrentMembers.FirstOrDefault(p => p.AccountName == accountName);
            if (player == null) return;

            _squad.CurrentMembers.Remove(player);
            _squad.FormerMembers.Add(player);

            _squadInterfaceView.Remove(accountName);
            PlayerLeftSquad?.Invoke(accountName);
        }

        private void OnPlayerUpdate(Player player)
        {
            _squadInterfaceView.Update(player);
            PlayerUpdateSquad?.Invoke(player);
        }
    }
}
