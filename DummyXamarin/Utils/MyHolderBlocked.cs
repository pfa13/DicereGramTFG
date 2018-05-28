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

namespace DummyXamarin.Utils
{
    class MyHolderBlocked
    {
        public TextView BlockedName;

        public MyHolderBlocked(View view)
        {
            this.BlockedName = view.FindViewById<TextView>(Resource.Id.blocked);
        }
    }
}