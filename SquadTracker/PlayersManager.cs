using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blish_HUD;
using Blish_HUD.Controls;
using BridgeHandler;

namespace Torlando.SquadTracker
{
    internal class PlayersManager  : IDisposable
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
        public event ClearPlayers PlayerClear;

        private readonly IDictionary<string, Player> _players = new Dictionary<string, Player>();
        private readonly IDictionary<string, Character> _characters = new Dictionary<string, Character>();

        private Handler _bridgeHandler;

        private static readonly Logger Logger = Logger.GetLogger<Module>();

        public PlayersManager(Handler bridgeHandler)
        {
            _bridgeHandler = bridgeHandler;

            _bridgeHandler.OnSquadStatusEvent += OnSquadInfo;
            _bridgeHandler.OnPlayerAddedEvent += OnPlayerAdd;
            _bridgeHandler.OnPlayerRemovedEvent += OnPlayerRemove;
            _bridgeHandler.OnPlayerUpdateEvent += OnPlayerUpdate;
        }

        public void Dispose()
        {
            _characters.Clear();
            _players.Clear();
            
            if (_bridgeHandler == null)
                return;
            
            _bridgeHandler.OnSquadStatusEvent -= OnSquadInfo;
            _bridgeHandler.OnPlayerAddedEvent -= OnPlayerAdd;
            _bridgeHandler.OnPlayerRemovedEvent -= OnPlayerRemove;
            _bridgeHandler.OnPlayerUpdateEvent -= OnPlayerUpdate;

            _bridgeHandler = null;
        }

        public IReadOnlyCollection<Player> GetPlayers()
        {
            return _players.Values.ToList(); // Return a clone.
        }

        private void OnSquadInfo(SquadStatus squad)
        {
            // Would be nice to batch all the members here to add them all at once
            // instead of one at a time.
            // Will also fixed the UI to not sort after every player addition.

            foreach (var entry in squad.members)
                OnPlayerAdd(entry);
        }

        private void OnPlayerAdd(PlayerInfoEntry entry)
        {
            Character character = null;
            var playerInfo = entry.player;
            playerInfo.accountName = playerInfo.accountName.TrimStart(':');

            if (!String.IsNullOrEmpty(playerInfo.characterName))
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
                player.CurrentCharacter = character; // Assigns the character to known characters for player.
                player.CurrentCharacter = (playerInfo.inInstance) ? character : null; // Sets current character to null if not in instance.
                player.IsInInstance = playerInfo.inInstance;
                player.Subgroup = playerInfo.subgroup;
                player.Role = playerInfo.role;
                player.JoinTime = playerInfo.joinTime;
            }
            else
            {
                Logger.Info("Assigning Character: \"{}\" : to new user \"{}\"", (character != null) ? character.Name : "", playerInfo.accountName);
                player = new Player(playerInfo.accountName, character, playerInfo.subgroup)
                {
                    IsInInstance = playerInfo.inInstance,
                    Role = playerInfo.role,
                    IsSelf = playerInfo.self,
                    JoinTime = playerInfo.joinTime
                };
                player.CurrentCharacter = (playerInfo.inInstance) ? character : null; // Sets current character to null if not in instance.
                _players.Add(player.AccountName, player);
            }

            this.PlayerJoinedInstance?.Invoke(player);
        }

        private static void PlayerLeftNotification(Player player)
        {
            if (!Module.PlayerWithRoleLeaveNotification.Value)
                return;

            if (!(player.Roles.Count > 0))
                return;
            
            var name = (player.CurrentCharacter != null) ? player.CurrentCharacter.Name + " (" + player.AccountName + ")" : player.AccountName;
            var roles = player.Roles.OrderBy(role => role.Name.ToLowerInvariant()).ToList();
            var roleStr = String.Join(", ", roles.Select(x => x.Name).ToArray());
            var role = (roles.Count > 1) ? "roles" : "role";
            var str = name + " from subgroup " + player.Subgroup.ToString() + " with " + role + " '" + roleStr + "' left the squad.";

            const int lineBreak = 70;
            var index = 0;

            while (index != -1 && (index + lineBreak) < str.Length)
            {
                var start = (((index + lineBreak) > str.Length) ? str.Length : index + lineBreak) - 1;
                var count = start - index + 1;
                index = str.LastIndexOf(' ', start, count);

                if (index == -1) continue;
                
                var sb = new StringBuilder(str)
                {
                    [index] = '\n'
                };
                str = sb.ToString();
            }

            // TODO(knobin): Rare collection modified error here (happened when many in squad left at around the same time).
            ScreenNotification.ShowNotification(str, ScreenNotification.NotificationType.Info, null, 6);
        }

        public void RemovePlayer(Player player)
        {
            foreach (var ch in player.KnownCharacters)
                _characters.Remove(ch.Name);
            _players.Remove(player.AccountName);
        }

        private void OnPlayerRemove(PlayerInfoEntry entry)
        {
            var playerInfo = entry.player;
            playerInfo.accountName = playerInfo.accountName.TrimStart(':');
            
            Logger.Info("Removing {}", playerInfo.accountName);
            if (playerInfo.self)
            {
                Logger.Info("Removing self! {}. Clearing squad...", playerInfo.accountName);
                this.PlayerClear?.Invoke();
                _players.Clear();
            }
            else
            {
                if (!_players.TryGetValue(playerInfo.accountName, out var player)) return;

                player.IsInInstance = false;
                this.PlayerLeftInstance?.Invoke(player.AccountName);

                PlayerLeftNotification(player);
                // RemovePlayer(player);
            }
        }

        private void OnPlayerUpdate(PlayerInfoEntry entry)
        {
            var playerInfo = entry.player;
            playerInfo.accountName = playerInfo.accountName.TrimStart(':');
            
            Logger.Info("Update {} : {}, inInstance {}", playerInfo.accountName, playerInfo.characterName ?? "", playerInfo.inInstance);
            if (!string.IsNullOrEmpty(playerInfo.characterName))
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
                        player.JoinTime = playerInfo.joinTime;
                        this.PlayerUpdated?.Invoke(player);
                    }
                }
                else
                {
                    Logger.Info("Creating Character: {}", playerInfo.characterName);
                    var character = new Character(playerInfo.characterName, playerInfo.profession, playerInfo.elite);
                    _characters.Add(character.Name, character);
                    
                    if (_players.TryGetValue(playerInfo.accountName, out var player))
                    {
                        Logger.Info("Assigning Character: {} : to user {}", playerInfo.characterName, playerInfo.accountName);
                        player.CurrentCharacter = character; // Assigns the character to known characters for player.
                        player.CurrentCharacter = (playerInfo.inInstance) ? character : null; // Sets current character to null if not in instance.
                        player.IsInInstance = playerInfo.inInstance;
                        player.Subgroup = playerInfo.subgroup;
                        player.Role = playerInfo.role;
                        player.JoinTime = playerInfo.joinTime;
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
                        player.JoinTime = playerInfo.joinTime;
                        this.PlayerUpdated?.Invoke(player);
                    }
                }
            }
        }
    }
}
