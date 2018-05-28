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
    public class Flood
    {
        [PrimaryKey]
        public DateTime TimeStart { get; set; }
        public DateTime TimeWait { get; set; }
    }
}