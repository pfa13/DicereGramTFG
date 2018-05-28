using Android.App;
using Android.Widget;
using Android.OS;
using System;
using Android.Views;
using Android.Speech.Tts;
using Android.Speech;
using Android.Content;
using Android.Runtime;
using Java.Util;
using Android.Util;
using System.Collections.Generic;
using DummyXamarin.Repositories;
using DummyXamarin.Utils;
using System.Threading;
using DummyXamarin.Services;
using TeleSharp.TL;

namespace DummyXamarin
{
    [Activity(Label = "Menu")]
    public class MainActivity : Activity, GestureDetector.IOnGestureListener, IRecognitionListener, TextToSpeech.IOnInitListener
    {
        Intent service;
        string LOG_TAG = "VoiceRecognitionActivity";
        public string textToSpeak = "¿Qué quiere hacer?. ¿Enviar, leer, contactos, configuración, cerrar sesión, o no hacer nada?";
        int _swipeThresold = 100, _swipeVelocityThresold = 100, count = 0;
        Voice v = null;
        bool finished = false, record = true;

        TextToSpeech toSpeech = null;
        SpeechRecognizer speechReco;
        Intent intentReco;
        GestureDetector gestureDetector;

        FakeSessionDelete FakeSessionDelete;
        TLClient client;
        TLUser usuario;
        SQLiteRepository database;
        ConfigRepository configRepo;
        Models.Config configuracion;
        LoginService loginService;
        ErrorText errorText;
        ISharedPreferences logoutPref;

