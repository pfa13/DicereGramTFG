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
using DummyXamarin.Models;

namespace DummyXamarin.Interfaces.IRepositories
{
    public interface IUserRepository
    {
        bool InsertUser(User user);
        bool UpdateUser(string username);
        User GetUser();
    }
}