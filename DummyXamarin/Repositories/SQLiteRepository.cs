using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using DummyXamarin.Interfaces.IRepositories;
using DummyXamarin.Models;
using SQLite;

namespace DummyXamarin.Repositories
{
    public class SQLiteRepository : ISQLiteRepository
    {
        private string GetPath()
        {
            var dbName = "diceregram.db3";
            var path = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), dbName);
            return path;
        }

        public SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(GetPath());
        }

        public SQLiteAsyncConnection GetConnectionAsync()
        {
            return new SQLiteAsyncConnection(GetPath());
        }

        public bool CreateDatabase()
        {
            try
            {
                using (var connection = GetConnection())
                {
                    connection.CreateTable<Chat>();
                    connection.CreateTable<Models.Config>();
                    connection.CreateTable<Contact>();
                    connection.CreateTable<User>();
                    connection.CreateTable<Flood>();
                    
                    return true;
                }
            }
            catch(SQLiteException ex)
            {
                return false;
            }
        }

        public bool DeleteDataDatabase()
        {
            try
            {
                using (var connection = GetConnection())
                {
                    connection.DeleteAll<Models.Config>();
                    connection.DeleteAll<Contact>();
                    connection.DeleteAll<User>();
                    connection.DeleteAll<Flood>();
                    connection.DeleteAll<Chat>();
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