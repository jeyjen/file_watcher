using FileWatcher.entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FileWatcher
{
    public class Watcher
    {
        FileSystemWatcher _watcher;
        FileInfo _fileInfo;
        string _item_pattern;

        public bool IsAlive
        {
            get
            {
                return _watcher.EnableRaisingEvents;
            }
        }

        public Watcher(string dir, string file_filter, string item_pattern)
        {
            // определить самый свежий файл по маске
            _item_pattern = item_pattern;
            _watcher = new FileSystemWatcher(dir, file_filter);
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.Size;
            _watcher.IncludeSubdirectories = false;

            _watcher.Changed += new FileSystemEventHandler(_fsw_Changed);
            _watcher.Created += _fsw_Changed;
            _watcher.Deleted += _fsw_Changed;
            _watcher.Renamed += new RenamedEventHandler(_fsw_Renamed);
            _watcher.Error += new ErrorEventHandler(_fsw_Error);

            // получить самый последний измененый файл
            string file = get_last_modified_file(_watcher.Path, _watcher.Filter);
            if (file == null)
                throw new Exception(string.Format("Не найден ни один файл в директории {0} удовлетворяющий фильтру {1}", _watcher.Path, _watcher.Filter));
            _fileInfo = new FileInfo(file);
        }

        private void _fsw_Error(object sender, ErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void _fsw_Renamed(object sender, RenamedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void _fsw_Changed(object sender, FileSystemEventArgs e)
        {
            var listeners = C.watchers[this];
            foreach (var listener in listeners)
            {
                SendChangesForListener(listener);
            }
        }
        
        public void SendChangesForListener(Listener listener)
        {
            /*
                если watcher не запущен
                    указанный файл может быть не актуальным
                иначе 
                    присвоить полный путь watcher

                если offset < file.Lenght
                    получить разницу в данных
                    конвертировать к строке
                    применить парсер watcher
                    для каждого полученного сообщения
                        применить фильтр слушателя
                        если сообщение уовлетворяет условию
                        добавить сообщение для отправки на указанное подключение

            */
            if (IsAlive)
            {
                listener.full_filename = _fileInfo.FullName;
            }
            else
            {
                // если произошла смена названия последнеизмененного файла
                if (listener.full_filename != _fileInfo.FullName)
                {
                    string file = get_last_modified_file(_watcher.Path, _watcher.Filter);
                    if (file == null)
                        throw new Exception(string.Format("Не найден ни один файл в директории {0} удовлетворяющий фильтру {1}", _watcher.Path, _watcher.Filter));
                    _fileInfo = new FileInfo(file);
                }
            }
            _fileInfo.Refresh();
            if (listener.offset < _fileInfo.Length)
            {
                var fs = _fileInfo.OpenRead();
                fs.Seek(listener.offset, SeekOrigin.Begin);
                byte[] deltaData = new byte[fs.Length - listener.offset];
                fs.Read(deltaData, 0, (int)(fs.Length - listener.offset));
                fs.Close();
                listener.offset = _fileInfo.Length;
                string delta = Encoding.Default.GetString(deltaData);

                var messages = Regex.Matches(delta, _item_pattern);
                var value = "";
                foreach (var message in messages)
                {
                    value = ((Match)message).Value;
                    if (string.IsNullOrEmpty(listener.filter) || Regex.IsMatch(value, listener.filter))
                    {
                        C.send_messages.Enqueue(new Message() { connection = listener.connection, message = value });
                    }
                }
            }
        }

        private string get_last_modified_file(string path, string filter)
        {

            var paths = Directory.GetFiles(path, filter);
            string full_filename = "";
            DateTime max_date = DateTime.MinValue;
            foreach (var p in paths)
            {
                if (max_date < File.GetLastWriteTime(p))
                {
                    full_filename = p;
                }
            }
            return full_filename;
        }

        public void Start()
        {
            _watcher.EnableRaisingEvents = true;
        }
        public void Stop()
        {
            _watcher.EnableRaisingEvents = false;
        }
    }
}
