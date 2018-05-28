using System;
using System.Collections.Generic;
using System.Linq;
using Android.Database.Sqlite;
using DummyXamarin.Interfaces.IRepositories;
using DummyXamarin.Models;
using DummyXamarin.Models.Auxiliares;

namespace DummyXamarin.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        ISQLiteRepository _sqlite;

        public MessageRepository(ISQLiteRepository sqlite)
        {
            _sqlite = sqlite;
        }

        public List<Chat> GetMessages()
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    return connection.Table<Chat>().ToList();
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }

        public List<Chat> GetMessagesByPhoneOrdered(string contactPhone)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    return connection.Query<Chat>("SELECT * FROM Chat WHERE FromTo ='" + contactPhone + "' ORDER BY Created DESC");
                }
            }
            catch (SQLiteException ex)
            {
                return null;
            }
        }

        public List<Chat> GetMessagesByPhone(string contactPhone)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    return connection.Query<Chat>("SELECT * FROM Chat WHERE FromTo ='" + contactPhone + "'");
                }
            }
            catch (SQLiteException ex)
            {
                return null;
            }
        }

        public List<Chat> GetMessagesOrdered()
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    return connection.Query<Chat>(@"SELECT Id, FromTo, Send, Mensaje, max(Created) as Created, Seen 
                                                    FROM Chat
                                                    GROUP BY FromTo
                                                    ORDER BY Created desc");
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }

        public List<Chat> GetMessagesNotReaded()
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    return connection.Query<Chat>(@"SELECT * 
                                                    FROM Chat
                                                    WHERE Seen = 0");
                }
            }
            catch (SQLiteException ex)
            {
                return null;
            }
        }       
        
        public List<Chat> GetMessagesNotReadedOrderedByContact()
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    return connection.Query<Chat>(@"SELECT * 
                                                    FROM Chat
                                                    WHERE Seen = 0
                                                    ORDER BY FromTo, Created");
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }

        public List<CountChats> CountMessagesNotReaded()
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    return connection.Query<CountChats>(@"SELECT FromTo, COUNT(FromTo) as Counter
                                                            FROM Chat
                                                            WHERE Seen = 0
                                                            GROUP BY FromTo");                    
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }

        public bool InsertMessage(Chat mensaje)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    connection.Insert(mensaje);
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                return false;
            }
        }

        public bool InsertChat(Chat mensaje)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    connection.Insert(mensaje);
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }

        public bool CheckExistFromContact(string phone)
        {
            bool result = false;
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    var list = connection.Query<Chat>(@"SELECT *
                                                            FROM Chat
                                                            WHERE FromTo = '" + phone + "'");
                    if (list.Count >= 1)
                        result = true;

                    return result;
                }
            }
            catch (SQLiteException ex)
            {
                return result;
            }
        }

        public List<Chat> GetMessagesByPhoneWithoutSeen(string contactPhone)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    var list = connection.Query<Chat>(@"SELECT *
                                                            FROM Chat
                                                            WHERE FromTo = '" + contactPhone + "'" +
                                                            "AND Seen = 0");                    
                    return list;
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }

        public List<Chat> GetMessagesByPhoneWithoutSeen(string contactPhone, int position)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    var list = connection.Query<Chat>($"SELECT * FROM Chat WHERE FromTo = '{contactPhone}' AND Seen = 0 LIMIT 1 OFFSET {position.ToString()}");
                    return list;
                }
            }
            catch (SQLiteException ex)
            {
                return null;
            }
        }

        public List<Chat> GetMessagesByPhoneAndMessage(string phone, string message)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    var list = connection.Query<Chat>(@"SELECT *
                                                            FROM Chat
                                                            WHERE FromTo = '" + phone + "'" +
                                                            "AND Created >= (SELECT Created FROM Chat WHERE Mensaje LIKE '%" + message + "%' LIMIT 1) ORDER BY Created");
                    return list;
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }

        public List<Chat> GetMessagesByPhoneAndDate(string phone, DateTime date)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    var lista = GetMessages().Where(x => x.FromTo == phone && x.Created >= date).OrderBy(x => x.Created).ToList();
                    return lista;
                }
            }
            catch (SQLiteException ex)
            {
                return null;
            }
        }

        public bool MarkMessagesAsRead(string phone)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    var query = $"UPDATE Chat SET Seen = 1 WHERE FromTo = '{phone}'";
                    var list = connection.Execute(query);
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }

        public List<Chat> GetMessagesByPhoneAndMessage(string phone, string message, int position)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    var list = connection.Query<Chat>(@"SELECT *
                                                            FROM Chat
                                                            WHERE FromTo = '" + phone + "'" +
                                                            "AND Created >= (SELECT Created FROM Chat WHERE Mensaje LIKE '%" + message + "%' LIMIT 1) ORDER BY Created " +
                                                            "LIMIT 10 OFFSET " + position.ToString());
                    return list;
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }

        public List<Chat> GetMessagesByPhoneAndDate(string phone, DateTime date, int position)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    var query = $"SELECT * FROM Chat WHERE FromTo = '{phone}' AND Created >= '{date.ToString("yyyy-MM-dd HH:MM:ss")}' ORDER BY Created " +
                        $"LIMIT 10 OFFSET " + position.ToString();
                    var list = connection.Query<Chat>(query);
                    return list;
                }
            }
            catch (SQLiteException ex)
            {
                return null;
            }
        }

        public bool DeleteChat(Contact contact)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    var query = $"DELETE FROM Chat WHERE FromTo = '{contact.Phone}'";
                    var list = connection.Execute(query);
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }
    }
}