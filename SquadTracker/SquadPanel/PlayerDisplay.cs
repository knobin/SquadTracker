using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Torlando.SquadTracker.Constants;
using Torlando.SquadTracker.RolesScreen;

namespace Torlando.SquadTracker.SquadPanel
{
    internal class PlayerDisplay : DetailsButton
    {
        public Dropdown RoleDropdown { get; private set; }
        public string CharacterName 
        { 
            get 
            { 
                return _characterName; 
            } 
            set 
            {
                _characterName = value;
                UpdateText();
            } 
        }
        private string _characterName;
        public string AccountName 
        { 
            get 
            { 
                return _accountName; 
            }
            set
            {
                _accountName = value;
                UpdateText();
            } 
        }
        private string _accountName;

        public uint Profession { get; set; }
        public uint Specialization { get; set; }
        public byte Role { get; set; } = 5;
        public bool IsSelf { get; set; }= false;
        public bool IsInInstance { get; set; } = false;

        public uint Subgroup
        {
            get
            {
                return _subgroup;
            }
            set
            {
                _subgroup = value;
                UpdateText();
            }
        }
        private uint _subgroup = 0;

        private void UpdateText()
        {
            if (_characterName != "")
                Text = $"{_characterName} ({_accountName})\nSubgroup: {_subgroup}";
            else
                Text = $"{_accountName}\nSubgroup: {_subgroup}";
        }

        public ref List<RoleImage> RoleIcons()
        {
            return ref _roleIcons;
        }
        private List<RoleImage> _roleIcons = new List<RoleImage>();

        public delegate void RoleRemovedHandler(PlayerDisplay display, Role role);
        public event RoleRemovedHandler OnRoleRemove;

        private const string _placeholderRoleName = Placeholder.DefaultRole;

        public PlayerDisplay(IEnumerable<Role> availableRoles) : base()
        {
            IconSize = DetailsIconSize.Small;
            ShowVignette = true;
            HighlightType = DetailsHighlightType.LightHighlight;
            ShowToggleButton = false;
            Size = new Point(354, 90);
            RoleDropdown = CreateDropdown(availableRoles);
        }

        //ToDo - KnownCharacters
        public void UpdateToolTip(string text)
        {
            Tooltip.BasicTooltipText = text;
        }

        protected override void DisposeControl()
        {
            RoleDropdown.Parent = null;
            RoleDropdown.Dispose();
            RoleDropdown = null;

            foreach (var icon in _roleIcons)
                icon.Dispose();

            _roleIcons = null;
            OnRoleRemove = null;

            base.DisposeControl();
        }

        private Dropdown CreateDropdown(IEnumerable<Role> roles)
        {
            var dropdown = new Dropdown
            {
                Parent = this,
                Width = 135,
                Visible = true
            };
            dropdown.Location = new Point(this.Width - 135 - 10, dropdown.Location.Y);
            dropdown.Items.Add(_placeholderRoleName);
            foreach (var role in roles.OrderBy(role => role.Name.ToLowerInvariant()))
            {
                dropdown.Items.Add(role.Name);
            }
            dropdown.ValueChanged += delegate
            {
                dropdown.SelectedItem = _placeholderRoleName;
            };
            return dropdown;
        }

        private RoleImage CreateRoleImage(string name, Role role)
        {
            var rImage = new RoleImage(role)
            {
                Parent = this,
                Size = new Point(27, 27),
                Texture = role.Icon
            };
            rImage.OnRemoveClick += OnRemoveClick;
            return rImage;
        }

        private void OnRemoveClick(Role role)
        {
            OnRoleRemove?.Invoke(this, role);
        }

        public void UpdateRoles(IEnumerable<Role> availableRoles, IEnumerable<Role> roles)
        {
            RoleDropdown.Items.Clear();
            RoleDropdown.Items.Add(_placeholderRoleName);
            var selectable = availableRoles.Except(roles);

            foreach (var role in selectable.OrderBy(r => r.Name.ToLowerInvariant()))
            {
                var rImage = _roleIcons.Find(i => i.SetRole.Name == role.Name);
                if (rImage != null)
                {
                    rImage.Parent = null;
                    rImage.Dispose();
                    _roleIcons.Remove(rImage);
                }
                RoleDropdown.Items.Add(role.Name);
            }
            
            foreach (var role in roles.OrderBy(r => r.Name.ToLowerInvariant()))
            {
                var rImage = _roleIcons.Find(i => i.SetRole.Name == role.Name);
                if (rImage == null)
                {
                    RoleImage icon = CreateRoleImage(role.Name, role);
                    _roleIcons.Add(icon);
                }
            }

            AlignBottom();
        }

        private void AlignBottom()
        {
            var icons = _roleIcons.OrderBy(r => r.SetRole.Name).ToList();

            const int bottomHeight = 35;
            var point = new Point(10, 0);
            foreach (var icon in icons)
            {
                point.Y = (bottomHeight - icon.Height) / 2;
                icon.Location = point;
                point.X += icon.Width + 3;
                icon.BasicTooltipText = icon.SetRole.Name;
            }
            point.X = this.Width - 135 - 10;
            point.Y = (bottomHeight - RoleDropdown.Height) / 2;
            RoleDropdown.Location = point;

            //List<Control> list = _children.ToList();
            //list.Sort((c1, c2) => { return c1.Location.X.CompareTo(c2.Location.X); });
            //_children = new ControlCollection<Control>(list);
        }
    }

    internal class RoleImage : Image
    {
        public Role SetRole { get; private set; }
        public delegate void RemoveClickHandler(Role role);
        public event RemoveClickHandler OnRemoveClick;

        private ContextMenuStrip _menu = null;

        public RoleImage(Role role)
        {
            SetRole = role;
        }

        protected override void DisposeControl()
        {
            _menu?.Dispose();
            base.DisposeControl();
        }

        protected override void OnRightMouseButtonPressed(MouseEventArgs e)
        {
            _menu?.Dispose();

            _menu = new ContextMenuStrip();

            var items = new List<ContextMenuStripItem>();

            var remove = new ContextMenuStripItem("Remove " + SetRole.Name);
            remove.Click += (sender, args) => {
                OnRemoveClick?.Invoke(SetRole);
            };

            items.Add(remove);
            _menu.AddMenuItems(items);
            _menu.Show(Input.Mouse.Position);

            base.OnRightMouseButtonPressed(e);
        }
    };
}
