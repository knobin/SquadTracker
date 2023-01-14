using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Graphics.UI;
using Torlando.SquadTracker.RolesScreen;
using Torlando.SquadTracker.SquadPanel;

namespace Torlando.SquadTracker.ChatPanel
{
    internal class ChatPresenter : Presenter<ChatView, object>
    {
        private readonly SquadManager _squadManager;
        private readonly ICollection<Role> _roles; 
        private static readonly Logger Logger = Logger.GetLogger<Module>();
        
        public ChatPresenter(ChatView view, SquadManager squadManager, ICollection<Role> roles) : base(view, null)
        {
            _squadManager = squadManager;
            _roles = roles;
        }

        protected override void UpdateView()
        {
            Logger.Info("Updating ChatPresenter");
            
            var messages = _squadManager.GetChatLog().Messages().ToList();
            foreach (var t in messages)
                HandleChatMessageEvent(t);
            
            messages.Clear();

            _squadManager.GetChatLog().OnMessageEvent += HandleChatMessageEvent;
        }

        protected override void Unload()
        {
            Logger.Info("Unloading ChatPresenter");
            
            _squadManager.GetChatLog().OnMessageEvent -= HandleChatMessageEvent;
        }

        private void HandleChatMessageEvent(ChatMessageEvent evt)
        {
            if (View.Count() >= ChatLog.Limit)
            {
                var diff = View.Count() - ChatLog.Limit + 1;
                for (var i = 0; i < diff; ++i)
                    View.RemoveFirst();
            }

            View.DisplayChatMessage(_squadManager, _roles, evt.AccountName, evt.CharacterName, evt.Subgroup, evt.Timestamp, evt.Text);
        }
    }
}
