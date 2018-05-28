using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Database.Sqlite;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using DummyXamarin.Interfaces.IRepositories;
using DummyXamarin.Models;

namespace DummyXamarin.Repositories
{
    public class ContactRepository : IContactRepository
    {
        ISQLiteRepository _sqlite;

        public ContactRepository(ISQLiteRepository sqlite)
        {
            _sqlite = sqlite;
        }

        public bool InsertContact(Contact contacto)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    connection.InsertOrReplace(contacto);
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                return false;
            }
        }

        public List<Contact> GetContacts()
        {
            try
            {
                using(var connection = _sqlite.GetConnection())
                {
                    var lista = connection.Table<Contact>().OrderBy(x => x.FirstName).ToList();
                    return lista;
                }
            }
            catch(SQLiteException ex)
            {
                return null;
            }
        }

        public List<Contact> GetContactsByName(string name)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    var lista = connection.Query<Contact>("SELECT * FROM Contact WHERE FirstName || ' ' || LastName LIKE '%" + name + "%'");
                    return lista;
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }

        public Contact GetContactByName(string name)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    var lista = connection.Query<Contact>("SELECT * FROM Contact WHERE FirstName || ' ' || LastName LIKE '" + name + "%'");
                    return lista.FirstOrDefault();
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }

        public List<string> GetContactsName()
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    return GetContacts().Select(x => x.FirstName + " " + x.LastName).ToList();
                }
            }
            catch (SQLiteException ex)
            {
                return null;
            }
        }

        public List<string> GetContactsStatus()
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {                    
                    return GetContacts().Select(x => x.FirstName + " " + x.LastName + " - " + x.Status).ToList();
                }
            }
            catch (SQLiteException ex)
            {
                return null;
            }
        }

        public bool UpdateContact(Contact contact)
        {
            try
            {                
                using (var connection = _sqlite.GetConnection())
                {
                    connection.Update(contact);
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }

        public List<Contact> GetBlocked()
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    return connection.Table<Contact>().Where(x => x.Blocked == true).ToList();
                }
            }
            catch (SQLiteException ex)
            {
                return null;
            }
        }

        public List<string> GetBlockedName()
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    return GetBlocked().Select(x => x.FirstName + " " + x.LastName).ToList();
                }
            }
            catch (SQLiteException ex)
            {
                return null;
            }
        }

        public bool DeleteContact(Contact contact)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    connection.Delete(contact);
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }

        public bool DeleteContacts()
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    connection.DeleteAll<Contact>();
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                return false;
            }
        }

        public Contact GetContactById(int id)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    var lista = connection.Query<Contact>("SELECT * FROM Contact WHERE Id = " + id);
                    return lista.FirstOrDefault();
                }
            }
            catch (SQLiteException ex)
            {
                return null;
            }
        }

        public Contact GetContactByPhone(string phone)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    var lista = connection.Query<Contact>($"SELECT * FROM Contact WHERE Phone = '{phone}'");
                    return lista.FirstOrDefault();
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }

        public List<Contact> GetContactsByNameWithChat(string name)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    var lista = connection.Query<Contact>($"SELECT * FROM Contact " +
                                                            $"WHERE FirstName || ' ' || LastName LIKE '%{name}%' " +
                                                            $"AND Phone IN (SELECT FromTo FROM Chat GROUP BY FromTo)");
                    return lista;
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }
    }
}