using Android.App.Job;
using Android.Content;
using Android.OS;

namespace DummyXamarin.Utils
{
    public static class JobSchedulerHelper
    {
        private static string CHANNEL_ID = "channelId";
        public static JobInfo.Builder CreateJobBuilderUsingJobId<T>(this Context context, int jobId) where T : JobService
        {
            
            PersistableBundle bundle = new PersistableBundle();
            bundle.PutInt(CHANNEL_ID, 989);
            var javaClass = Java.Lang.Class.FromType(typeof(T));
            var componentName = new ComponentName(context, javaClass);
            return new JobInfo.Builder(jobId, componentName)
                .SetPersisted(true)
                .SetPeriodic(120000)
                .SetExtras(bundle)
                .SetRequiredNetworkType(NetworkType.Any);
        }
    }
}