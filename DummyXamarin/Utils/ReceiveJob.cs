using System.Threading.Tasks;
using Android.App;
using Android.App.Job;
using Android.Content;
using DummyXamarin.Repositories;
using DummyXamarin.Services;
using TeleSharp.TL;

namespace DummyXamarin.Utils
{
    [Service(Permission = "android.permission.BIND_JOB_SERVICE")]
    public class ReceiveJob : JobService
    {
        private TLClient client;
        private TLUser usuario;
        private SQLiteRepository database;
        private ContactRepository contactRepository;
        private UserRepository userRepository;
        private MessageRepository messageRepository;

        private LoginService loginService;
        private MessageService messageService;

        public override bool OnStartJob(JobParameters @params)
        {
            Task.Run(() =>
            {
                // Work is happening asynchronously
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

                //if (messageCount > 0)
                //    Notification(messageCount.ToString());

                // Have to tell the JobScheduler the work is done. 
                JobFinished(@params, false);
            });

            // Return true because of the asynchronous work
            return false;
        }

        public override bool OnStopJob(JobParameters @params)
        {
            // we don't want to reschedule the job if it is stopped or cancelled.
            return true;
        }

        public override void OnTaskRemoved(Intent rootIntent)
        {
            base.OnTaskRemoved(rootIntent);
        }
    }
}