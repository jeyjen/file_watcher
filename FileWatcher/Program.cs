using FileWatcher.entity;
using Fleck;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileWatcher
{
    class Program
    {
        //https://github.com/statianzo/Fleck

        private static void _fsw_Changed(object sender, FileSystemEventArgs e)
        {
            
        }

        static void Main(string[] args)
        {
            
            List<Log> logs = new List<Log>();
            logs.Add(new Log() { directory = @"D:\prjs\_own\RP\FileWatcher\FileWatcher\bin\Debug\logs_1", file_filter = "log*", item_pattern = ".+\r\n" });
            logs.Add(new Log() { directory = @"D:\prjs\_own\RP\FileWatcher\FileWatcher\bin\Debug\logs_2", file_filter = "log*", item_pattern = ".+\r\n" });
            
            // Добавить существующие
            // И удалить не нужные
            var deletedWatchers = new HashSet<string>();
            foreach (var key in C.dirs.Keys)
            {
                deletedWatchers.Add(key);
            }

            // добавление не существующих
            string watcher_id = "";
            foreach (var log in logs)
            {
                watcher_id = log.directory + log.file_filter;
                if (C.dirs.ContainsKey(watcher_id))
                {
                    deletedWatchers.Remove(watcher_id);
                }
                else
                {
                    var watcher = new Watcher(log.directory, log.file_filter, log.item_pattern);
                    C.dirs.Add(watcher_id, watcher);
                    C.watchers.Add(watcher, new List<Listener>());
                }
            }

            // удаление не используемых
            foreach (var removeWatcher in deletedWatchers)
            {
                var watcher = C.dirs[removeWatcher];
                var listeners = C.watchers[watcher];
                foreach (var listener in listeners)
                {
                    listener.connection.Close();
                }
                C.watchers.Remove(watcher);
                C.dirs.Remove(removeWatcher);
            }
            
            
            var server = new WebSocketServer("ws://0.0.0.0:8181");
            server.Start(connection =>
            {
                connection.OnOpen = () =>
                {
                    connect(connection);
                };
                connection.OnClose = () =>
                {
                    disconnect(connection);
                };
                connection.OnMessage = message =>
                {
                    // распарсить сообщение
                    JObject jo = JObject.Parse(message);
                    var dir = jo.Value<string>("dir");
                    var file_filter = jo.Value<string>("file_filter");
                    var filter = jo.Value<string>("filter");
                    set_listener(connection, dir, file_filter, filter);
                };
            });

            //while (true)
            //{
            //    // Проверяет работу потока отправки
            //    // Проверяет работу работу сервера

            //    // проверяет необходимость инициализации конфигурации
            //    Thread.Sleep(3000);
            //}

            Thread t = new Thread(p => {
                sending_to_client();
            });
            t.Start();
            Console.ReadLine();
        }

        private static void RemoveListener(Watcher watcher, Listener listener)
        {
            // var 
            // если больше нет watcher то удалить из коллекции подключений
            // 
        }

        private static void connect(IWebSocketConnection connection)
        {
            C.connections.Add(connection, new List<Watcher>());
        }

        private static void disconnect(IWebSocketConnection connection)
        {
            // Удалить из watcher всех слушателей которые относятся к подключению
            foreach (var watcher in C.connections[connection])
            {
                var listeners = C.watchers[watcher];
                C.watchers[watcher] = listeners.Where(l => l.connection != connection).ToList();
            }
            C.connections.Remove(connection);
        }

        /// <summary>
        /// добавляет или изменяет фильтры слушателей
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="dir"></param>
        /// <param name="file_filter"></param>
        /// <param name="filter"></param>
        public static void set_listener(IWebSocketConnection connection, string dir, string file_filter, string filter)
        {
            var watcher_id = dir + file_filter;
            if (! C.dirs.ContainsKey(watcher_id))
            {
                return;
            }
            var watcher = C.dirs[watcher_id];

            // выбрать из директории файл по фильтру с самой последней датой изменения

            // найти слушателя с таким же connection
            var listeners = C.watchers[watcher];
            Listener listener = null;
            foreach (var item in listeners)
            {
                if (item.connection == connection)
                {
                    listener = item;
                    listener.filter = filter;
                    watcher.SendChangesForListener(listener);
                }
            }
            if (listener == null)
            {
                listener = new Listener() { connection = connection, filter = filter, offset = 0 };
                watcher.SendChangesForListener(listener);
                C.watchers[watcher].Add(listener);
                // запустить отслеживание если не запущено
                if (! watcher.IsAlive)
                {
                    watcher.Start();
                }
            }
            
            if (! C.connections.ContainsKey(connection))
            {
                C.connections.Add(connection, new List<Watcher>());
            }
            C.connections[connection].Add(watcher);
        }

        public static void remove_listener(IWebSocketConnection connection, string dir, string file_filter)
        {
            string watcher_id = dir + file_filter;
            if (! C.dirs.ContainsKey(watcher_id))
            {
                return;
            }
            var watcher = C.dirs[watcher_id];
            var listeners = C.watchers[watcher].Where(l => l.connection != connection).ToList();
            C.watchers[watcher] = listeners;
            // если у watcher нет слушателей, то остановить прослушку
        }

        private static void sending_to_client()
        {
            Message message = null;
            while (true)
            {
                //if (C.send_messages.Count == 0)
                //{
                //    foreach (var con in C.connections.Keys)
                //    {
                //        con.SendPing(new byte[4]);
                //    }
                //}
                while (C.send_messages.Count > 0)
                {
                    message = C.send_messages.Dequeue();
                    message.connection.Send(message.message);
                }
                Thread.Sleep(1000);
            }
        }
    }
}
