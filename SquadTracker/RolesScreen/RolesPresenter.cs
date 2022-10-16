using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Graphics.UI;

namespace Torlando.SquadTracker.RolesScreen
{
    internal class RolesPresenter : Presenter<RolesView, object>
    {
        private static readonly Logger Logger = Logger.GetLogger<Module>();

        public RolesPresenter(RolesView view, ICollection<Role> roles) : base(view, null)
        {
            _roles = roles;
        }

        protected override void UpdateView()
        {
            foreach (var role in _roles)
            {
                this.View.AddRoleDisplay(role);
            }
        }

        protected override void Unload()
        {
            Logger.Info("Unloading RolesPresenter");
        }

        public void CreateRole(string roleName)
        {
            var newRoleName = roleName.Trim();
            if (_roles.Any(role => role.Name == newRoleName))
            {
                this.View.DisplayAddRoleError("A role with this name already exists.");
                return;
            }

            var role = new Role(newRoleName);
            role.Icon = RoleIconCreator.GenerateIcon(role.Name);
            this._roles.Add(role);

            this.View.AddRoleDisplay(role);
            this.View.ResetAddRoleForm();
            
        }

        public void DeleteRole(Role role)
        {
            this._roles.Remove(role);
            this.View.RemoveRoleDisplay(role.Name);
        }

        private readonly ICollection<Role> _roles;
    }
}
