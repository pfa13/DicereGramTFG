using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using System.Timers;

namespace DummyXamarin.Utils
{
    [Service(Enabled = true)]
    public class ReceiveService : Service
    {
        Timer t;
        ISharedPreferences sessionPref;

        public override void OnCreate()
        {
            base.OnCreate();            
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }        

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            base.OnStartCommand(intent, flags, startId);

            DoWork();

            return StartCommandResult.RedeliverIntent;
        }

        public void DoWork()
        { 
            t = new Timer();
            t.Interval = (double)120000; // specify interval time as you want            
            t.Elapsed += OnTimedEvent;
            t.Enabled = true;
            t.AutoReset = true;              
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            SendBroadcast(new Intent(this, typeof(GetMessages)));
        }        
        
        public override void OnDestroy()
        {
            base.OnDestroy();
            t.Stop();
            t.Dispose();
            sessionPref = GetSharedPreferences("logout", FileCreationMode.Private);
            string p = sessionPref.GetString("exit", "");
            if (!p.Equals("yes"))
            {
                SendBroadcast(new Intent(this, typeof(ReceiveMessageService)));
            }                       
        }

        public override void OnTaskRemoved(Intent rootIntent)
        {
            base.OnTaskRemoved(rootIntent);
            sessionPref = GetSharedPreferences("logout", FileCreationMode.Private);
            string p = sessionPref.GetString("exit", "");
            if (!p.Equals("yes"))
            {
                SendBroadcast(new Intent(this, typeof(ReceiveMessageService)));
            }
        }        

        
    }
}