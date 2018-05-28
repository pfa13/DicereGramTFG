using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using DummyXamarin.Models;
using DummyXamarin.Repositories;

namespace DummyXamarin.Utils
{
    public class MyAdapter : ArrayAdapter
    {
        private Context c;
        private List<Chat> chats;
        private LayoutInflater inflater;
        private int resource;
        private SQLiteRepository database;
        private ContactRepository contactRepository;

        public MyAdapter(Context context, int resource, List<Chat> chats) : base(context, resource, chats)
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

            Contact contact = contactRepository.GetContactByPhone(chats[position].FromTo);
            string[] lines = chats[position].Mensaje.Split(new String[] { System.Environment.NewLine }, StringSplitOptions.None);
            var h = chats[position].Created.Date == DateTime.Now.Date ? chats[position].Created.ToShortTimeString() : chats[position].Created.ToShortDateString() + chats[position].Created.ToShortTimeString();

            MyHolder holder = new MyHolder(convertView)
            {
                NameTxt = { Text = contact == null ? chats[position].FromTo : contact.FirstName + " " + contact.LastName },
                DateTxt = { Text = h },
                MsgText = { Text = lines[0].Length > 80 && lines.Count() > 0 ? lines[0].Substring(0,80) + "..." : lines[0] }
            };

            convertView.SetBackgroundColor(Color.Black);

            return convertView;
        }
    }
}