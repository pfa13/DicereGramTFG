using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
using DummyXamarin.Utils;
using Java.Util;

namespace DummyXamarin
{
    [Activity(Label = "Contactos_Nombre")]
    public class ContactosNombre : Activity, GestureDetector.IOnGestureListener, IRecognitionListener, TextToSpeech.IOnInitListener
    {
        Intent service;
        string LOG_TAG = "VoiceRecognitionActivity";
        public string textToSpeak = "Diga el nombre del contacto que quiere buscar";
        int _swipeThresold = 100, _swipeVelocityThresold = 100, countSpeech = 0;
        bool record = true;
        Voice v = null;

        TextToSpeech toSpeech = null;
        SpeechRecognizer speechReco;
        Intent intentReco;
        GestureDetector gestureDetector;

        SQLiteRepository database;
        ContactRepository contactRepository;
        ConfigRepository configRepository;
        Models.Config configuracion;
        Contact contactSearch;
        ErrorText errorText;

        TextView nombre;
        TextView telefono;
        TextView estado;
        TextView bloqueado;
        string accion = "buscar";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.ContactosNombre);
            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);
            
            nombre = FindViewById<TextView>(Resource.Id.textView1);
            telefono = FindViewById<TextView>(Resource.Id.textView2);
            estado = FindViewById<TextView>(Resource.Id.textView3);
            bloqueado = FindViewById<TextView>(Resource.Id.textView4);

            service = new Intent(this, typeof(ReceiveService));
            if (!IsMyServiceRunning(service))
                StartService(service);
        }

        protected override void OnResume()
        {
            base.OnResume();            

            database = new SQLiteRepository();
            contactRepository = new ContactRepository(database);
            configRepository = new ConfigRepository(database);
            configuracion = configRepository.GetConfig();
            errorText = new ErrorText();

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
                countSpeech = 0;
                textToSpeak = "Lo siento, no le he entendido. ¿Quiere buscar un contacto, no hacer nada o volver atrás?";
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

            int count = 0;
            countSpeech++;
            record = true;
            foreach (string result in matches)
            {
                count++;
                if (count == 1)
                {
                    if (countSpeech == 1)
                    {
                        if (result.ToLower() == "terminar")
                            StartActivity(typeof(MisContactos));
                        else
                        {
                            if (accion == "buscar")
                            {
                                try
                                {
                                    List<Contact> list = contactRepository.GetContactsByName(result);
                                    if (list.Count == 0)
                                    {
                                        countSpeech = 0;
                                        textToSpeak = "Lo siento, no existe ese contacto. Por favor, diga el nombre de un contacto válido o si no desea buscar un contacto, diga terminar.";
                                        toSpeech = new TextToSpeech(this, this);
                                    }
                                    else if (list.Count > 1)
                                    {
                                        string hablar = $"Existen {list.Count} contactos que contienen ese nombre: ";
                                        for (int i = 0; i < list.Count; i++)
                                        {
                                            if (i == list.Count - 1)
                                                hablar += $"y {list[i].FirstName} {list[i].LastName}. ";
                                            else
                                                hablar += $"{list[i].FirstName} {list[i].LastName}, ";
                                        }
                                        hablar += "Diga el nombre del contacto que quiere buscar";
                                        textToSpeak = hablar;
                                        toSpeech = new TextToSpeech(this, this);
                                    }
                                    else if (list.Count == 1)
                                    {
                                        contactSearch = list[0];
                                        SearchContact(contactSearch);
                                    }
                                }
                                catch(Exception ex)
                                {
                                    countSpeech = 0;
                                    textToSpeak = "Ha ocurrido un error al acceder a la base de datos. Por favor, inténtelo más tarde. " +
                                        "¿Quiere buscar un contacto, no hacer nada o volver atrás?";
                                    toSpeech = new TextToSpeech(this, this);
                                }
                            }
                            else
                            {
                                if (result.ToLower().Contains("buscar"))
                                {
                                    countSpeech = 0;
                                    accion = "buscar";
                                    textToSpeak = "Diga el nombre del contacto que quiere buscar";
                                    toSpeech = new TextToSpeech(this, this);
                                }
                                else if (result.ToLower().Contains("nada")) { }
                                else if (result.ToLower().Contains("atras") || result.ToLower().Contains("atrás") || result.ToLower().Contains("volver"))
                                {
                                    StopItems();
                                    StartActivity(typeof(MisContactos));
                                }
                                else
                                {
                                    countSpeech = 0;
                                    accion = "no buscar";
                                    textToSpeak = "Lo siento, no ha dicho una opción válida. ¿Quiere buscar otro contacto, no hacer nada o volver atrás?";
                                    toSpeech = new TextToSpeech(this, this);
                                }
                            }
                        }
                    }
                    else if (countSpeech == 2)
                    {
                        if (contactSearch == null)
                        {
                            try
                            {
                                contactSearch = contactRepository.GetContactByName(result);
                                if (contactSearch != null)
                                    SearchContact(contactSearch);
                                else
                                {
                                    countSpeech = 0;
                                    textToSpeak = "Lo siento, no existe ese contacto. Por favor, diga el nombre de un contacto válido o si no desea buscar un contacto, diga terminar.";
                                    toSpeech = new TextToSpeech(this, this);
                                }
                            }
                            catch(Exception ex)
                            {
                                countSpeech = 0;
                                textToSpeak = "Ha ocurrido un error al acceder a la base de datos. Por favor, inténtelo más tarde. " +
                                    "¿Quiere buscar un contacto, no hacer nada o volver atrás?";
                                toSpeech = new TextToSpeech(this, this);
                            }
                        }
                    }
                }
            }
        }

        private void SearchContact(Contact contact)
        {
            countSpeech = 0;
            accion = "no buscar";
            nombre.Text = contact.FirstName + " " + contact.LastName;
            telefono.Text = contact.Phone;
            estado.Text = contact.Status;
            bloqueado.Text = contact.Blocked ? "Bloqueado" : "No bloqueado";
            string texto = contact.Blocked ? "Está bloqueado." : "No está bloqueado.";
            textToSpeak = $"El contacto buscado es: {contact.FirstName} {contact.LastName}. Teléfono: ";
            for (int i = 0; i < telefono.Text.Length; i++)
            {
                if (i == 1)
                    textToSpeak += $"{telefono.Text.Substring(0, 2)} ";
                else if (i > 1)
                    textToSpeak += $"{telefono.Text.Substring(i, 1)} ";
            }
            textToSpeak += $". Estado: {contact.Status}. {texto} ¿Quiere buscar otro contacto, no hacer nada o volver atrás?";
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