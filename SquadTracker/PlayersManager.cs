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
        public delegate void SelfUpdatedHandler(string accountName);

        public event PlayerJoinedInstanceHandler PlayerJoinedInstance;
        public event PlayerLeftInstanceHandler PlayerLeftInstance;
        public event PlayerUpdatedHandler PlayerUpdated;
        public event CharacterChangedSpecializationHandler CharacterChangedSpecialization;
        public event SelfUpdatedHandler SelfUpdated;

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
            this.SelfUpdated?.Invoke(_self);

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
                    Logger.Info("Updating Character: {}", playerInfo.characterName);
                    character = ch;
                    character.Specialization = playerInfo.elite;
                }
                else
                {
                    Logger.Info("Creating Character: {}", playerInfo.characterName);
                    character = new Character(playerInfo.characterName, playerInfo.profession, playerInfo.elite);
                    _characters.Add(character.Name, character);
                }
            }

            if (_players.TryGetValue(playerInfo.accountName, out var player))
            {
                Logger.Info("Assigning Character: \"{}\" : to user \"{}\"", (character != null) ? character.Name : "", playerInfo.accountName);
                player.CurrentCharacter = character;
                player.IsInInstance = playerInfo.inInstance;
                player.Subgroup = (uint)playerInfo.subgroup;
                player.Role = playerInfo.role;
            }
            else
            {
                Logger.Info("Assigning Character: \"{}\" : to new user \"{}\"", (character != null) ? character.Name : "", playerInfo.accountName);
                player = new Player(playerInfo.accountName, character, (uint)playerInfo.subgroup)
                {
                    IsInInstance = playerInfo.inInstance,
                    Role = playerInfo.role,
                    IsSelf = (playerInfo.accountName == _self)
            };
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
            Logger.Info("Update {} : {}, inInstance {}", playerInfo.accountName, (playerInfo.characterName != null) ? playerInfo.characterName : "", playerInfo.inInstance);
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
                        player.IsInInstance = playerInfo.inInstance;
                        player.Subgroup = playerInfo.subgroup;
                        player.Role = playerInfo.role;
                        this.PlayerUpdated?.Invoke(player);
                    }
                }
                else
                {
                    Logger.Info("Creating Character: {}", playerInfo.characterName);
                    Character character = new Character(playerInfo.characterName, playerInfo.profession, playerInfo.elite);
                    _characters.Add(character.Name, character);
                    
                    if (_players.TryGetValue(playerInfo.accountName, out var player))
                    {
                        Logger.Info("Assigning Character: {} : to user {}", playerInfo.characterName, playerInfo.accountName);
                        player.CurrentCharacter = character;
                        player.IsInInstance = playerInfo.inInstance;
                        player.Subgroup = playerInfo.subgroup;
                        player.Role = playerInfo.role;
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
                        player.IsInInstance = playerInfo.inInstance;
                        player.Role = playerInfo.role;
                        this.PlayerUpdated?.Invoke(player);
                    }
                }
            }
        }
    }
}
