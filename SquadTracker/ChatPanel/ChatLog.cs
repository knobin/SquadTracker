using System.Collections.Generic;
using System.Linq;

namespace Torlando.SquadTracker.ChatPanel
{
    public class ChatLog
    {
        private readonly List<ChatMessageEvent> _messages = new List<ChatMessageEvent>();
        public const int Limit = 100;
        
        public delegate void MessageHandler(ChatMessageEvent message);
        public event MessageHandler OnMessageEvent;

        public void Add(ChatMessageEvent evt)
        {
            if (_messages.Count >=Limit)
                _messages.RemoveAt(0);
            
            _messages.Add(evt);
            OnMessageEvent?.Invoke(_messages.Last());
        }

        public List<ChatMessageEvent> Messages()
        {
            return _messages;
        }
    }
}