        ImageButton b;
        ImageButton b2;
        ImageButton b3;
        ImageButton b4;
        Button logout;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);                 
            SetContentView(Resource.Layout.Main);
            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);

            database = new SQLiteRepository();
            configRepo = new ConfigRepository(database);
            loginService = new LoginService();
            errorText = new ErrorText();
            FakeSessionDelete = new FakeSessionDelete();
            configuracion = configRepo.GetConfig();

            b = FindViewById<ImageButton>(Resource.Id.Enviar);
            b2 = FindViewById<ImageButton>(Resource.Id.Leer);
            b3 = FindViewById<ImageButton>(Resource.Id.Contactos);
            b4 = FindViewById<ImageButton>(Resource.Id.Configuracion);
            logout = FindViewById<Button>(Resource.Id.logout);

            try
            {
                client = loginService.Connect();
            }
            catch(Exception ex)
            {
                this.FinishAffinity();
            }

            if (client.IsUserAuthorized())
                usuario = client.Session.TLUser;

            service = new Intent(this, typeof(ReceiveService));
            if (!IsMyServiceRunning(service))
                StartService(service);

            SetVisible(true);
        }
        

        protected override void OnResume()
        {
            base.OnResume();

            b.Click += delegate
            {
                StopItems();
                StartActivity(typeof(Enviar));
            };

            b2.Click += delegate
            {
                StopItems();
                StartActivity(typeof(MisChats));
            };

            b3.Click += delegate
            {
                StopItems();
                StartActivity(typeof(MisContactos));
            };

            b4.Click += delegate
            {
                StopItems();
                StartActivity(typeof(Configuracion));
            };

            logout.Click += delegate
            {
                count = 1;
                record = true;
                textToSpeak = "¿Está seguro que quiere cerrar la sesión? Hasta dentro de 2 horas no podrá volverse a loguear con el mismo número de teléfono.";
                toSpeech = new TextToSpeech(this, this);
            };            

            speechReco = SpeechRecognizer.CreateSpeechRecognizer(this.ApplicationContext);
            speechReco.SetRecognitionListener(this);
            intentReco = new Intent(RecognizerIntent.ActionRecognizeSpeech);
            intentReco.PutExtra(RecognizerIntent.ExtraLanguagePreference, "es");
            intentReco.PutExtra(RecognizerIntent.ExtraCallingPackage, this.PackageName);
            intentReco.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelWebSearch);
            intentReco.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
            toSpeech = new TextToSpeech(this, this);
            gestureDetector = new GestureDetector(this);
        }

        private bool IsMyServiceRunning(Intent serviceClass)
        {
            ActivityManager manager = (ActivityManager) GetSystemService(Context.ActivityService);
            foreach (ActivityManager.RunningServiceInfo service in  manager.GetRunningServices(int.MaxValue))
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
                if (record)
                    speechReco.StartListening(intentReco);
                else if (!record && textToSpeak == "Sesión cerrada correctamente")
                    this.FinishAffinity();
                    
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
                textToSpeak = "Lo siento, no le he entendido. ¿Qué quiere hacer?. ¿Enviar, leer, contactos, configuración, cerrar sesión, o no hacer nada?";
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
            Log.Info(LOG_TAG, "onResults");
            IEnumerable<string> matches = results.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            int countOnResults = 0;
            count++;
            record = true;
            foreach (string result in matches)
            {
                countOnResults++;
                if(countOnResults == 1)
                {
                    if(count == 1)
                    {
                        if (result.ToLower().Contains("enviar"))
                        {
                            StopItems();
                            StartActivity(typeof(Enviar));
                        }
                        else if (result.ToLower().Contains("leer"))
                        {
                            StopItems();
                            StartActivity(typeof(MisChats));
                        }
                        else if (result.ToLower().Contains("contactos"))
                        {
                            StopItems();
                            StartActivity(typeof(MisContactos));
                        }
                        else if (result.ToLower().Contains("configuracion") || result.ToLower().Contains("configuración"))
                        {
                            StopItems();
                            StartActivity(typeof(Configuracion));
                        }
                        else if (result.ToLower().Contains("nada")) { }
                        else if (result.ToLower().Contains("cerrar sesión") || result.ToLower().Contains("cerrar sesion"))
                        {
                            textToSpeak = "¿Está seguro que quiere cerrar la sesión? Hasta dentro de 2 horas no podrá volverse a loguear con el mismo número de teléfono.";
                            toSpeech = new TextToSpeech(this, this);
                        }
                        else
                        {
                            textToSpeak = "Lo siento, no ha dicho una opción válida. ¿Qué quiere hacer?. ¿Enviar, leer, contactos, configuración, cerrar sesión o no hacer nada?";
                            toSpeech = new TextToSpeech(this, this);
                        }
                    }
                    else if(count == 2)
                    {
                        if(result.ToLower().Contains("sí") || result.ToLower().Contains("si"))
                        {
                            Logout();                            
                        }
                        else if(result.ToLower().Contains("no"))
                        {
                            count = 0;
                            record = true;
                            textToSpeak = "¿Qué quiere hacer?. ¿Enviar, leer, contactos, configuración, cerrar sesión, o no hacer nada?";
                            toSpeech = new TextToSpeech(this, this);
                        }
                        else
                        {
                            count = 1;
                            record = true;
                            textToSpeak = "¿Está seguro que quiere cerrar la sesión?";
                            toSpeech = new TextToSpeech(this, this);
                        }
                    }
                }
            }
        }

        private void Logout()
        {
            try
            {
                var salir = loginService.Logout(client);
                if (salir)
                {
                    try
                    {
                        record = false;

                        ISharedPreferencesEditor l = logoutPref.Edit();
                        l.PutString("exit", "yes");
                        l.Commit();
                        database.DeleteDataDatabase();
                        FakeSessionDelete.DeleteSession();
                        ISharedPreferencesEditor shared = GetSharedPreferences("userPref", FileCreationMode.Private).Edit();
                        shared.Clear();
                        shared.Commit();
                        StopService(service);
                        textToSpeak = "Sesión cerrada correctamente";
                        toSpeech = new TextToSpeech(this, this);
                    }
                    catch (Exception ex)
                    {
                        textToSpeak = "Ha ocurrido un problema al cerrar la sesión. Por favor, inténtelo más tarde. ¿Qué quiere hacer?. ¿Enviar, leer, contactos, configuración, o no hacer nada?";
                        toSpeech = new TextToSpeech(this, this);
                    }
                }
                else
                {
                    count = 0;
                    record = true;
                    textToSpeak = "Lo siento, ha ocurrido un error cerrando la sesión. Inténtelo más tarde. ¿Qué quiere hacer?. ¿Enviar, leer, contactos, configuración, o no hacer nada?";
                    toSpeech = new TextToSpeech(this, this);
                }
            }
            catch(Exception ex)
            {
                textToSpeak = "Ha ocurrido un problema al cerrar la sesión en Telegram. Por favor, inténtelo más tarde. ¿Qué quiere hacer?. ¿Enviar, leer, contactos, configuración, o no hacer nada?";
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
                            StartActivity(typeof(MisChats));
                        }
                        else // Left
                        {
                            StopItems();
                            StartActivity(typeof(Enviar));
                        }
                    }
                }
                else if (System.Math.Abs(diffY) > _swipeThresold && System.Math.Abs(velocityY) > _swipeVelocityThresold)
                {
                    if (diffY > 0) // Bottom
                    {
                        StopItems();
                        StartActivity(typeof(Configuracion));
                    }
                    else // Top
                    {
                        StopItems();
                        StartActivity(typeof(MisContactos));
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

