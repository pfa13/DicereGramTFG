
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
using System;
using System.Collections.Generic;
using TeleSharp.TL;

namespace DummyXamarin
{
    [Activity(Label = "Configuracion")]
    public class Configuracion : Activity, GestureDetector.IOnGestureListener, IRecognitionListener, TextToSpeech.IOnInitListener
    {
        Intent service;
        string LOG_TAG = "VoiceRecognitionActivity";
        string textToSpeak = "¿Quiere modificar su nombre de usuario, el tipo de voz, la velocidad de lectura, la activación de la voz, no hacer nada o volver atrás?";
        int _swipeThresold = 100, _swipeVelocityThresold = 100, countSpeech = 0;
        string accion = "";
        bool record = true;
        ICollection<Voice> listVoice;
        Voice v = null;

        TextToSpeech toSpeech = null;
        SpeechRecognizer speechReco;
        Intent intentReco;
        GestureDetector gestureDetector;

        TLClient client;
        TLUser usuario;
        ErrorText errorText;

        SQLiteRepository database;
        UserRepository userRepository;
        ConfigRepository configRepository;
        User user;
        Models.Config configuracion;
        
        LoginService loginService;
        UserService userService;

        Button nomusuario;
        Button tipo;
        Button velocidad;
        Button activacion;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Configuracion);
            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);

            nomusuario = FindViewById<Button>(Resource.Id.btnUsername);
            tipo = FindViewById<Button>(Resource.Id.btnChangeVoz);
            velocidad = FindViewById<Button>(Resource.Id.btnVelocidad);
            activacion = FindViewById<Button>(Resource.Id.btnDesactivarVoz);

            try
            {
                service = new Intent(this, typeof(ReceiveService));
                if (!IsMyServiceRunning(service))
                    StartService(service);
            }
            catch (Exception ex)
            {

            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            nomusuario.Click += delegate
            {
                StopItems();
            };

            tipo.Click += delegate
            {
                StopItems();
            };

            velocidad.Click += delegate
            {
                StopItems();
            };

            activacion.Click += delegate
            {
                StopItems();
            };

            loginService = new LoginService();
            userService = new UserService();

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

            database = new SQLiteRepository();
            userRepository = new UserRepository(database);
            configRepository = new ConfigRepository(database);
            errorText = new ErrorText();

            configuracion = configRepository.GetConfig();                                 

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
            if (error == SpeechRecognizerError.NoMatch)
            {
                countSpeech = 0;
                textToSpeak = "Lo siento, no le he entendido. ¿Quiere modificar su nombre de usuario, el tipo de voz, la velocidad de lectura " +
                    "o la activación de la voz, no hacer nada o volver atrás?";
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
            configuracion = configRepository.GetConfig();

            foreach (string result in matches)
            {
                count++;
                try
                {
                    if (count == 1)
                    {
                        if(countSpeech == 1)
                        {
                            if (result.ToLower().Contains("usuario"))
                            {
                                accion = "usuario";
                                try
                                {
                                    user = userRepository.GetUser();
                                    textToSpeak = user.Username != null ? $"Su nombre de usuario es {user.Username}" : "No tiene nombre de usuario";
                                    textToSpeak += ". ¿Quiere modificarlo?";
                                }
                                catch(Exception ex)
                                {
                                    countSpeech = 0;
                                    textToSpeak = "Ha ocurrido un error al acceder a la base de datos. Por favor, inténtelo más tarde. " +
                                        "¿Quiere modificar su nombre de usuario, el tipo de voz, la velocidad de lectura o " +
                                        "la activación de la voz, no hacer nada o volver atrás?";
                                }                                
                                toSpeech = new TextToSpeech(this, this);
                            }
                            else if (result.ToLower().Contains("tipo"))
                            {
                                accion = "tipo";
                                string voz = toSpeech.Voice != null ? toSpeech.Voice.Name : "la voz por defecto";
                                textToSpeak = $"La voz actual es: {voz}. ";
                                listVoice = toSpeech.Voices;
                                if (listVoice != null && listVoice.Count > 1)
                                {
                                    textToSpeak += "Las voces actualmente instaladas son: ";
                                    int contador = 0;
                                    foreach (var item in listVoice)
                                    {
                                        contador++;
                                        if (contador == listVoice.Count)
                                            textToSpeak += $"y {item.Name}. ";
                                        else
                                            textToSpeak += $"{item.Name}, ";
                                    }
                                    textToSpeak += "Diga el nombre de la voz a cambiar o diga cancelar";
                                }
                                else
                                {
                                    countSpeech = 0;
                                    textToSpeak += "No tiene más voces instaladas. ¿Quiere modificar su nombre de usuario, el tipo de voz, la velocidad de lectura o " +
                                    "la activación de la voz, no hacer nada o volver atrás?";
                                }                                
                                toSpeech = new TextToSpeech(this, this);
                            }
                            else if (result.ToLower().Contains("velocidad"))
                            {
                                accion = "velocidad";                                
                                string velocidad = configuracion.Velocidad == 1.0 ? "normal" : configuracion.Velocidad == 0.5 ? "lenta" : 
                                    configuracion.Velocidad == 0.0 ? "muy lenta" : configuracion.Velocidad == 1.5 ? "rápida" : configuracion.Velocidad == 2 ?
                                    "muy rápida" : "super rápida";
                                textToSpeak = $"La velocidad de lectura es {velocidad}. ¿Quiere aumentar la velocidad de lectura, reducirla o cancelar?";
                                toSpeech = new TextToSpeech(this, this);
                            }
                            else if (result.ToLower().Contains("activación") || result.ToLower().Contains("activacion"))
                            {
                                accion = "activar";
                                string voz = configuracion.Voz ? "activada" : "desactivada";
                                string act = configuracion.Voz ? "desactivarla" : "activarla";
                                textToSpeak = $"La voz está {voz} .¿Quiere {act}?";
                                toSpeech = new TextToSpeech(this, this);
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
                                textToSpeak = "Lo siento, no ha dicho una opción válida. ¿Quiere modificar su nombre de usuario, el tipo de voz, la velocidad de lectura " +
                                    "o la activación de la voz, no hacer nada o volver atrás?";
                                toSpeech = new TextToSpeech(this, this);
                            }
                        }
                        else if(countSpeech == 2)
                        {
                            if(accion == "usuario" && (result.ToLower().Contains("si") || result.ToLower().Contains("sí")))
                            {
                                textToSpeak = "¿Qué nombre de usuario quiere?";
                                toSpeech = new TextToSpeech(this, this);
                            }
                            else if((accion == "usuario" || accion == "activar") && result.ToLower().Contains("no"))
                            {
                                countSpeech = 0;
                                textToSpeak = "¿Quiere modificar su nombre de usuario, el tipo de voz, la velocidad de lectura o " +
                                    "la activación de la voz, no hacer nada o volver atrás?";
                                toSpeech = new TextToSpeech(this, this);
                            }
                            else if(accion == "velocidad")
                            {
                                countSpeech = 0;
                                if(result.ToLower().Contains("cancelar")) { }
                                else if (result.ToLower().Contains("aumentar"))
                                {
                                    if (configuracion.Velocidad == 2.5)
                                    {                                        
                                        textToSpeak = "No se puede aumentar más la velocidad de lectura.";
                                    }
                                    else
                                    {
                                        try
                                        {
                                            configuracion.Velocidad = configuracion.Velocidad + (float)0.5;
                                            configRepository.UpdateConfig(configuracion);
                                            textToSpeak = "Velocidad de lectura aumentada.";
                                        }
                                        catch(Exception ex)
                                        {
                                            countSpeech = 0;
                                            textToSpeak = "Ha ocurrido un error al acceder a la base de datos. Por favor, inténtelo más tarde.";
                                        }
                                    }
                                }
                                else if (result.ToLower().Contains("reducir"))
                                {
                                    if (configuracion.Velocidad == 0.0)
                                    {
                                        countSpeech = 0;
                                        textToSpeak = "No se puede reducir más la velocidad de lectura.";
                                    }
                                    else
                                    {
                                        try
                                        {
                                            configuracion.Velocidad = configuracion.Velocidad - (float)0.5;
                                            configRepository.UpdateConfig(configuracion);
                                            textToSpeak = "Velocidad de lectura reducida.";
                                        }
                                        catch(Exception ex)
                                        {
                                            countSpeech = 0;
                                            textToSpeak = "Ha ocurrido un error al acceder a la base de datos. Por favor, inténtelo más tarde.";
                                        }
                                    }
                                }
                                configuracion = configRepository.GetConfig();
                                textToSpeak += " ¿Quiere modificar su nombre de usuario, el tipo de voz, la velocidad de lectura " +
                                            "o la activación de la voz, no hacer nada o volver atrás?";
                                toSpeech = new TextToSpeech(this, this);
                            }
                            else if(accion == "tipo")
                            {
                                if(result.ToLower().Contains("cancelar"))
                                {
                                    countSpeech = 0;
                                    textToSpeak += "¿Quiere modificar su nombre de usuario, el tipo de voz, la velocidad de lectura " +
                                            "o la activación de la voz, no hacer nada o volver atrás?";
                                    toSpeech = new TextToSpeech(this, this);
                                }
                                else
                                {
                                    bool exist = false;
                                    foreach(var item in listVoice)
                                    {
                                        if (item.Name == result)
                                        {
                                            v = item;
                                            exist = true;
                                            break;
                                        }
                                    }
                                    if(exist)
                                    {
                                        try
                                        {
                                            countSpeech = 0;
                                            configuracion.TipoVoz = result;
                                            configRepository.UpdateConfig(configuracion);
                                            toSpeech.SetVoice(v);
                                            textToSpeak = "Voz modificada. ¿Quiere modificar su nombre de usuario, el tipo de voz, la velocidad de lectura " +
                                                "o la activación de la voz, no hacer nada o volver atrás?";
                                        }
                                        catch(Exception ex)
                                        {
                                            countSpeech = 0;
                                            textToSpeak = "Ha ocurrido un error al acceder a la base de datos. Por favor, inténtelo más tarde. ¿Quiere modificar" +
                                                " su nombre de usuario, el tipo de voz, la velocidad de lectura " +
                                                "o la activación de la voz, no hacer nada o volver atrás?";
                                        }
                                    }
                                    else
                                    {
                                        countSpeech = 1;
                                        textToSpeak = "No existe ese tipo de voz. Por favor, diga otro nombre de voz o cancelar";                                        
                                    }
                                    toSpeech = new TextToSpeech(this, this);
                                }
                            }
                            else if (accion == "activar" && (result.ToLower().Contains("si") || result.ToLower().Contains("sí")))
                            {
                                try
                                {
                                    countSpeech = 0;
                                    configuracion.Voz = !configuracion.Voz;
                                    string texto = configuracion.Voz ? "activada" : "desactivada";
                                    configRepository.UpdateConfig(configuracion);
                                    textToSpeak = $"Voz {texto}. ¿Quiere modificar su nombre de usuario, el tipo de voz, la velocidad de lectura " +
                                                "o la activación de la voz, no hacer nada o volver atrás?";
                                }
                                catch(Exception ex)
                                {
                                    countSpeech = 0;
                                    textToSpeak = "Ha ocurrido un error al acceder a la base de datos. Por favor, inténtelo más tarde. ¿Quiere modificar" +
                                        " su nombre de usuario, el tipo de voz, la velocidad de lectura " +
                                        "o la activación de la voz, no hacer nada o volver atrás?";
                                }
                                toSpeech = new TextToSpeech(this, this);
                            }
                        }
                        else if (countSpeech == 3)
                        {
                            if (accion == "usuario")
                            {
                                if(result.ToLower().Contains("cancelar"))
                                {
                                    countSpeech = 0;
                                    textToSpeak = "¿Quiere modificar tu nombre de usuario, el tipo de voz, la velocidad de lectura o " +
                                        "la activación de la voz, no hacer nada o volver atrás?";
                                    toSpeech = new TextToSpeech(this, this);
                                }
                                else
                                {
                                    try
                                    {
                                        bool valido = userService.CheckUsername(client, result);
                                        if (valido)
                                        {
                                            try
                                            {
                                                userService.UpdateUsername(client, result, userRepository);
                                                Vibrator vibrator = (Vibrator)this.GetSystemService(Context.VibratorService);
                                                vibrator.Vibrate(500);
                                                countSpeech = 0;
                                                textToSpeak = $"Su nombre de usuario ha sido modificado correctamente a {result}. ¿Quiere modificar tu nombre de usuario, " +
                                                    "el tipo de voz, la velocidad de lectura o la activación de la voz, no hacer nada o volver atrás?";
                                            }
                                            catch(Exception ex)
                                            {
                                                countSpeech = 0;
                                                textToSpeak = "Ha ocurrido un error al modificar su nombre de usuario. Por favor, inténtelo más tarde. ¿Quiere modificar" +
                                                        " su nombre de usuario, el tipo de voz, la velocidad de lectura " +
                                                        "o la activación de la voz, no hacer nada o volver atrás?";
                                            }
                                            toSpeech = new TextToSpeech(this, this);
                                        }
                                        else
                                        {
                                            countSpeech = 2;
                                            textToSpeak = "El nombre de usuario no es válido. Por favor, diga otro nombre de usuario o cancelar";
                                            toSpeech = new TextToSpeech(this, this);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        countSpeech = 0;
                                        textToSpeak = "Ha ocurrido un error al modificar su nombre de usuario. Por favor, inténtelo más tarde. ¿Quiere modificar" +
                                                " su nombre de usuario, el tipo de voz, la velocidad de lectura " +
                                                "o la activación de la voz, no hacer nada o volver atrás?";
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

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