using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SQLite;

namespace DummyXamarin.Interfaces.IRepositories
{
    public interface ISQLiteRepository
    {
        SQLiteConnection GetConnection();
        SQLiteAsyncConnection GetConnectionAsync();
        bool CreateDatabase();
        bool DeleteDataDatabase();
    }
}