using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Blish_HUD.Input;
using BridgeHandler;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Torlando.SquadTracker.MainScreen;
using Torlando.SquadTracker.RolesScreen;
using Torlando.SquadTracker.SquadInterface;
using Torlando.SquadTracker.SquadPanel;
using Microsoft.Xna.Framework.Input;

namespace Torlando.SquadTracker
{
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class Module : Blish_HUD.Modules.Module
    {
        private const string MODULE_FOLDER_NAME = "squadtracker";

        private static readonly Logger Logger = Logger.GetLogger<Module>();

        private PlayersManager _playersManager;
        private SquadManager _squadManager;
        private PlayerIconsManager _playerIconsManager;
        private ObservableCollection<Role> _customRoles;
        private Handler _bridgeHandler;
        private SquadInterfaceView _squadInterfaceView;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion


        #region Controls
        private WindowTab _newTab;

        #endregion

        private SettingEntry<bool> _areColorIconsEnabled; //todo: remove after refactor
        public static SettingEntry<Point> _settingSquadInterfaceLocation;
        public static SettingEntry<Point> _settingSquadInterfaceSize;
        public static SettingEntry<bool> _settingSquadInterfaceMoving;
        public static SettingEntry<bool> _settingSquadInterfaceEnable;
        private AsyncTexture2D _squadTileTexture;

        public SettingEntry<KeyBinding> ToggleSquadInterface { get; private set; }
        private bool _squadInterfaceShouldShow = false;

        [ImportingConstructor]
        public Module([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

        /// <summary>
        /// Define the settings you would like to use in your module.  Settings are persistent
        /// between updates to both Blish HUD and your module.
        /// </summary>
        protected override void DefineSettings(SettingCollection settings)
        {
            _areColorIconsEnabled = settings.DefineSetting(
                "EnableColorIcons", 
                true, () => "Enable Color Icons", 
                () => "When enabled, replaces the monochrome icons with icons colored to match their profession color"
            );
            _settingSquadInterfaceLocation = settings.DefineSetting(
                "SquadInterfaceLocation",
                new Point(100, 100), () => "SquadInterface Location.",
                () => ""
            );
            _settingSquadInterfaceLocation.SettingChanged += UpdateSquadInterfaceLocation;
            _settingSquadInterfaceSize = settings.DefineSetting(
                "SquadInterfaceSize",
                new Point(100, 250), () => "SquadInterface Size.",
                () => ""
            );
            _settingSquadInterfaceSize.SettingChanged += UpdateSquadInterfaceSize;
            
            _settingSquadInterfaceEnable = settings.DefineSetting(
                "EnableSquadInterface",
                false, () => "Enable SquadInterface",
                () => "SquadInterface to be enabled or not."
            );
            _settingSquadInterfaceEnable.SettingChanged += EnableSquadInterface;

            _settingSquadInterfaceMoving = settings.DefineSetting(
                "EnableSquadInterfaceDrag",
                false, () => "Enable SquadInterface Moving",
                () => "SquadInterface can be moved when enabled."
            );
            _settingSquadInterfaceMoving.SettingChanged += UpdateSquadInterfaceMoving;

            ToggleSquadInterface = settings.DefineSetting(
                "ToggleSquadInterface",
                new KeyBinding(ModifierKeys.Shift | ModifierKeys.Ctrl, Keys.P),
                () => "Toggle SquadInterface Visibility",
                () => "Set keybind to toggle the SquadInterface."
            );
            ToggleSquadInterface.Value.BlockSequenceFromGw2 = true;
            ToggleSquadInterface.Value.Enabled = true;
            ToggleSquadInterface.Value.Activated += delegate
            {
                if (_settingSquadInterfaceEnable.Value)
                {
                    _squadInterfaceShouldShow = !_squadInterfaceView.Visible;
                    _squadInterfaceView.Visible = !_squadInterfaceView.Visible;
                }
            };
        }

       

        /// <summary>
        /// Allows your module to perform any initialization it needs before starting to run.
        /// Please note that Initialize is NOT asynchronous and will block Blish HUD's update
        /// and render loop, so be sure to not do anything here that takes too long.
        /// </summary>
        protected override void Initialize()
        {
            
        }

        /// <summary>
        /// Load content and more here. This call is asynchronous, so it is a good time to run
        /// any long running steps for your module including loading resources from file or ref.
        /// </summary>
        protected override async Task LoadAsync()
        {
            await LoadRoles();
            _playerIconsManager = new PlayerIconsManager(this.ContentsManager, _areColorIconsEnabled);
            _squadTileTexture = ContentsManager.GetTexture("textures/squadtile.png");
        }


        private async Task LoadRoles()
        {
            // Throws if the squadtracker folder does not exists, but Blish
            // HUD creates it from the manifest so it's probably okay!
            var directoryName = DirectoriesManager.RegisteredDirectories.First(directoryName => directoryName == MODULE_FOLDER_NAME);
            var directoryPath = DirectoriesManager.GetFullDirectoryPath(directoryName);

            _customRoles = await RolesPersister.LoadRolesFromFileSystem(directoryPath);

            foreach (var role in _customRoles)
            {
                if (!string.IsNullOrEmpty(role.IconPath))
                {
                    try
                    {
                        if (role.IconPath.StartsWith("icons"))
                        {
                            role.Icon = ContentsManager.GetTexture(role.IconPath);
                        }
                        else
                        {
                            if (File.Exists(role.IconPath))
                            {
                                using var textureStream = File.Open(role.IconPath, FileMode.Open);
                                if (textureStream != null)
                                {
                                    Logger.Debug("Successfully loaded texture {dataReaderFilePath}.", role.IconPath);
                                    role.Icon = TextureUtil.FromStreamPremultiplied(GameService.Graphics.GraphicsDevice, textureStream);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Warn($"Could not load texture {role.IconPath}: {e.Message}");
                    }
                }

                if (role.Icon == null)
                {
                    role.Icon = RoleIconCreator.GenerateIcon(role.Name);
                }
            }
        }

        /// <summary>
        /// Allows you to perform an action once your module has finished loading (once
        /// <see cref="LoadAsync"/> has completed).  You must call "base.OnModuleLoaded(e)" at the
        /// end for the <see cref="Module.ModuleLoaded"/> event to fire.
        /// </summary>
        protected override void OnModuleLoaded(EventArgs e)
        {
            _squadInterfaceView = new SquadInterfaceView(_playerIconsManager, _customRoles, _squadTileTexture)
            {
                Parent = GameService.Graphics.SpriteScreen
            };
            UpdateSquadInterfaceLocation();
            UpdateSquadInterfaceSize();
            UpdateSquadInterfaceMoving();
            EnableSquadInterface();

            _bridgeHandler = new Handler();
            _playersManager = new PlayersManager(_bridgeHandler);
            _squadManager = new SquadManager(_playersManager, _squadInterfaceView);

            // If already added role gets deleted, remove it from players.
            _customRoles.CollectionChanged += (sender, e) => {
                var players = _squadManager.GetSquad().CurrentMembers;
                for (int i = 0; i < players.Count; ++i)
                {
                    var player = players.ElementAt(i);
                    List<Role> roles = player.Roles.ToList();
                    foreach (Role role in roles)
                    {
                        if (!_customRoles.Contains(role))
                        {
                            player.RemoveRole(role);
                        }
                    }
                }
            };

            _newTab = GameService.Overlay.BlishHudWindow.AddTab(
                icon: ContentsManager.GetTexture(@"textures\commandertag.png"),
                viewFunc: () => {
                    var view = new MainScreenView();
                    var presenter = new MainScreenPresenter(view, _playersManager, _squadManager, _playerIconsManager, _customRoles);
                    return view.WithPresenter(presenter);
                },
                name: "Squad Tracker Tab"
            );

            _squadManager.SetBridgeConnectionStatus(false);
            _bridgeHandler.OnConnectionUpdate += (connected) => _squadManager.SetBridgeConnectionStatus(connected);

            Handler.Subscribe sub = new Handler.Subscribe() { Squad = true };
            _bridgeHandler.Start(sub);

            // Base handler must be called
            base.OnModuleLoaded(e);

            #if DEBUG
            GameService.Overlay.BlishHudWindow.Show();
            #endif
        }

        protected override void Update(GameTime gameTime)
        {
            if (_settingSquadInterfaceEnable.Value)
            {
                if (GameService.GameIntegration.Gw2Instance.IsInGame && !GameService.Gw2Mumble.UI.IsMapOpen && _squadInterfaceShouldShow)
                    _squadInterfaceView.Show();
                else
                    _squadInterfaceView.Hide();
            }
        }

        // happens when you disable the module
        protected override void Unload()
        {
            if (_bridgeHandler != null)
                _bridgeHandler.Stop();
            GameService.Overlay.BlishHudWindow.RemoveTab(_newTab);
        }

        private void UpdateSquadInterfaceLocation(object sender = null, ValueChangedEventArgs<Point> e = null)
        {
            _squadInterfaceView.Location = _settingSquadInterfaceLocation.Value;
        }

        private void UpdateSquadInterfaceSize(object sender = null, ValueChangedEventArgs<Point> e = null)
        {
            _squadInterfaceView.Size = _settingSquadInterfaceSize.Value;
        }

        private void UpdateSquadInterfaceMoving(object sender = null, ValueChangedEventArgs<bool> e = null)
        {
            _squadInterfaceView.EnableMoving = _settingSquadInterfaceMoving.Value;
        }

        private void EnableSquadInterface(object sender = null, ValueChangedEventArgs<bool> e = null)
        {
            _squadInterfaceView.Visible = _settingSquadInterfaceEnable.Value;
            _squadInterfaceShouldShow = _squadInterfaceView.Visible;
        }
    }

}
