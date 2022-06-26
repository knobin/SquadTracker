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

            _bridgeHandler.OnSquadStatusEvent += OnSquadInfo;
            _bridgeHandler.OnPlayerAddedEvent += OnPlayerAdd;
            _bridgeHandler.OnPlayerRemovedEvent += OnPlayerRemove;
            _bridgeHandler.OnPlayerUpdateEvent += OnPlayerUpdate;
        }

        public IReadOnlyCollection<Player> GetPlayers()
        {
            return _players.Values.ToList(); // Return a clone.
        }

        private void OnSquadInfo(Handler.SquadStatus squad)
        {
            _self = squad.self;

            foreach (Handler.PlayerInfo pi in squad.members)
                OnPlayerAdd(pi);
        }

        private void OnPlayerAdd(Handler.PlayerInfo playerInfo)
        {
            Character character = null;

            if (playerInfo.characterName != null && playerInfo.characterName != "")
            {
                if (_characters.TryGetValue(playerInfo.characterName, out var ch))
                {
                    character = ch;
                    character.Specialization = playerInfo.elite;
                }
                else
                {
                    character = new Character(playerInfo.characterName, playerInfo.profession, playerInfo.elite);
                    _characters.Add(character.Name, character);
                }
            }

            if (_players.TryGetValue(playerInfo.accountName, out var player))
            {
                player.CurrentCharacter = character;
                player.IsInInstance = true;
                player.Subgroup = (uint)playerInfo.subgroup;
            }
            else
            {
                player = new Player(playerInfo.accountName, character, (uint)playerInfo.subgroup);
                _players.Add(player.AccountName, player);
            }

            this.PlayerJoinedInstance?.Invoke(player);
        }

        private void OnPlayerRemove(Handler.PlayerInfo playerInfo)
        {
            Logger.Info("Removing {}", playerInfo.accountName);
            if (_self == playerInfo.accountName)
            {
                Logger.Info("Removing self! {}", playerInfo.accountName);
                List<string> keys = new List<string>(_players.Keys);
                foreach (string key in keys)
                {
                    _players[key].IsInInstance = false;
                    this.PlayerLeftInstance?.Invoke(_players[key].AccountName);
                }
            }
            else
            {
                if (!_players.TryGetValue(playerInfo.accountName, out var player)) return;

                player.IsInInstance = false;
                this.PlayerLeftInstance?.Invoke(player.AccountName);
            }
        }

        private void OnPlayerUpdate(Handler.PlayerInfo playerInfo)
        {
            Logger.Info("Update {} : {}", playerInfo.accountName, (playerInfo.characterName != null) ? playerInfo.characterName : "");
            if (playerInfo.characterName != null)
            {
                if (_characters.TryGetValue(playerInfo.characterName, out var srcCharacter))
                {
                    if (srcCharacter.Specialization != playerInfo.elite)
                    {
                        srcCharacter.Specialization = playerInfo.elite;
                        this.CharacterChangedSpecialization?.Invoke(srcCharacter);
                    }
                    if (_players.TryGetValue(playerInfo.accountName, out var player))
                    {
                        player.CurrentCharacter = srcCharacter;
                        player.IsInInstance = true;
                        this.PlayerUpdated?.Invoke(player);
                    }
                }
                else
                {
                    Logger.Info("Adding Character: {}", playerInfo.characterName);
                    Character character = new Character(playerInfo.characterName, playerInfo.profession, playerInfo.elite);
                    _characters.Add(character.Name, character);
                    
                    if (_players.TryGetValue(playerInfo.accountName, out var player))
                    {
                        Logger.Info("Adding Character: {} : to user {}", playerInfo.characterName, playerInfo.accountName);
                        player.CurrentCharacter = character;
                        player.IsInInstance = true;
                        this.PlayerUpdated?.Invoke(player);
                    }
                }
            } 
            else
            {
                // No character name here.

                if (_players.TryGetValue(playerInfo.accountName, out var player))
                {
                    if (player.Subgroup != playerInfo.subgroup)
                    {
                        player.Subgroup = playerInfo.subgroup;
                        this.PlayerUpdated?.Invoke(player);
                    }
                }
            }
        }
    }
}
