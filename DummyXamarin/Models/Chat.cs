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
using SQLiteNetExtensions.Attributes;

namespace DummyXamarin.Models
{
    [Table("Chat")]
    public class Chat
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [ForeignKey(typeof(Contact))]
        public string FromTo { get; set; }
        public bool Send { get; set; }
        public string Mensaje { get; set; }
        public DateTime Created { get; set; }
        public bool Seen { get; set; }
    }
}