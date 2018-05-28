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
    public class Config
    {
        private bool voz = true;
        private float velocidad = (float)1.0;

        [PrimaryKey]
        public string Phone { get; set; }
        [NotNull]
        public bool Voz
        {
            get { return voz; }
            set { voz = value; }
        }
        [NotNull]
        public float Velocidad {
            get { return velocidad; }
            set { velocidad = value; }
        }
        public string TipoVoz { get; set; }
    }
}