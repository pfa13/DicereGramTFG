using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Speech;
using Android.Views;
using Android.Widget;

namespace DummyXamarin.Utils
{
    public class ErrorText
    {
        public string GetErrorText(SpeechRecognizerError errorCode)
        {
            string message = "";
            switch (errorCode)
            {
                case SpeechRecognizerError.Audio:                    
                    message = "Audio recording error";
                    break;
                case SpeechRecognizerError.Client:                    
                    message = "client side error";
                    break;
                case SpeechRecognizerError.InsufficientPermissions:                    
                    message = "insufficient permissions";
                    break;
                case SpeechRecognizerError.Network:                    
                    message = "network error";
                    break;
                case SpeechRecognizerError.NetworkTimeout:                    
                    message = "network timeout";
                    break;
                case SpeechRecognizerError.NoMatch:                    
                    message = "no match";
                    break;
                case SpeechRecognizerError.RecognizerBusy:                    
                    message = "recognitionservice busy";
                    break;
                case SpeechRecognizerError.Server:                    
                    message = "error from server";
                    break;
                case SpeechRecognizerError.SpeechTimeout:                    
                    message = "no speech input";
                    break;
                default:                    
                    message = "didn't understand, please try again";
                    break;
            }
            return message;
        }
    }
}