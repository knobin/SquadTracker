using System.Collections.Generic;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Torlando.SquadTracker.RolesScreen;
using Torlando.SquadTracker.SquadPanel;
using Torlando.SquadTracker.SearchPanel;
using Blish_HUD;
using Torlando.SquadTracker.LogPanel;

namespace Torlando.SquadTracker.MainScreen
{
    internal class MainScreenPresenter : Presenter<MainScreenView, int>
    {
        private readonly PlayersManager _playersManager;
        private readonly SquadManager _squadManager;
        private readonly PlayerIconsManager _iconsManager;
        private readonly ICollection<Role> _roles;
        private readonly StLogger _stLogger;

        private SquadPanelView _squadView;
        private SquadPanelPresenter _squadPresenter;
        private RolesView _rolesView;
        private RolesPresenter _rolesPresenter;
        private LogView _logView;
        private LogPresenter _logPresenter;

        private static readonly Logger Logger = Logger.GetLogger<Module>();

        public MainScreenPresenter(MainScreenView view, PlayersManager playersManager, SquadManager squadManager, PlayerIconsManager iconsManager, ICollection<Role> roles, StLogger stLogger) : base (view, 0)
        {
            _playersManager = playersManager;
            _squadManager = squadManager;
            _iconsManager = iconsManager;
            _roles = roles;
            _stLogger = stLogger;

            _squadView = new SquadPanelView(_roles);
            _squadPresenter = new SquadPanelPresenter(_squadView, _playersManager, _squadManager, _iconsManager, _roles);

            _rolesView = new RolesView();
            _rolesPresenter = new RolesPresenter(_rolesView, _roles);
            
            _logView = new LogView();
            _logPresenter = new LogPresenter(_logView, _stLogger);
        }

        protected override void Unload()
        {
            Logger.Info("Unloading MainScreenPresenter");

            _squadView = null;
            _squadPresenter = null;
            _rolesView = null;
            _rolesPresenter = null;
            _logView = null; 
            _logPresenter = null;
        }

        public IView SelectView(string name)
        {
            return name switch
            {
                "Squad Members" => this.CreateSquadView(),
                "Squad Roles" => this.CreateRolesView(),
                "Log" => this.CreateLogView(),
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
        
        private IView CreateLogView()
        {
            // var view = new RolesView();
            // var presenter = new RolesPresenter(view, _roles);
            // return view.WithPresenter(presenter);
            return _logView.WithPresenter(_logPresenter);
        }

        public IView SearchView(TextBox searchbar)
        {
            var view = new SearchPanelView(_roles);
            var presenter = new SearchPanelPresenter(view, _playersManager, _squadManager, _iconsManager, _roles, searchbar);
            return view.WithPresenter(presenter);
        }
    }
}
