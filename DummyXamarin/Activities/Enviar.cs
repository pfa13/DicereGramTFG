using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Speech;
using Android.Speech.Tts;
using Android.Util;
using Android.Views;
using Android.Widget;
using DummyXamarin.Models;
using DummyXamarin.Repositories;
using DummyXamarin.Services;
using DummyXamarin.Utils;
using Java.Util;
using TeleSharp.TL;
using TLSharp.Core;

namespace DummyXamarin
{
    [Activity(Label = "Enviar")]
    public class Enviar : Activity, GestureDetector.IOnGestureListener, IRecognitionListener, TextToSpeech.IOnInitListener
    {
        Intent service;
        string LOG_TAG = "VoiceRecognitionActivity";
        public string textToSpeak = "Diga el nombre del contacto a quien quiere enviar un mensaje";
        public int _swipeThresold = 100;
        public int _swipeVelocityThresold = 100;
        public int count = 0;
        public string message;
        public bool userToSend = false, messageToSend = false, record = true;
        Voice v = null;

        IEnumerable<TLUser> users = new List<TLUser>();
        Contact usuarioToSend = null;
        TLClient client;
        TLUser usuario;
        TLUser usertosend = null;

        TextToSpeech toSpeech = null;
        SpeechRecognizer speechReco;
        Intent intentReco;
        GestureDetector gestureDetector;

        SQLiteRepository database;
        MessageRepository messageRepository;
        ContactRepository contactRepository;
        ConfigRepository configRepository;
        Models.Config configuracion;
        ErrorText errorText;
        LoginService loginService;
        MessageService messageService;

