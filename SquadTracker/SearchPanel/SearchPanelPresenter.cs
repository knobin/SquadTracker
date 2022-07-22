using Blish_HUD.Graphics.UI;
using System.Linq;
using System.Collections.Generic;
using Torlando.SquadTracker.RolesScreen;
using Blish_HUD.Controls;
using Torlando.SquadTracker.SearchPanel;

namespace Torlando.SquadTracker.SquadPanel
{
    internal class SearchPanelPresenter : Presenter<SearchPanelView, object>
    {
        private readonly PlayersManager _playersManager;
        private readonly SquadManager _squadManager;
        private readonly PlayerIconsManager _iconsManager;
        private readonly IEnumerable<Role> _roles;
        private readonly TextBox _searchbar;
        private TaskQueue _queue = new TaskQueue();

        private readonly Squad _squad;

        public SearchPanelPresenter(
            SearchPanelView view,
            PlayersManager playersManager,
            SquadManager squadManager,
            PlayerIconsManager iconsManager,
            IEnumerable<Role> roles,
            TextBox searchbar
        ) : base (view, null)
        {
            _playersManager = playersManager;
            _squadManager = squadManager;
            _iconsManager = iconsManager;
            _roles = roles;
            _searchbar = searchbar;
            _squad = _squadManager.GetSquad();
        }

        private void OnSearchInput(object sender, System.EventArgs e)
        {
            string input = _searchbar.Text;
            _queue.Enqueue(() =>
            {
                Filter(input);
            });
        }

        private void Filter(string input)
        {
            Dictionary<string, int> order = new Dictionary<string, int>();

            foreach (var member in _squad.CurrentMembers.ToList())
            {
                bool exists = View.Exists(member.AccountName);
                int match = Match(member, input);
                if (match > 0)
                {
                    order.Add(member.AccountName, match);
                    if (!exists)
                    {
                        AddPlayer(member, false);
                    }
                }
                else
                {
                    if (exists)
                    {
                        View.RemovePlayer(member.AccountName);
                    }
                }
            }

            if (order.Count > 0)
            {
                View.Sort(order);
            }
        }

        private int Match(Player player, string input)
        {
            Character character = player.CurrentCharacter;
            string charname = (character != null) ? character.Name.ToLowerInvariant() : "";

            if ((player.AccountName.ToLowerInvariant().Contains(input) || charname.Contains(input)))
                return input.Length;

            return 0; 
        }

        protected override void UpdateView()
        {
            Dictionary<string, int> order = new Dictionary<string, int>();

            foreach (var member in _squad.CurrentMembers.ToList())
            {
                int match = Match(member, _searchbar.Text);
                if (match > 0)
                {
                    order.Add(member.AccountName, match);
                    AddPlayer(member, false);
                }
            }

            if (order.Count > 0)
            {
                View.Sort(order);
            }

            _squadManager.PlayerJoinedSquad += AddPlayer;
            _playersManager.CharacterChangedSpecialization += ChangeCharacterSpecialization;
            _squadManager.PlayerLeftSquad += RemovePlayer;
            _squadManager.PlayerUpdateSquad += UpdatePlayer;

            _queue.Start();

            _searchbar.TextChanged += OnSearchInput;
        }

        protected override void Unload()
        {
            _queue.Stop();

            // To allow for garbage collection.
            _squadManager.PlayerJoinedSquad -= AddPlayer;
            _playersManager.CharacterChangedSpecialization -= ChangeCharacterSpecialization;
            _squadManager.PlayerLeftSquad -= RemovePlayer;
            _squadManager.PlayerUpdateSquad -= UpdatePlayer;
        }

        private void AddPlayer(Player player, bool isReturning)
        {
            _queue.Enqueue(() =>
            {
                Character character = player.CurrentCharacter;
                var icon = (character != null) ? _iconsManager.GetSpecializationIcon(character.Profession, character.Specialization) : null;

                if (!View.Exists(player.AccountName))
                {
                    if (Match(player, _searchbar.Text) > 0)
                    {
                        View.DisplayPlayer(player, icon, _roles);
                        Filter(_searchbar.Text);
                    }
                }
            });
        }

        private void UpdatePlayer(Player player)
        {
            _queue.Enqueue(() =>
            {
                Character character = player.CurrentCharacter;
                var icon = (character != null) ? _iconsManager.GetSpecializationIcon(character.Profession, character.Specialization) : null;

                if (View.Exists(player.AccountName))
                {
                    if(Match(player, _searchbar.Text) > 0)
                    {
                        View.UpdatePlayer(player, icon, _roles, _squad.GetRoles(player.AccountName));
                        Filter(_searchbar.Text);
                    }
                else
                    {
                        View.RemovePlayer(player.AccountName);
                    }
                } 
            });
        }

        private void ChangeCharacterSpecialization(Character character)
        {
            _queue.Enqueue(() =>
            {
                var icon = (character != null) ? _iconsManager.GetSpecializationIcon(character.Profession, character.Specialization) : null;

                // TODO: Check with search input? (sorting doesnt depend on specialization for now).
                View.SetPlayerIcon(character.Player, icon);
            });
        }

        private void RemovePlayer(string accountName)
        {
            _queue.Enqueue(() =>
            {
                View.RemovePlayer(accountName);
            });
        }

        public void UpdateSelectedRoles(string accountName, string role, int index)
        {
            // TODO: Check with search input? (sorting doesnt depend on roles for now).
            _squad.SetRole(accountName, role, index);
        }
    }
}
