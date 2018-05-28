using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using DummyXamarin.Models;
using DummyXamarin.Repositories;

namespace DummyXamarin.Utils
{
    class MyAdapterBlocked : ArrayAdapter
    {
        private Context c;
        private List<string> blocked;
        private LayoutInflater inflater;
        private int resource;
        private SQLiteRepository database;

        public MyAdapterBlocked(Context context, int resource, List<string> blocked) : base(context, resource, blocked)
        {
            this.c = context;
            this.resource = resource;
            this.blocked = blocked;
            database = new SQLiteRepository();
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (inflater == null)
            {
                inflater = (LayoutInflater)c.GetSystemService(Context.LayoutInflaterService);
            }

            if (convertView == null)
            {
                convertView = inflater.Inflate(resource, parent, false);
            }

            MyHolderBlocked myHolder = new MyHolderBlocked(convertView)
            {
                BlockedName = { Text = blocked[position] }
            };

            convertView.SetBackgroundColor(Color.Black);

            return convertView;
        }
    }
}