        EditText telephoneToSend;
        EditText textToSend;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Enviar);
            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);                                           

            telephoneToSend = FindViewById<EditText>(Resource.Id.editText1);
            textToSend = FindViewById<EditText>(Resource.Id.editText2);

            service = new Intent(this, typeof(ReceiveService));
            if (!IsMyServiceRunning(service))
                StartService(service);
        }

        protected override void OnResume()
        {
            base.OnResume();            

            database = new SQLiteRepository();
            contactRepository = new ContactRepository(database);
            messageRepository = new MessageRepository(database);
            configRepository = new ConfigRepository(database);
            configuracion = configRepository.GetConfig();

            errorText = new ErrorText();
            loginService = new LoginService();
            messageService = new MessageService();

            count = 0;
            try
            {
                client = loginService.Connect();

                if (client.IsUserAuthorized())
                    usuario = client.Session.TLUser;
            }                                 
            catch(Exception ex)
            {
                this.FinishAffinity();
            }

            speechReco = SpeechRecognizer.CreateSpeechRecognizer(this.ApplicationContext);
            speechReco.SetRecognitionListener(this);
            intentReco = new Intent(RecognizerIntent.ActionRecognizeSpeech);
            intentReco.PutExtra(RecognizerIntent.ExtraLanguagePreference, "es");
            intentReco.PutExtra(RecognizerIntent.ExtraCallingPackage, this.PackageName);
            intentReco.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelWebSearch);
            intentReco.PutExtra(RecognizerIntent.ExtraMaxResults, 1);

            gestureDetector = new GestureDetector(this);
            toSpeech = new TextToSpeech(this, this);
        }

        private bool IsMyServiceRunning(Intent serviceClass)
        {
            ActivityManager manager = (ActivityManager)GetSystemService(Context.ActivityService);
            foreach (ActivityManager.RunningServiceInfo service in manager.GetRunningServices(int.MaxValue))
            {
                if (serviceClass.Class.Name.Equals(service.Service.ClassName))
                {
                    return true;
                }
            }
            return false;
        }

        public void OnInit([GeneratedEnum] OperationResult status)
        {
            if (status == OperationResult.Success)
            {
                try
                {
                    var list = toSpeech.Voices;
                    foreach (var item in list)
                    {
                        if (item.Name == configuracion.TipoVoz)
                        {
                            v = item;
                            break;
                        }
                    }
                    if (v != null)
                        toSpeech.SetVoice(v);
                    toSpeech.SetSpeechRate(configuracion.Velocidad);
                    toSpeech.SetLanguage(new Locale("es", "ES"));
                    toSpeech.Speak(textToSpeak, QueueMode.Flush, null);
                    Thread.Sleep(2000);
                }
                catch (Exception e)
                {
                    Toast.MakeText(this, e.Message, ToastLength.Long).Show();
                }
                while (true)
                {
                    if (!toSpeech.IsSpeaking)
                    {
                        Log.Info(LOG_TAG, "he terminado de hablar");
                        toSpeech.Stop();
                        toSpeech.Shutdown();
                        break;
                    }
                }
                
                if(record)
                    speechReco.StartListening(intentReco);
                else if(!record && !textToSpeak.Contains("No hay conexión a Internet"))
                    StartActivity(typeof(MainActivity));
            }
        }

        public void OnBeginningOfSpeech() { }

        public void OnBufferReceived(byte[] buffer) { }

        public void OnEndOfSpeech() { }

        public void OnError([GeneratedEnum] SpeechRecognizerError error)
        {
            string errorMensaje = errorText.GetErrorText(error);
            if(error == SpeechRecognizerError.NoMatch)
            {
                record = true;
                count = 0;
                textToSpeak = "Lo siento, no le he entendido. Por favor, diga el nombre del contacto a quien quiere enviar un mensaje";
                toSpeech = new TextToSpeech(this, this);
            }
            else if (error == SpeechRecognizerError.Network || error == SpeechRecognizerError.NetworkTimeout)
            {
                record = false;
                textToSpeak = "No hay conexión a Internet. Por favor, inténtelo mas tarde.";
            }
        }

        public void OnEvent(int eventType, Bundle @params) { }

        public void OnPartialResults(Bundle partialResults) { }

        public void OnReadyForSpeech(Bundle @params) { }

        public void OnResults(Bundle results)
        {
            count++;
            int countOnResults = 0;            
            Log.Info(LOG_TAG, "onResults");
            IEnumerable<string> matches = results.GetStringArrayList(SpeechRecognizer.ResultsRecognition);

            foreach (string result in matches)
            {
                countOnResults++;
                if(countOnResults == 1)
                {
                    record = true;
                    if (count == 1)
                    {
                        if(result.ToLower() == "terminar")
                        {
                            StartActivity(typeof(MainActivity));
                        }
                        else
                        {
                            try
                            {
                                List<Contact> list = contactRepository.GetContactsByName(result);
                                if (list.Count == 0)
                                {
                                    count = 0;
                                    textToSpeak = "Lo siento, no existe ese contacto. Por favor, diga el nombre de un contacto válido o si no desea enviar un mensaje, diga terminar";
                                    toSpeech = new TextToSpeech(this, this);
                                }
                                else if (list.Count > 1)
                                {
                                    string hablar = $"Existen {list.Count} contactos que contienen ese nombre: ";
                                    for (int i = 0; i < list.Count(); i++)
                                    {
                                        if (i == list.Count() - 1)
                                            hablar += $"y {list[i].FirstName} {list[i].LastName}";
                                        else
                                            hablar += $"{list[i].FirstName} {list[i].LastName}, ";
                                    }
                                    hablar += ". Diga el nombre del contacto a quien quiere enviar el mensaje";
                                    textToSpeak = hablar;
                                    toSpeech = new TextToSpeech(this, this);
                                }
                                else if (list.Count == 1)
                                {
                                    usuarioToSend = list[0];
                                    telephoneToSend.Text = $"{list[0].FirstName} {list[0].LastName}";
                                    textToSpeak = "¿Qué mensaje quiere enviar?";
                                    toSpeech = new TextToSpeech(this, this);
                                }
                            }
                            catch(Exception ex)
                            {
                                record = false;
                                textToSpeak = "Ha ocurrido un error al acceder a la base de datos. Por favor, inténtelo más tarde.";
                                toSpeech = new TextToSpeech(this, this);
                            }
                        }
                    }
                    else if (count == 2)
                    {
                        if(usuarioToSend == null)                        
                        {
                            try
                            {
                                usuarioToSend = contactRepository.GetContactByName(result);
                                telephoneToSend.Text = usuarioToSend.FirstName + " " + usuarioToSend.LastName;
                                textToSpeak = "¿Qué mensaje quiere enviar?";
                                toSpeech = new TextToSpeech(this, this);
                            }
                            catch(Exception ex)
                            {
                                record = false;
                                textToSpeak = "Ha ocurrido un error al acceder a la base de datos. Por favor, inténtelo más tarde.";
                                toSpeech = new TextToSpeech(this, this);
                            }
                        }
                        else
                        {                            
                            message = result;
                            textToSend.Text = message;
                            textToSpeak = $"Su mensaje es: {message}. ¿Quiere enviarlo?";
                            toSpeech = new TextToSpeech(this, this);
                        }
                        
                    }
                    else if (count == 3)
                    {
                        if(string.IsNullOrEmpty(message))
                        {
                            message = result;
                            textToSend.Text = message;
                            textToSpeak = $"Su mensaje es: {message}. ¿Quiere enviarlo?";
                            toSpeech = new TextToSpeech(this, this);
                        }
                        else if (result.ToLower() == "si" || result.ToLower() == "sí")
                        {
                            try
                            {
                                TLAbsUpdates updates = null;
                                updates = messageService.SendMessage(client, usuarioToSend.Id, message);
                                MensajeEnviado(updates, usuarioToSend.Phone, message);
                            }
                            catch(Exception ex)
                            {
                                record = false;
                                textToSpeak = "Ha ocurrido un error al enviar el mensaje. Por favor, inténtelo más tarde.";
                                toSpeech = new TextToSpeech(this, this);
                            }
                        }
                        else if (result.ToLower() == "no")
                        {
                            StartActivity(typeof(MainActivity));
                        }
                    }
                    else if(count == 4)
                    {
                        if (result.ToLower() == "si" || result.ToLower() == "sí")
                        {
                            try
                            {
                                TLAbsUpdates updates = null;
                                updates = messageService.SendMessage(client, usuarioToSend.Id, message);
                                MensajeEnviado(updates, usuarioToSend.Phone, message);
                            }
                            catch(Exception ex)
                            {
                                record = false;
                                textToSpeak = "Ha ocurrido un error al enviar el mensaje. Por favor, inténtelo más tarde.";
                                toSpeech = new TextToSpeech(this, this);
                            }
                        }
                        else if (result.ToLower() == "no")
                        {
                            StartActivity(typeof(MainActivity));
                        }
                    }
                }
            }
        }

        public void MensajeEnviado(TLAbsUpdates updates, string fromPhone, string mensaje)
        {
            record = false;
            if(updates != null)
            {
                try
                {
                    Chat m = new Chat
                    {
                        FromTo = fromPhone,
                        Mensaje = mensaje,
                        Send = true,
                        Created = DateTime.Now,
                        Seen = true
                    };
                    messageRepository.InsertChat(m);
                    Vibrator vibrator = (Vibrator)this.GetSystemService(Context.VibratorService);
                    vibrator.Vibrate(1000);
                    textToSpeak = "Mensaje enviado";
                    toSpeech = new TextToSpeech(this, this);
                }
                catch(Exception ex)
                {
                    textToSpeak = "Ha ocurrido un error al acceder a la base de datos. Por favor, inténtelo más tarde.";
                    toSpeech = new TextToSpeech(this, this);
                }
            }
            else
            {
                textToSpeak = "No se ha podido enviar el mensaje. Inténtelo más tarde";
                toSpeech = new TextToSpeech(this, this);
            }
        }

        public void OnRmsChanged(float v) { }

        public override bool OnTouchEvent(MotionEvent e)
        {
            gestureDetector.OnTouchEvent(e);
            return false;
        }

        public bool OnDown(MotionEvent e) { return false; }

        public bool OnFling(MotionEvent event1, MotionEvent event2, float velocityX, float velocityY)
        {
            bool result = false;
            try
            {
                float diffY = event2.GetY() - event1.GetY();
                float diffX = event2.GetX() - event1.GetX();

                if (System.Math.Abs(diffX) > System.Math.Abs(diffY))
                {
                    if (System.Math.Abs(diffX) > _swipeThresold && System.Math.Abs(velocityX) > _swipeVelocityThresold)
                    {
                        if (diffX > 0) // Right
                        {
                            StopItems();
                            StartActivity(typeof(MainActivity));
                        }
                        else
                        {
                            StopItems();
                            StartActivity(typeof(MainActivity));
                        }                           
                    }
                }
                result = true;
            }
            catch (Exception exception)
            {
                Toast.MakeText(this, exception.Message, ToastLength.Long).Show();
            }
            return result;
        }

        public void OnLongPress(MotionEvent e) { }

        public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY) { return false; }

        public void OnShowPress(MotionEvent e) { }

        public bool OnSingleTapUp(MotionEvent e) { return false; }

        public void StopItems()
        {
            speechReco.Cancel();
            toSpeech.Stop();
            toSpeech.Shutdown();
        }

        protected override void OnDestroy()
        {
            StopItems();
            StopService(service);
            base.OnDestroy();                       
        }
    }
}