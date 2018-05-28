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
    public class Contact
    {
        [PrimaryKey]
        public string Phone { get; set; }
        public int Id { get; set; }
        public long AccessHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Status { get; set; }
        public bool Blocked { get; set; }
    }
}