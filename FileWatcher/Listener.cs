using Fleck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileWatcher
{
    public class Listener
    {
        public Listener()
        {
            full_filename = "";
        }

        public IWebSocketConnection connection { get; set; }
        public string filter { get; set; }
        public long offset { get; set; }
        public string full_filename { get; set; }
    }
}
