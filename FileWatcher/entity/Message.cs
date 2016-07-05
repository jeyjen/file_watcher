using Fleck;
namespace FileWatcher.entity
{
    public class Message
    {
        public IWebSocketConnection connection { get; set; }
        public string message { get; set; }
    }
}
