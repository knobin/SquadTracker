using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blish_HUD;
using Blish_HUD.Controls;
using BridgeHandler;
using Torlando.SquadTracker.RolesScreen;

namespace Torlando.SquadTracker
{
    class PlayersManager
    {
        public delegate void PlayerJoinedInstanceHandler(Player newPlayer);
        public delegate void PlayerLeftInstanceHandler(string accountName);
        public delegate void CharacterChangedSpecializationHandler(Character character);
        public delegate void PlayerUpdatedHandler(Player newPlayer);
        public delegate void SelfUpdatedHandler(string accountName);
        public delegate void ClearPlayers();

        public event PlayerJoinedInstanceHandler PlayerJoinedInstance;
        public event PlayerLeftInstanceHandler PlayerLeftInstance;
        public event PlayerUpdatedHandler PlayerUpdated;
        public event CharacterChangedSpecializationHandler CharacterChangedSpecialization;
        public event SelfUpdatedHandler SelfUpdated;
        public event ClearPlayers PlayerClear;

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
                player.CurrentCharacter = character; // Assigns the character to known charactes for player.
                player.CurrentCharacter = (playerInfo.inInstance) ? character : null; // Sets current character to null if not in instance.
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
                player.CurrentCharacter = (playerInfo.inInstance) ? character : null; // Sets current character to null if not in instance.
                _players.Add(player.AccountName, player);
            }

            this.PlayerJoinedInstance?.Invoke(player);
        }

        private void PlayerLeftNotification(Player player)
        {
            if (Module.PlayerWithRoleLeaveNotification.Value)
            {
                if (player.Roles.Count > 0)
                {
                    string name = (player.CurrentCharacter != null) ? player.CurrentCharacter.Name + " (" + player.AccountName + ")" : player.AccountName;
                    List<Role> roles = player.Roles.OrderBy(role => role.Name.ToLowerInvariant()).ToList();
                    string roleStr = String.Join(", ", roles.Select(x => x.Name).ToArray());
                    string role = (roles.Count > 1) ? "roles" : "role";
                    string str = name + " from subgroup " + player.Subgroup.ToString() + " with " + role + " '" + roleStr + "' left the squad.";

                    const int lineBreak = 70;
                    int index = 0;

                    while (index != -1 && (index + lineBreak) < str.Length)
                    {
                        int start = (((index + lineBreak) > str.Length) ? str.Length : index + lineBreak) - 1;
                        int count = start - index + 1;
                        index = str.LastIndexOf(' ', start, count);

                        if (index != -1)
                        {
                            StringBuilder sb = new StringBuilder(str);
                            sb[index] = '\n';
                            str = sb.ToString();
                        }
                    }

                    ScreenNotification.ShowNotification(str, ScreenNotification.NotificationType.Info, null, 6);
                }
            }
        }

        public void RemovePlayer(Player player)
        {
            foreach (Character ch in player.KnownCharacters)
                _characters.Remove(ch.Name);
            _players.Remove(player.AccountName);
        }

        private void OnPlayerRemove(Handler.PlayerInfo playerInfo)
        {
            Logger.Info("Removing {}", playerInfo.accountName);
            if (_self == playerInfo.accountName)
            {
                Logger.Info("Removing self! {}", playerInfo.accountName);
                this.PlayerClear?.Invoke();
                _players.Clear();
            }
            else
            {
                if (!_players.TryGetValue(playerInfo.accountName, out var player)) return;

                player.IsInInstance = false;
                this.PlayerLeftInstance?.Invoke(player.AccountName);

                PlayerLeftNotification(player);
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
                        player.CurrentCharacter = (playerInfo.inInstance) ? srcCharacter : null;
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
                        player.CurrentCharacter = character; // Assigns the character to known charactes for player.
                        player.CurrentCharacter = (playerInfo.inInstance) ? character : null; // Sets current character to null if not in instance.
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
                        player.CurrentCharacter = null;
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
