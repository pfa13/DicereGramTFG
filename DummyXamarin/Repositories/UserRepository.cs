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
    public class UserRepository : IUserRepository
    {
        ISQLiteRepository _sqlite;

        public UserRepository(ISQLiteRepository sqlite)
        {
            _sqlite = sqlite;
        }

        public User GetUser()
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    var result = connection.Table<User>();
                    return result.FirstOrDefault();
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }

        public bool InsertUser(User usuario)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    connection.InsertOrReplace(usuario);
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }

        public bool UpdateUser(string username)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    connection.Execute("UPDATE User SET Username = '" + username + "'");
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