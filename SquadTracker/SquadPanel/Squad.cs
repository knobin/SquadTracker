using System;
using System.Collections.Generic;
using Torlando.SquadTracker.Constants;

namespace Torlando.SquadTracker.SquadPanel
{
    public class Squad : IDisposable
    {
        public ICollection<Player> CurrentMembers { get; private set; } = new HashSet<Player>();
        public ICollection<Player> FormerMembers { get; private set; } = new HashSet<Player>();
        /// <summary>
        /// Key is account name, value is list of role names
        /// </summary>
        private Dictionary<string, List<string>> _assignedRoles = new Dictionary<string, List<string>>();

        public void Dispose()
        {
            Clear();
            CurrentMembers = null;
            FormerMembers = null;
            _assignedRoles = null;
        }

        public void Clear()
        {
            CurrentMembers.Clear();
            FormerMembers.Clear();
            _assignedRoles.Clear();
        }
        
        public List<string> GetRoles(string accountName)
        {
            if (!_assignedRoles.TryGetValue(accountName, out var roles)) return new List<string> { Placeholder.DefaultRole, Placeholder.DefaultRole };
            return roles;
        }

        public void SetRole(string accountName, string role, int index)
        {
            if (!_assignedRoles.ContainsKey(accountName))
            {
                _assignedRoles.Add(accountName, new List<string> { Placeholder.DefaultRole, Placeholder.DefaultRole });
            }
            _assignedRoles[accountName][index] = role;
        }

        //public ICollection<Role> FilledRoles { get; } = new List<Role>();

        public void ClearFormerMembers()
        {
            FormerMembers = new HashSet<Player>();
        }
    }
}
