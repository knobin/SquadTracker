using System.Collections.Generic;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Torlando.SquadTracker.RolesScreen;
using Torlando.SquadTracker.SquadPanel;
using Torlando.SquadTracker.SearchPanel;
using Blish_HUD;

namespace Torlando.SquadTracker.MainScreen
{
    internal class MainScreenPresenter : Presenter<MainScreenView, int>
    {
        private readonly PlayersManager _playersManager;
        private readonly SquadManager _squadManager;
        private readonly PlayerIconsManager _iconsManager;
        private readonly ICollection<Role> _roles;

        private SquadPanelView _squadView;
        private SquadPanelPresenter _squadPresenter;
        private RolesView _rolesView;
        private RolesPresenter _rolesPresenter;

        private static readonly Logger Logger = Logger.GetLogger<Module>();

        public MainScreenPresenter(MainScreenView view, PlayersManager playersManager, SquadManager squadManager, PlayerIconsManager iconsManager, ICollection<Role> roles) : base (view, 0)
        {
            _playersManager = playersManager;
            _squadManager = squadManager;
            _iconsManager = iconsManager;
            _roles = roles;

            _squadView = new SquadPanelView(_roles);
            _squadPresenter = new SquadPanelPresenter(_squadView, _playersManager, _squadManager, _iconsManager, _roles);

            _rolesView = new RolesView();
            _rolesPresenter = new RolesPresenter(_rolesView, _roles);
        }

        protected override void Unload()
        {
            Logger.Info("Unloading MainScreenPresenter");

            _squadView = null;
            _squadPresenter = null;
            _rolesView = null;
            _rolesPresenter = null;
        }

        public IView SelectView(string name)
        {
            return name switch
            {
                "Squad Members" => this.CreateSquadView(),
                "Squad Roles" => this.CreateRolesView(),
                _ => this.CreateSquadView(),
            };
        }

        private IView CreateSquadView()
        {
            // var view = new SquadPanelView(_roles);
            // var presenter = new SquadPanelPresenter(view, _playersManager, _squadManager, _iconsManager, _roles);
            // return view.WithPresenter(presenter);
            return _squadView.WithPresenter(_squadPresenter);
        }

        private IView CreateRolesView()
        {
            // var view = new RolesView();
            // var presenter = new RolesPresenter(view, _roles);
            // return view.WithPresenter(presenter);
            return _rolesView.WithPresenter(_rolesPresenter);
        }

        public IView SearchView(TextBox searchbar)
        {
            var view = new SearchPanelView(_roles);
            var presenter = new SearchPanelPresenter(view, _playersManager, _squadManager, _iconsManager, _roles, searchbar);
            return view.WithPresenter(presenter);
        }
    }
}
