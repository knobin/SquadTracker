using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Torlando.SquadTracker.Constants;
using Torlando.SquadTracker.RolesScreen;

namespace Torlando.SquadTracker.SquadPanel
{
    class PlayerDisplay : DetailsButton
    {
        public Dropdown RoleDropdown { get; set; }
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

        private List<RoleImage> _roleIcons = new List<RoleImage>();

        public delegate void RoleRemovedHandler(Role role);
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

        public RoleImage CreateRoleImage(string name, Role role, Dropdown dropdown)
        {
            var rImage = new RoleImage(name) { Parent = this, Size = new Point(27, 27) };
            rImage.Texture = role.Icon;
            rImage.OnRemoveClick += () => OnRoleRemove?.Invoke(role);
            return rImage;
        }

        public void UpdateRoles(IEnumerable<Role> availableRoles, IEnumerable<Role> roles)
        {
            RoleDropdown.Items.Clear();
            RoleDropdown.Items.Add(_placeholderRoleName);
            var selectable = availableRoles.Except(roles);

            foreach (var role in selectable.OrderBy(r => r.Name.ToLowerInvariant()))
            {
                var rImage = _roleIcons.Find(i => i.Name == role.Name);
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
                var rImage = _roleIcons.Find(i => i.Name == role.Name);
                if (rImage == null)
                {
                    RoleImage icon = CreateRoleImage(role.Name, role, RoleDropdown);
                    _roleIcons.Add(icon);
                }
            }

            AlignBottom();
        }

        private void AlignBottom()
        {

            var icons = _roleIcons.OrderBy(r => r.Name).ToList();

            Point point = new Point(10, RoleDropdown.Location.Y);
            for (int i = 0; i < icons.Count; ++i)
            {
                icons[i].Location = point;
                point.X += icons[i].Width + 3;
                icons[i].BasicTooltipText = icons[i].Name;
            }
            point.X = this.Width - 135 - 10;
            RoleDropdown.Location = point;

            List<Control> list = _children.ToList();
            list.Sort((c1, c2) => { return c1.Location.X.CompareTo(c2.Location.X); });
            _children = new ControlCollection<Control>(list);
        }
    }

    class RoleImage : Image
    {
        public string Name { get;}
        public delegate void RemoveClickHandler();
        public event RemoveClickHandler OnRemoveClick;

        private ContextMenuStrip _menu = null;

        public RoleImage(string name)
        {
            Name = name;
        }

        protected override void OnRightMouseButtonPressed(MouseEventArgs e)
        {
            if (_menu != null)
                _menu.Dispose();

            _menu = new ContextMenuStrip();

            List<ContextMenuStripItem> items = new List<ContextMenuStripItem>();

            ContextMenuStripItem remove = new ContextMenuStripItem("Remove " + Name);
            remove.Click += (sender, args) => {
                OnRemoveClick?.Invoke();
            };

            items.Add(remove);
            _menu.AddMenuItems(items);
            _menu.Show(Input.Mouse.Position);

            base.OnRightMouseButtonPressed(e);
        }
    };
}
