using System.Linq;
using Torlando.SquadTracker.SquadInterface;

namespace Torlando.SquadTracker.SquadPanel
{
    class SquadManager
    {
        public delegate void PlayerJoinedSquadHandler(Player player, bool isReturning);
        public delegate void PlayerUpdateSquadHandler(Player player);
        public delegate void PlayerLeftSquadHandler(string accountName);
        public delegate void BridgeErrorHandler(string message);
        public delegate void BridgeHandler();
        public delegate void ClearSquadHandler();

        public event PlayerJoinedSquadHandler PlayerJoinedSquad;
        public event PlayerLeftSquadHandler PlayerLeftSquad;
        public event PlayerUpdateSquadHandler PlayerUpdateSquad;
        public event ClearSquadHandler ClearSquad;

        public event BridgeErrorHandler BridgeError;
        public event BridgeHandler BridgeConnected;

        private readonly PlayersManager _playersManager;

        private readonly Squad _squad;
        private readonly SquadInterfaceView _squadInterfaceView;

        private bool _bridgeConnected = false;
        private string _bridgeError = Constants.Placeholder.BridgeHandlerErrorMessage;

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
            _playersManager.PlayerClear += OnPlayerClear;

            OnBridgeError(Constants.Placeholder.BridgeHandlerErrorMessage);
        }

        public bool IsBridgeConnected()
        {
            return _bridgeConnected;
        }

        public string LastBridgeError()
        {
            return _bridgeError;
        }

        public void OnBridgeError(string message)
        {
            _bridgeError = message;
            _squadInterfaceView.ShowErrorMessage(message);
            BridgeError?.Invoke(message);
        }

        public void BridgeConnectionInfo(bool combatEnabled, bool extrasFound, bool extrasEnabled, bool squadEnabled)
        {
            // Getting this function call means that arcdps_bridge has loaded.
            // Both combat and extras can be disabled in the arcdps_bridge config file.
            // For SquadTracker we need both to be loaded.

            if (!squadEnabled) // squad == combatEnabled && extrasEnabled.
            {
                string message = "";

                if (!combatEnabled)
                {
                    message += Constants.Placeholder.BridgeHandlerCombatDisabledErrorMessage;
                    message += ((!extrasFound) || (!extrasEnabled)) ? "\n\n" : "";
                }

                if (!extrasFound)
                {
                    message += Constants.Placeholder.BridgeHandlerExtrasErrorMessage;
                }
                else if (!extrasEnabled)
                {
                    message += Constants.Placeholder.BridgeHandlerExtrasDisabledErrorMessage;
                }

                if (message.Length > 0)
                {
                    OnBridgeError(message);    
                }
            }
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
                OnBridgeError(Constants.Placeholder.BridgeHandlerDisconnectMessage);
            }
        }

        public Squad GetSquad()
        {
            return _squad;
        }

        public void OnPlayerClear()
        {
            _squad.CurrentMembers.Clear();
            _squad.FormerMembers.Clear();

            _squadInterfaceView.Clear();
            ClearSquad?.Invoke();
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
