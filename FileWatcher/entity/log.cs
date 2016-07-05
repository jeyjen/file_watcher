using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileWatcher.entity
{
    public class Log
    {
        public string directory { get; set; }
        public string file_filter { get; set; }
        public string item_pattern { get; set; }
    }
}
