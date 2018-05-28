using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using DummyXamarin.Models;
using DummyXamarin.Repositories;

namespace DummyXamarin.Utils
{
    public class MyAdapterSingle : ArrayAdapter
    {
        private Context c;
        private List<Chat> chats;
        private LayoutInflater inflater;
        private int resource;
        private SQLiteRepository database;
        private ContactRepository contactRepository;

        public MyAdapterSingle(Context context, int resource, List<Chat> chats) : base(context, resource, chats)
        {
            this.c = context;
            this.resource = resource;
            this.chats = chats;
            database = new SQLiteRepository();
            contactRepository = new ContactRepository(database);
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

            var t = chats[position].Created.Date == DateTime.Now.Date ? chats[position].Created.ToShortTimeString() : chats[position].Created.ToShortDateString();

            if (chats[position].Send)
            {
                MyHolderSingle holder = new MyHolderSingle(convertView)
                {
                    MsgText = { Text = t, Gravity = GravityFlags.Right, TextSize = 11 },
                    DateText = { Text = chats[position].Mensaje, Gravity = GravityFlags.Right, TextSize = 20 }
                };
                holder.AlignRight();
            }
            else
            {
                MyHolderSingle holder = new MyHolderSingle(convertView)
                {
                    MsgText = { Text = chats[position].Mensaje, Gravity = GravityFlags.Left, TextSize = 20 },
                    DateText = { Text = t, Gravity = GravityFlags.Left, TextSize = 11 }
                };
                holder.AlignLeft();
            }

            convertView.SetBackgroundColor(Color.Black);

            return convertView;
        }

        public void RefreshList(List<Chat> chats)
        {
            this.chats.Clear();
            this.chats = chats;
            NotifyDataSetChanged();
        }
    }
}