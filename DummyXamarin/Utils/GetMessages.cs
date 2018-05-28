using System;

using Android.App;
using Android.Content;
using DummyXamarin.Repositories;
using DummyXamarin.Services;
using TeleSharp.TL;

namespace DummyXamarin.Utils
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    public class GetMessages : BroadcastReceiver
    {
        private TLClient client;
        private TLUser usuario;
        private SQLiteRepository database;
        private ContactRepository contactRepository;
        private UserRepository userRepository;
        private MessageRepository messageRepository;

        private LoginService loginService;
        private MessageService messageService;
        Context context;

        public override void OnReceive(Context context, Intent intent)
        {
            this.context = context;
            Get();
        }

        public void Get()
        {
            int messageCount = 0;

            database = new SQLiteRepository();
            contactRepository = new ContactRepository(database);
            userRepository = new UserRepository(database);
            messageRepository = new MessageRepository(database);

            loginService = new LoginService();
            messageService = new MessageService();

            client = loginService.Connect();
            if (client.IsUserAuthorized())
            {
                usuario = client.Session.TLUser;
            }
            messageCount = messageService.ReceiveMessages(client, messageRepository);

            if (messageCount > 0)
            {
                Notification(messageCount.ToString());
                context.SendBroadcast(new Intent(context, typeof(MisChats.BroadcastMisChats)));
                context.SendBroadcast(new Intent(context, typeof(SingleChat.BroadcastSingle)));
            }
            
        }

        public void Notification(string howMany)
        {
            NotificationManager notificationManager = (NotificationManager) context.GetSystemService(Context.NotificationService);

            var statusBar = notificationManager.GetActiveNotifications();
            var resultIntent = new Intent(context, typeof(MisChats));
            resultIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);

            var pending = PendingIntent.GetActivity(context, 0, resultIntent, PendingIntentFlags.UpdateCurrent);

            var builder = new Notification.Builder(context)
                    .SetContentTitle("DicereGram")
                    .SetContentText(Int32.Parse(howMany) > 1 ? "Tiene" + howMany + " mensajes nuevos" : "Tiene 1 mensaje nuevo")
                    .SetSmallIcon(Resource.Drawable.logodg48)
                    .SetDefaults(NotificationDefaults.All);

            builder.SetAutoCancel(true);
            builder.SetContentIntent(pending);
            var notification = builder.Build();
            notification.Flags = NotificationFlags.AutoCancel;
            notificationManager.Notify(1337, notification);
        }
    }
}