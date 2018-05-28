using Android.Views;
using Android.Widget;

namespace DummyXamarin.Utils
{
    public class MyHolder
    {
        public TextView NameTxt;
        public TextView DateTxt;
        public TextView MsgText;

        public MyHolder(View v)
        {
            this.NameTxt = v.FindViewById<TextView>(Resource.Id.tlf);
            this.DateTxt = v.FindViewById<TextView>(Resource.Id.date);
            this.MsgText = v.FindViewById<TextView>(Resource.Id.msg);
        }
    }
}