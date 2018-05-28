using System;
using System.Collections.Generic;
using System.Linq;
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

namespace DummyXamarin
{
    [Activity(Label = "Contactos_Bloqueados")]
    public class ContactosBloqueados : Activity, GestureDetector.IOnGestureListener, IRecognitionListener, TextToSpeech.IOnInitListener
    {
        Intent service;
        string LOG_TAG = "VoiceRecognitionActivity";
        public string textToSpeak = "";
        int _swipeThresold = 100, _swipeVelocityThresold = 100, count = 0;
        string accion;
        bool record = true;
        Voice v = null;

        TextToSpeech toSpeech = null;
        SpeechRecognizer speechReco;
        Intent intentReco;
        GestureDetector gestureDetector;

        TLClient client;
        TLUser usuario;

        SQLiteRepository database;
        ContactRepository contactRepository;
        ConfigRepository configRepo;      
        Models.Config configuracion;
        Contact contacto = null;
        ErrorText errorText;

        ContactService contactService;
        LoginService loginService;

        private MyAdapterBlocked _adapter;        
        ListView lista;
        List<string> listaContactos;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.ContactosBloqueados);
            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);            

            database = new SQLiteRepository();
            contactRepository = new ContactRepository(database);
            contactService = new ContactService();
            loginService = new LoginService();

            service = new Intent(this, typeof(ReceiveService));
            if (!IsMyServiceRunning(service))
                StartService(service);

            errorText = new ErrorText();
            lista = FindViewById<ListView>(Resource.Id.listView1);                        
            listaContactos = contactRepository.GetBlockedName();      
            _adapter = new MyAdapterBlocked(this, Resource.Layout.BloqueadosItem, listaContactos);
            lista.Adapter = _adapter;
        }

        protected override void OnResume()
        {
            base.OnResume();

            configRepo = new ConfigRepository(database);
            configuracion = configRepo.GetConfig();

            if (listaContactos.Count > 0)
            {
                textToSpeak = "Sus contactos bloqueados son: ";
                for (int i = 0; i < listaContactos.Count; i++)
                {
                    if (listaContactos.Count == 1)
                        textToSpeak += $"{listaContactos[i]}. ";
                    else
                    {
                        if (i == (listaContactos.Count - 1))
                            textToSpeak += $"y {listaContactos[i]}. ";
                        else
                            textToSpeak += $"{listaContactos[i]}, ";
                    }
                }
            }
            else
                textToSpeak = "No tienes contactos bloqueados. ";

            textToSpeak += "¿Quiere bloquear un contacto, desbloquear un contacto, no hacer nada o volver atrás?";

            try
            {
                client = loginService.Connect();

                if (client.IsUserAuthorized())
                {
                    usuario = client.Session.TLUser;
                }
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
                    System.Threading.Thread.Sleep(2000);
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
                textToSpeak = "Lo siento, no le he entendido. ¿Quiere bloquear un contacto, desbloquear un contacto, no hacer nada o volver atrás?";
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
            record = true;
            foreach (string result in matches)
            {
                if(countOnResults == 0)
                {
                    if(count == 1)
                    {
                        if(result.ToLower().Contains("bloquear"))
                        {
                            accion = "B";
                            textToSpeak = "Diga el nombre del contacto que quiere bloquear";
                            toSpeech = new TextToSpeech(this, this);
                        }
                        else if(result.ToLower().Contains("desbloquear"))
                        {
                            accion = "D";
                            textToSpeak = "Diga el nombre del contacto que quiere desbloquear";
                            toSpeech = new TextToSpeech(this, this);
                        }
                        else if(result.ToLower().Contains("atras") || result.ToLower().Contains("atrás") || result.ToLower().Contains("volver"))
                        {
                            StopItems();
                            StartActivity(typeof(MisContactos));
                        }
                        else if(result.ToLower().Contains("nada")) { }
                        else if(result.ToLower() == "terminar")
                        {
                            StartActivity(typeof(MisContactos));
                        }
                        else
                        {
                            count = 0;
                            textToSpeak = "Lo siento, no ha dicho una opción válida. ¿Quiere bloquear un contacto, desbloquear un contacto, no hacer nada o volver atrás?";
                            toSpeech = new TextToSpeech(this, this);
                        }
                    }
                    else if(count == 2)
                    {
                        try
                        {
                            List<Contact> list = contactRepository.GetContactsByName(result);
                            if (list.Count == 0)
                            {
                                count = 1;
                                textToSpeak = "Lo siento, no existe ese contacto. Por favor, diga el nombre de un contacto o si no desea bloquear o desbloquear un contacto, diga terminar";
                                toSpeech = new TextToSpeech(this, this);
                            }
                            else if (list.Count > 1)
                            {
                                string hablar = "";
                                if (accion == "B")
                                    hablar = "¿Quiere bloquear ";
                                else if (accion == "D")
                                    hablar = "¿Quiere desbloquear ";
                                for (int i = 0; i < list.Count(); i++)
                                {
                                    if (i != list.Count() - 1)
                                        hablar += " o ";
                                    hablar += $"a {list[i].FirstName} {list[i].LastName}";
                                }
                                hablar += "?";
                                textToSpeak = hablar;
                                toSpeech = new TextToSpeech(this, this);
                            }
                            else if (list.Count == 1)
                            {
                                contacto = list[0];
                                CheckAccion(accion, client, contacto);
                            }
                        }
                        catch(Exception ex)
                        {
                            count = 0;
                            textToSpeak = "Ha ocurrido un problema al acceder a la base de datos. Por favor, inténtelo más tarde. " +
                                "¿Quiere bloquear un contacto, desbloquear un contacto, no hacer nada o volver atrás?";
                            toSpeech = new TextToSpeech(this, this);
                        }
                    }
                    else if(count == 3)
                    {
                        if(contacto == null)
                        {
                            try
                            {
                                contacto = contactRepository.GetContactByName(result);
                                CheckAccion(accion, client, contacto);
                            }
                            catch(Exception ex)
                            {
                                count = 0;
                                textToSpeak = "Ha ocurrido un problema al acceder a la base de datos. Por favor, inténtelo más tarde. " +
                                    "¿Quiere bloquear un contacto, desbloquear un contacto, no hacer nada o volver atrás?";
                                toSpeech = new TextToSpeech(this, this);
                            }
                        }
                    }
                    countOnResults++;
                }
            }
        }

        public void CheckAccion(string accion, TLClient client, Contact contact)
        {
            try
            {
                if (accion == "B")
                {
                    contactService.BlockKnownContact(client, contacto, contactRepository);
                    textToSpeak = "Contacto bloqueado. ";
                }
                else if (accion == "D")
                {
                    contactService.UnblockKnownContact(client, contacto, contactRepository);
                    textToSpeak = "Contacto desbloqueado. ";
                }
                Vibrator vibrator = (Vibrator)this.GetSystemService(Context.VibratorService);
                vibrator.Vibrate(1000);
                count = 0;
                textToSpeak += "¿Quiere bloquear un contacto, desbloquear un contacto, no hacer nada o volver atrás?";
            }
            catch(Exception ex)
            {
                count = 0;
                textToSpeak = "Ha ocurrido un problema bloquear o desbloquear el contacto. Por favor, inténtelo más tarde. " +
                    "¿Quiere bloquear un contacto, desbloquear un contacto, no hacer nada o volver atrás?";
                toSpeech = new TextToSpeech(this, this);
            }
            toSpeech = new TextToSpeech(this, this);
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
                    if (System.Math.Abs(diffX) > _swipeThresold && System.Math.Abs(velocityX) > _swipeVelocityThresold) // Right or left
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