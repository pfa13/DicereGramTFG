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
using TLSharp.Core;
using TLSharp.Core.Network;

namespace DummyXamarin
{
    public class TLClient : TelegramClient
    {
        public Session Session { get { return new FakeSessionStore().Load("session"); } }

        public TLClient(int apiId, string apiHash, ISessionStore store = null, string sessionUserId = "session", TcpClientConnectionHandler handler = null) : base(apiId, apiHash, store, sessionUserId, handler)
        {
        }

    }
}