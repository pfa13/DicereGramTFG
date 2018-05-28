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
using DummyXamarin.Services;
using DummyXamarin.Utils;
using Java.Util;
using TeleSharp.TL;

namespace DummyXamarin
{
    [Activity(Label = "Contactos")]
    public class MisContactos : Activity, GestureDetector.IOnGestureListener, IRecognitionListener, TextToSpeech.IOnInitListener
    {
        Intent service;
        string LOG_TAG = "VoiceRecognitionActivity";
        private string textToSpeak = "¿Quiere buscar un contacto, actualizar, eliminar, bloqueados, no hacer nada o volver atrás?";
        int _swipeThresold = 100, _swipeVelocityThresold = 100, countSpeech = 0;
        bool record = true;
        Voice v = null;

        TextToSpeech toSpeech = null;
        SpeechRecognizer speechReco;
        Intent intentReco;
        GestureDetector gestureDetector;

        SQLiteRepository database;
        ContactRepository contactRepository;
        ConfigRepository configRepo;
        Models.Config configuracion;
        Contact deleteContact;
        ContactService contactService;
        LoginService loginService;

        ErrorText errorText;

        TLClient client;
        TLUser usuario;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.MisContactos);
            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);

            database = new SQLiteRepository();
            contactRepository = new ContactRepository(database);
            loginService = new LoginService();
            contactService = new ContactService();
            deleteContact = null;

            errorText = new ErrorText();

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

            Button contactos = FindViewById<Button>(Resource.Id.btnContactos);
            Button estados = FindViewById<Button>(Resource.Id.btnEstados);
            Button bloqueados = FindViewById<Button>(Resource.Id.btnBloquear);
            Button eliminar = FindViewById<Button>(Resource.Id.btnEliminar);

            contactos.Click += delegate
            {
                StopItems();
                StartActivity(typeof(ContactosNombre));
            };

            estados.Click += delegate
            {
                StopItems();
                contactService.UpdateContacts(client, contactRepository);
            };

            bloqueados.Click += delegate
            {
                StopItems();
                StartActivity(typeof(ContactosBloqueados));
            };

            eliminar.Click += delegate
            {
                StopItems();
                contactService.DeleteContact(client, deleteContact, contactRepository);
            };
        }

        protected override void OnResume()
        {
            base.OnResume();

            configRepo = new ConfigRepository(database);
            configuracion = configRepo.GetConfig();

            service = new Intent(this, typeof(ReceiveService));
            if (!IsMyServiceRunning(service))
                StartService(service);

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
                countSpeech = 0;
                textToSpeak = "Lo siento, no le he entendido. ¿Quiere buscar un contacto, actualizar, eliminar, bloqueados, no hacer nada o volver atrás?";
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
                        if (result.ToLower().Contains("buscar"))
                            StartActivity(typeof(ContactosNombre));
                        else if (result.ToLower().Contains("actualizar"))
                        {
                            try
                            {
                                countSpeech = 0;
                                contactService.UpdateContacts(client, contactRepository);
                                Vibrator vibrator = (Vibrator)this.GetSystemService(Context.VibratorService);
                                vibrator.Vibrate(1000);
                                textToSpeak = "Contactos actualizados. ¿Quiere buscar un contacto, actualizar, eliminar, bloqueados, no hacer nada o volver atrás?";
                                toSpeech = new TextToSpeech(this, this);
                            }
                            catch(Exception ex)
                            {
                                countSpeech = 0;
                                textToSpeak = "Ha ocurrido un error al actualizar los contactos. Por favor, inténtelo más tarde. ¿Quiere buscar un contacto, actualizar, eliminar, bloqueados, no hacer nada o volver atrás?";
                                toSpeech = new TextToSpeech(this, this);
                            }
                        }
                        else if (result.ToLower().Contains("eliminar"))
                        {
                            textToSpeak = "Diga el nombre del contacto que quiere eliminar";
                            toSpeech = new TextToSpeech(this, this);
                        }
                        else if (result.ToLower().Contains("bloqueados"))
                        {
                            StopItems();
                            StartActivity(typeof(ContactosBloqueados));
                        }
                        else if (result.ToLower().Contains("atras") || result.ToLower().Contains("atrás") || result.ToLower().Contains("volver"))
                        {
                            StopItems();
                            StartActivity(typeof(MainActivity));
                        }
                        else if (result.ToLower().Contains("nada")) { }
                        else
                        {
                            countSpeech = 0;
                            textToSpeak = "Lo siento, no ha dicho una opción válida. ¿Quiere buscar un contacto, actualizar, eliminar, bloqueados, no hacer nada o volver atrás?";
                            toSpeech = new TextToSpeech(this, this);
                        }
                    }
                    else if (countSpeech == 2)
                    {
                        if(result.ToLower() == "terminar")
                        {
                            countSpeech = 0;
                            textToSpeak = "¿Quiere buscar un contacto, actualizar, eliminar, bloqueados, no hacer nada o volver atrás?";
                            toSpeech = new TextToSpeech(this, this);
                        }
                        else
                        {
                            try
                            {
                                List<Contact> list = contactRepository.GetContactsByName(result);
                                if (list.Count == 0)
                                {
                                    count = 1;
                                    textToSpeak = "Lo siento, no existe ese contacto. Por favor, diga el nombre de un contacto válido o si no desea eliminar, diga terminar";
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
                                    hablar += ". Diga el nombre del contacto que desea eliminar";
                                    textToSpeak = hablar;
                                    toSpeech = new TextToSpeech(this, this);
                                }
                                else if (list.Count == 1)
                                {
                                    deleteContact = list[0];
                                    textToSpeak = $"¿Está seguro que quiere eliminar a {deleteContact.FirstName} {deleteContact.LastName}?";
                                    toSpeech = new TextToSpeech(this, this);
                                }
                            }
                            catch(Exception ex)
                            {
                                countSpeech = 0;
                                textToSpeak = "Ha ocurrido un error al acceder a la base de datos. Por favor, inténtelo más tarde. ¿Quiere buscar un contacto, actualizar, eliminar, bloqueados, no hacer nada o volver atrás?";
                                toSpeech = new TextToSpeech(this, this);
                            }
                        }
                    }
                    else if (countSpeech == 3)
                    {
                        if (deleteContact == null)
                        {
                            try
                            {
                                deleteContact = contactRepository.GetContactByName(result);
                                textToSpeak = $"¿Está seguro que quiere eliminar a {deleteContact.FirstName} {deleteContact.LastName}?";
                                toSpeech = new TextToSpeech(this, this);
                            }
                            catch(Exception ex)
                            {
                                countSpeech = 0;
                                textToSpeak = "Ha ocurrido un error al acceder a la base de datos. Por favor, inténtelo más tarde. ¿Quiere buscar un contacto, actualizar, eliminar, bloqueados, no hacer nada o volver atrás?";
                                toSpeech = new TextToSpeech(this, this);
                            }
                        }
                        else
                        {
                            if (result.ToLower().Contains("si") || result.ToLower().Contains("sí"))
                            {
                                try
                                {
                                    countSpeech = 0;
                                    contactService.DeleteContact(client, deleteContact, contactRepository);
                                    Vibrator vibrator = (Vibrator)this.GetSystemService(Context.VibratorService);
                                    vibrator.Vibrate(1000);
                                    textToSpeak = "Contacto eliminado. ¿Quiere buscar un contacto, actualizar, eliminar, bloqueados, no hacer nada o volver atrás?";
                                    toSpeech = new TextToSpeech(this, this);
                                }
                                catch(Exception ex)
                                {
                                    countSpeech = 0;
                                    textToSpeak = "Ha ocurrido un error al eliminar el contacto. Por favor, inténtelo más tarde. ¿Quiere buscar un contacto, actualizar, eliminar, bloqueados, no hacer nada o volver atrás?";
                                    toSpeech = new TextToSpeech(this, this);
                                }
                            }
                            else if (result.ToLower().Contains("no"))
                            {
                                countSpeech = 0;
                                textToSpeak = "¿Quiere buscar un contacto, actualizar, eliminar, bloqueados, no hacer nada o volver atrás?";
                                toSpeech = new TextToSpeech(this, this);
                            }
                        }
                    }
                    else if (countSpeech == 4)
                    {
                        if (result.ToLower().Contains("si") || result.ToLower().Contains("sí"))
                        {
                            try
                            {
                                countSpeech = 0;
                                contactService.DeleteContact(client, deleteContact, contactRepository);
                                Vibrator vibrator = (Vibrator)this.GetSystemService(Context.VibratorService);
                                vibrator.Vibrate(1000);
                                textToSpeak = "Contacto eliminado. ¿Quiere buscar un contacto, actualizar, eliminar, bloqueados, no hacer nada o volver atrás?";
                                toSpeech = new TextToSpeech(this, this);
                            }
                            catch(Exception ex)
                            {
                                countSpeech = 0;
                                textToSpeak = "Ha ocurrido un error al eliminar el contacto. Por favor, inténtelo más tarde. ¿Quiere buscar un contacto, actualizar, eliminar, bloqueados, no hacer nada o volver atrás?";
                                toSpeech = new TextToSpeech(this, this);
                            }
                        }
                        else if (result.ToLower().Contains("no"))
                        {
                            countSpeech = 0;
                            textToSpeak = "¿Quiere buscar un contacto, actualizar, eliminar, bloqueados, no hacer nada o volver atrás?";
                            toSpeech = new TextToSpeech(this, this);
                        }
                    }
                }
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
                            StartActivity(typeof(ContactosBloqueados));
                        }
                    }
                }
                else if (System.Math.Abs(diffY) > _swipeThresold && System.Math.Abs(velocityY) > _swipeVelocityThresold)
                {
                    if (diffY < 0) // Top
                    {
                        StopItems();
                        StartActivity(typeof(ContactosNombre));
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