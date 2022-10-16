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
        public static SettingEntry<Point> SquadInterfaceLocation { get; private set; }
        public static SettingEntry<Point> SquadInterfaceSize { get; private set; }
        public static SettingEntry<bool> SquadInterfaceUseTileRoleColors { get; private set; }
        public static SettingEntry<bool> SquadInterfaceMoving { get; private set; }
        public static SettingEntry<bool> SquadInterfaceEnable { get; private set; }
        private AsyncTexture2D _squadTileTexture;

        public SettingEntry<KeyBinding> ToggleSquadInterface { get; private set; }
        private SettingEntry<bool> _squadInterfaceShouldShow { get; set; }

        public static SettingEntry<bool> PlayerWithRoleLeaveNotification { get; private set; }
        public static SettingEntry<bool> KeepPlayerRolesWhenRejoining { get; private set; }

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
            SquadInterfaceLocation = settings.DefineSetting(
                "SquadInterfaceLocation",
                new Point(100, 100), () => "SquadInterface Location.",
                () => ""
            );
            SquadInterfaceSize = settings.DefineSetting(
                "SquadInterfaceSize",
                new Point(100, 250), () => "SquadInterface Size.",
                () => ""
            );
            PlayerWithRoleLeaveNotification = settings.DefineSetting(
                "PlayerWithRoleLeaveNotification",
                false, () => "Enable Notifications for when Player with Role Leaves",
                () => "Enable notifications for when a players with a role leaves the squad."
            );
            KeepPlayerRolesWhenRejoining = settings.DefineSetting(
                "KeepPlayerRolesWhenRejoining",
                true, () => "Keep Assigned Roles when Player Rejoins the Squad",
                () => "Keep the assigned roles after the player left and then later join the squad."
            );
            SquadInterfaceEnable = settings.DefineSetting(
                "EnableSquadInterface",
                false, () => "Enable SquadInterface",
                () => "SquadInterface to be enabled or not."
            );
            SquadInterfaceMoving = settings.DefineSetting(
                "EnableSquadInterfaceDrag",
                false, () => "Enable SquadInterface Dragging",
                () => "SquadInterface can be moved and dragged around when enabled."
            );

            _squadInterfaceShouldShow = settings.DefineSetting(
                "SquadInterfaceShouldShow",
                false
            );
            _squadInterfaceShouldShow.SetDisabled(true);

            SquadInterfaceUseTileRoleColors = settings.DefineSetting(
                "SquadInterfaceUseTileRoleColors",
                true, () => "SquadInterface use role tile colors.",
                () => "Enable tiles in SquadInterface to display role colors."
            );

            ToggleSquadInterface = settings.DefineSetting(
                "ToggleSquadInterface",
                new KeyBinding(ModifierKeys.Shift | ModifierKeys.Ctrl, Keys.P),
                () => "Toggle SquadInterface Visibility",
                () => "Set key bind to toggle the SquadInterface."
            );
            ToggleSquadInterface.Value.BlockSequenceFromGw2 = true;
            ToggleSquadInterface.Value.Enabled = true;

            SquadInterfaceLocation.SettingChanged += UpdateSquadInterfaceLocation;
            SquadInterfaceSize.SettingChanged += UpdateSquadInterfaceSize;
            SquadInterfaceUseTileRoleColors.SettingChanged += UpdateSquadInterfaceTileColor;
            SquadInterfaceEnable.SettingChanged += EnableSquadInterface;
            SquadInterfaceMoving.SettingChanged += UpdateSquadInterfaceMoving;
            ToggleSquadInterface.Value.Activated += UpdateToggleSquadInterface;
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
                for (var i = 0; i < players.Count; ++i)
                {
                    var player = players.ElementAt(i);
                    var roles = player.Roles.ToList();
                    foreach (var role in roles.Where(role => !_customRoles.Contains(role)))
                        player.RemoveRole(role);
                }

                _squadInterfaceView.OnRoleCollectionUpdate();
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

            _bridgeHandler.OnConnectionInfo += (info) => _squadManager.BridgeConnectionInfo(info);
            _bridgeHandler.OnConnectionUpdate += (connected) => _squadManager.SetBridgeConnectionStatus(connected);

            _bridgeHandler.OnConnectionInfo += (info) => Logger.Info("[Bridge Information] CombatEnabled: {}, ExtrasEnabled: {}, ExtrasFound: {}, SquadEnabled: {}", info.CombatEnabled, info.ExtrasEnabled, info.ExtrasFound, info.SquadEnabled);

            var sub = new Subscribe() { Squad = true, Protocol = MessageProtocol.Serial };
            _bridgeHandler.Start(sub);

            // Base handler must be called
            base.OnModuleLoaded(e);

            #if DEBUG
            GameService.Overlay.BlishHudWindow.Show();
            #endif
        }

        protected override void Update(GameTime gameTime)
        {
            if (SquadInterfaceEnable.Value)
            {
                if (GameService.GameIntegration.Gw2Instance.IsInGame && !GameService.Gw2Mumble.UI.IsMapOpen && _squadInterfaceShouldShow.Value)
                    _squadInterfaceView.Show();
                else
                    _squadInterfaceView.Hide();
            }
        }

        // happens when you disable the module
        protected override void Unload()
        {
            _bridgeHandler?.Stop();
            if (_squadInterfaceView != null)
            {
                _squadInterfaceView.Dispose();
                _squadInterfaceView = null;
            }
            
            _playersManager?.Dispose();
            _playersManager = null;
            
            _squadManager?.Dispose();
            _squadManager = null;

            SquadInterfaceLocation.SettingChanged -= UpdateSquadInterfaceLocation;
            SquadInterfaceSize.SettingChanged -= UpdateSquadInterfaceSize;
            SquadInterfaceEnable.SettingChanged -= EnableSquadInterface;
            SquadInterfaceMoving.SettingChanged -= UpdateSquadInterfaceMoving;
            ToggleSquadInterface.Value.Activated -= UpdateToggleSquadInterface;
            
            GameService.Overlay.BlishHudWindow.RemoveTab(_newTab);
        }

        private void UpdateSquadInterfaceLocation(object sender = null, ValueChangedEventArgs<Point> e = null)
        {
            _squadInterfaceView.Location = SquadInterfaceLocation.Value;
        }

        private void UpdateSquadInterfaceSize(object sender = null, ValueChangedEventArgs<Point> e = null)
        {
            _squadInterfaceView.Size = SquadInterfaceSize.Value;
        }

        private void UpdateSquadInterfaceTileColor(object sender = null, ValueChangedEventArgs<bool> e = null)
        {
            _squadInterfaceView.TileColorPreference(SquadInterfaceUseTileRoleColors.Value);
        }

        private void UpdateSquadInterfaceMoving(object sender = null, ValueChangedEventArgs<bool> e = null)
        {
            _squadInterfaceView.EnableMoving = SquadInterfaceMoving.Value;
        }

        private void EnableSquadInterface(object sender = null, ValueChangedEventArgs<bool> e = null)
        {
            _squadInterfaceView.Visible = SquadInterfaceEnable.Value;
            _squadInterfaceShouldShow.Value = _squadInterfaceView.Visible;
        }

        private void UpdateToggleSquadInterface(object sender = null, EventArgs e = null)
        {
            if (SquadInterfaceEnable.Value)
            {
                _squadInterfaceShouldShow.Value = !_squadInterfaceView.Visible;
                _squadInterfaceView.Visible = !_squadInterfaceView.Visible;
            }
        }
    }

}
