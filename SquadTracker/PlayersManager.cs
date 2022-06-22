using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using BridgeHandler;

namespace Torlando.SquadTracker
{
    class PlayersManager
    {
        public delegate void PlayerJoinedInstanceHandler(Player newPlayer);
        public delegate void PlayerLeftInstanceHandler(string accountName);
        public delegate void CharacterChangedSpecializationHandler(Character character);
        public delegate void PlayerUpdatedHandler(Player newPlayer);

        public event PlayerJoinedInstanceHandler PlayerJoinedInstance;
        public event PlayerLeftInstanceHandler PlayerLeftInstance;
        public event PlayerUpdatedHandler PlayerUpdated;
        public event CharacterChangedSpecializationHandler CharacterChangedSpecialization;

        private readonly IDictionary<string, Player> _players = new Dictionary<string, Player>();
        private readonly IDictionary<string, Character> _characters = new Dictionary<string, Character>();

        private Handler _bridgeHandler;
        private string _self = "";

        private static readonly Logger Logger = Logger.GetLogger<Module>();

        public PlayersManager(Handler bridgeHandler)
        {
            _bridgeHandler = bridgeHandler;

            _bridgeHandler.OnSquadInfoEvent += OnSquadInfo;
            _bridgeHandler.OnPlayerAddedEvent += OnPlayerAdd;
            _bridgeHandler.OnPlayerRemovedEvent += OnPlayerRemove;
            _bridgeHandler.OnPlayerUpdateEvent += OnPlayerUpdate;
        }

        public IReadOnlyCollection<Player> GetPlayers()
        {
            return _players.Values.ToList(); // Return a clone.
        }

        private void OnSquadInfo(Handler.SquadInfoEvent squad)
        {
            _self = squad.Self;

            foreach (Handler.PlayerInfo pi in squad.Members)
                OnPlayerAdd(pi);
        }

        private void OnPlayerAdd(Handler.PlayerInfo playerInfo)
        {
            Character character = null;

            if (playerInfo.CharacterName != null && playerInfo.CharacterName != "")
            {
                if (_characters.TryGetValue(playerInfo.CharacterName, out var ch))
                {
                    character = ch;
                    character.Specialization = playerInfo.Elite;
                }
                else
                {
                    character = new Character(playerInfo.CharacterName, playerInfo.Profession, playerInfo.Elite);
                    _characters.Add(character.Name, character);
                }
            }

            if (_players.TryGetValue(playerInfo.AccountName, out var player))
            {
                player.CurrentCharacter = character;
                player.IsInInstance = true;
            }
            else
            {
                player = new Player(playerInfo.AccountName, character);
                _players.Add(player.AccountName, player);
            }

            this.PlayerJoinedInstance?.Invoke(player);
        }

        private void OnPlayerRemove(Handler.PlayerInfo playerInfo)
        {
            Logger.Info("Removing {}", playerInfo.AccountName);
            if (_self == playerInfo.AccountName)
            {
                Logger.Info("Removing self! {}", playerInfo.AccountName);
                List<string> keys = new List<string>(_players.Keys);
                foreach (string key in keys)
                {
                    _players[key].IsInInstance = false;
                    this.PlayerLeftInstance?.Invoke(_players[key].AccountName);
                }
            }
            else
            {
                if (!_players.TryGetValue(playerInfo.AccountName, out var player)) return;

                player.IsInInstance = false;
                this.PlayerLeftInstance?.Invoke(player.AccountName);
            }
        }

        private void OnPlayerUpdate(Handler.PlayerInfo playerInfo)
        {
            Logger.Info("Update {} : {}", playerInfo.AccountName, (playerInfo.CharacterName != null) ? playerInfo.CharacterName : "");
            if (playerInfo.CharacterName != null)
            {
                if (_characters.TryGetValue(playerInfo.CharacterName, out var srcCharacter))
                {
                    if (srcCharacter.Specialization != playerInfo.Elite)
                    {
                        srcCharacter.Specialization = playerInfo.Elite;
                        this.CharacterChangedSpecialization?.Invoke(srcCharacter);
                    }
                    if (_players.TryGetValue(playerInfo.AccountName, out var player))
                    {
                        player.CurrentCharacter = srcCharacter;
                        player.IsInInstance = true;
                        this.PlayerUpdated?.Invoke(player);
                    }
                }
                else
                {
                    Logger.Info("Adding Character: {}", playerInfo.CharacterName);
                    Character character = new Character(playerInfo.CharacterName, playerInfo.Profession, playerInfo.Elite);
                    _characters.Add(character.Name, character);
                    
                    if (_players.TryGetValue(playerInfo.AccountName, out var player))
                    {
                        Logger.Info("Adding Character: {} : to user {}", playerInfo.CharacterName, playerInfo.AccountName);
                        player.CurrentCharacter = character;
                        player.IsInInstance = true;
                        this.PlayerUpdated?.Invoke(player);
                    }
                }
            }
        }
    }
}
