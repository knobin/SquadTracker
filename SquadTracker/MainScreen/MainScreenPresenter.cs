﻿using System.Collections.Generic;
using Blish_HUD.Graphics.UI;
using Torlando.SquadTracker.RolesScreen;
using Torlando.SquadTracker.SquadPanel;

namespace Torlando.SquadTracker.MainScreen
{
    internal class MainScreenPresenter : Presenter<MainScreenView, int>
    {
        private readonly PlayersManager _playersManager;
        private readonly SquadManager _squadManager;
        private readonly PlayerIconsManager _iconsManager;
        private readonly ICollection<Role> _roles;

        public MainScreenPresenter(MainScreenView view, PlayersManager playersManager, SquadManager squadManager, PlayerIconsManager iconsManager, ICollection<Role> roles) : base (view, 0)
        {
            _playersManager = playersManager;
            _squadManager = squadManager;
            _iconsManager = iconsManager;
            _roles = roles;
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
            var view = new SquadPanelView(_roles);
            var presenter = new SquadPanelPresenter(view, _playersManager, _squadManager, _iconsManager, _roles);
            return view.WithPresenter(presenter);
        }

        private IView CreateRolesView()
        {
            var view = new RolesView();
            var presenter = new RolesPresenter(view, _roles);
            return view.WithPresenter(presenter);
        }
    }
}
