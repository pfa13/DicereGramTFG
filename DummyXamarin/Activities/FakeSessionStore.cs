using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using TLSharp.Core;

namespace DummyXamarin
{
    public class FakeSessionStore : ISessionStore
    {
        public FakeSessionStore() { }

        public Session Load(string sessionUserId)
        {
            string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string file = Path.Combine(documentsPath, sessionUserId + ".dat");
            
            if (!File.Exists(file))
                return (Session)null;

            var buffer = File.ReadAllBytes(file);
            return Session.FromBytes(buffer, this, sessionUserId);
        }

        public void Save(Session session)
        {
            string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string file = Path.Combine(documentsPath, session.SessionUserId + ".dat");
            
            using (FileStream fileStream = new FileStream(file, FileMode.OpenOrCreate))
            {
                byte[] bytes = session.ToBytes();
                fileStream.Write(bytes, 0, bytes.Length);
            }
        }
    }
}