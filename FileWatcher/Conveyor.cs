using FileWatcher.entity;
using Fleck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileWatcher
{
    public static class C
    {
        public static Dictionary<string, Watcher> dirs = new Dictionary<string, Watcher>();
        public static Dictionary<Watcher, List<Listener>> watchers = new Dictionary<Watcher, List<Listener>>();
        public static Queue<Message> send_messages = new Queue<Message>();
        public static Dictionary<IWebSocketConnection, List<Watcher>> connections = new Dictionary<IWebSocketConnection, List<Watcher>>();
    }
}
