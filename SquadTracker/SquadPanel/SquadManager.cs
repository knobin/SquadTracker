using System.Linq;
using Torlando.SquadTracker.SquadInterface;

namespace Torlando.SquadTracker.SquadPanel
{
    class SquadManager
    {
        public delegate void PlayerJoinedSquadHandler(Player player, bool isReturning);
        public delegate void PlayerUpdateSquadHandler(Player player);
        public delegate void PlayerLeftSquadHandler(string accountName);
        public delegate void BridgeConntectionHandler();

        public event PlayerJoinedSquadHandler PlayerJoinedSquad;
        public event PlayerLeftSquadHandler PlayerLeftSquad;
        public event PlayerUpdateSquadHandler PlayerUpdateSquad;

        public event BridgeConntectionHandler BridgeConnected;
        public event BridgeConntectionHandler BridgeDisconnected;

        private readonly PlayersManager _playersManager;

        private readonly Squad _squad;
        private readonly SquadInterfaceView _squadInterfaceView;

        private bool _bridgeConnected = false;

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

        public bool IsBridgeConnected()
        {
            return _bridgeConnected;
        }

        public void SetBridgeConnectionStatus(bool connected)
        {
            _bridgeConnected = connected;

            if (_bridgeConnected)
            {
                _squadInterfaceView.HideErrorMessage();
                BridgeConnected?.Invoke();
            }
            else
            {
                _squadInterfaceView.ShowErrorMessage(Constants.Placeholder.BridgeHandlerErrorMessage);
                BridgeDisconnected?.Invoke();
            } 
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

                if (!Module.KeepPlayerRolesWhenRejoining.Value)
                {
                    newPlayer.ClearRoles();
                }
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
