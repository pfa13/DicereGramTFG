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

namespace DummyXamarin.Models
{
    public class User
    {
        [PrimaryKey]
        public string Phone { get; set; }
        public string Username { get; set; }
        public string Hash { get; set; }
        public string Code { get; set; }
        public long? AccessHash { get; set; }
        public DateTime SessionExpires { get; set; }
    }
}