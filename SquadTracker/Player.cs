using System.Collections.Generic;
using Torlando.SquadTracker.RolesScreen;
using System.Linq;

namespace Torlando.SquadTracker
{
    internal class Player
    {
        public delegate void RoleUpdateHandler();

        public event RoleUpdateHandler OnRoleUpdated;

        public string AccountName { get; set; }
        public bool IsInInstance { get; set; } = true;
        public bool IsSelf { get; set; } = false;
        public byte Role { get; set; } = 5;
        public uint Subgroup { get; set; } = 0;
        public IReadOnlyCollection<Role> Roles => _roles;
        
        public void AddRole(Role role)
        {
            if (role != null)
            {
                if (!_roles.Contains(role))
                {
                    _roles.Add(role);
                    _roles = _roles.OrderBy(r => r.Name.ToLowerInvariant()).ToList();
                    OnRoleUpdated?.Invoke();
                }
            }
        }

        public void RemoveRole(Role role)
        {
            if (role != null)
            {
                if (_roles.Contains(role))
                {
                    _roles.Remove(role);
                    _roles = _roles.OrderBy(r => r.Name.ToLowerInvariant()).ToList();
                    OnRoleUpdated?.Invoke();
                }
            }
        }

        public void ClearRoles()
        {
            _roles.Clear();
            OnRoleUpdated?.Invoke();
        }

        private List<Role> _roles = new List<Role>();

        public Character CurrentCharacter
        {
            get => _currentCharacter;
            set
            {
                _currentCharacter = value;
                if (_currentCharacter != null)
                {
                    if (!_knownCharacters.Contains(value))
                    {
                        _knownCharacters.Add(value);
                        value.Player = this;
                    }
                }
            }
        }
        public IReadOnlyCollection<Character> KnownCharacters => _knownCharacters;

        public Player(string accountName, Character currentCharacter, uint subgroup)
        {
            AccountName = accountName;
            CurrentCharacter = currentCharacter;
            Subgroup = subgroup;
        }

        public Player(string accountName)
        {
            AccountName = accountName;
            CurrentCharacter = null;
        }

        private Character _currentCharacter;
        private readonly HashSet<Character> _knownCharacters = new HashSet<Character>();

        #if DEBUG
        public override string ToString()
        {
            return $"{AccountName} ({_knownCharacters.Count} character(s))";
        }
        #endif
    }
}
