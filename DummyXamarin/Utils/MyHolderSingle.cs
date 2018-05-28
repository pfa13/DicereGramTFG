using Android.Views;
using Android.Widget;

namespace DummyXamarin.Utils
{
    public class MyHolderSingle
    {
        public LinearLayout Linear;
        public TextView MsgText;
        public TextView DateText;

        public MyHolderSingle(View v)
        {
            this.Linear = v.FindViewById<LinearLayout>(Resource.Id.linearsinglechatitem);
            this.MsgText = v.FindViewById<TextView>(Resource.Id.received);
            this.DateText = v.FindViewById<TextView>(Resource.Id.datereceived);            
        }

        public void AlignRight()
        {
            Linear.SetGravity(GravityFlags.Right);
        }

        public void AlignLeft()
        {
            Linear.SetGravity(GravityFlags.Left);
        }
    }
}