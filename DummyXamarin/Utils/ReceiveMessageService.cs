
using Android.App;
using Android.Content;

namespace DummyXamarin.Utils
{
    [BroadcastReceiver(Enabled = true, Exported = true, DirectBootAware = true, Permission = "android.permission.RECEIVE_BOOT_COMPLETED")]
    [IntentFilter(new[] { Intent.ActionBootCompleted, Intent.ActionReboot, Intent.ActionLockedBootCompleted })]
    public class ReceiveMessageService : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            // Start service
            context.StartService(new Intent(context, typeof(ReceiveService)));
        }
    }
}