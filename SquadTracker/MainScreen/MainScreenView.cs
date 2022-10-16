using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Microsoft.Xna.Framework;

namespace Torlando.SquadTracker.MainScreen
{
    internal class MainScreenView : View<MainScreenPresenter>
    {
        #region Controls
        private ViewContainer _menuPanel;
        private Menu _menuCategories;
        private MenuItem _squadMembersMenu;
        private MenuItem _squadRolesMenu;
        private ViewContainer _viewContainer;
        private TextBox _searchbar;

        private static readonly Logger Logger = Logger.GetLogger<Module>();

        #if DEBUG
        // private StandardButton _addPlayerButton;
        // private StandardButton _removeButton;
        #endif
        #endregion

        public MainScreenView()
        {
        }

        protected override void Build(Container buildPanel)
        {
            _searchbar = new TextBox()
            {
                Parent = buildPanel,
                Size = new Point(Panel.MenuStandard.Size.X - 10, 30),
                Location = new Point(5, 0),
                PlaceholderText = "Search"
            };
            _menuPanel = new ViewContainer
            {
                Title = "Squad Tracker Menu",
                ShowBorder = true,
                Size = new Point(Panel.MenuStandard.Size.X, Panel.MenuStandard.Size.Y - _searchbar.Height - 10),
                Location = new Point(0, _searchbar.Height + 10),
                Parent = buildPanel
            };
            _menuCategories = new Menu
            {
                Size = _menuPanel.ContentRegion.Size,
                MenuItemHeight = 40,
                Parent = _menuPanel,
                CanSelect = true
            };
            _viewContainer = new ViewContainer
            {
                Parent = buildPanel,
                Location = new Point(_menuCategories.Right + 10, _menuCategories.Top),
                Width = buildPanel.ContentRegion.Width - _menuPanel.Width - 10,
                Height = buildPanel.ContentRegion.Height
            };
            _squadMembersMenu = _menuCategories.AddMenuItem("Squad Members");
            _squadMembersMenu.ItemSelected += (o, e) => ShowView("Squad Members");
            _squadMembersMenu.Select();

            _squadRolesMenu = _menuCategories.AddMenuItem("Squad Roles");
            _squadRolesMenu.ItemSelected += (o, e) => ShowView("Squad Roles");

            _searchbar.TextChanged += Searching;
        }

        protected override void Unload()
        {
            Logger.Info("Unloading MainScreenView");

            _menuPanel.Parent = null;
            _menuCategories.Parent = null;
            _squadMembersMenu.Parent = null;
            _squadRolesMenu.Parent = null;
            _viewContainer.Parent = null;
            _searchbar.Parent = null;

            _menuPanel.Dispose();
            _menuCategories.Dispose();
            _squadMembersMenu.Dispose();
            _squadRolesMenu.Dispose();
            _viewContainer.Dispose();
            _searchbar.Dispose();

            _menuPanel = null;
            _menuCategories = null;
            _squadMembersMenu = null;
            _squadRolesMenu = null;
            _viewContainer = null;
            _searchbar = null;
        }

        private void ShowView(string viewName)
        {
            _searching = false;
            _viewContainer.Show(Presenter.SelectView(viewName));
        }

        private bool _searching = false;
        private void Searching(object sender, System.EventArgs e)
        {
            if (_searchbar.Text.Length > 0 && !_searching)
            {
                SearchView();
            }
            else if (_searchbar.Text.Length == 0 && _searching)
            {
                ShowView(_menuCategories.SelectedMenuItem.Text);
            }
        }

        private void SearchView()
        {
            _searching = true;
            _viewContainer.Show(Presenter.SearchView(_searchbar));
        }
    }
}
