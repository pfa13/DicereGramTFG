using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Runtime;
using Android.Speech;
using Android.Speech.Tts;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Util;
using TeleSharp.TL;
using TLSharp.Core;
using DummyXamarin.Models;
using DummyXamarin.Repositories;
using DummyXamarin.Services;
using DummyXamarin.Utils;

namespace DummyXamarin
{
    [Activity(MainLauncher = true)]
    public class Login : Activity, IRecognitionListener, TextToSpeech.IOnInitListener
    {
        TextToSpeech toSpeech = null;
        SpeechRecognizer speechReco = null;
        Intent intentReco;

        FakeSessionDelete FakeSessionDelete;
        TLClient client;
        SQLiteRepository database;
        UserRepository userRepo;
        ContactRepository contactRepo;
        ConfigRepository configRepo;
        LoginService loginService;
        ContactService contactService;
        ErrorText errorText;
        Models.Config configuracion;

        string LOG_TAG = "VoiceRecognitionActivity";
        string textToSpeak = "¡Bienvenido a Diceregram!. ¿Cuál es su número de teléfono?";
        string telephone;
        bool _continue = true, record = true;

        ISharedPreferences sessionPref;
        static string userSessionPref = "userPref";
        string SESSION_PHONE = "";

        EditText phone;
        EditText code;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Login);
            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);

            // Control sesión
            sessionPref = GetSharedPreferences(userSessionPref, FileCreationMode.Private);
            string p = sessionPref.GetString("user", "");
            if (!p.Equals(""))
            {
                _continue = false;
                StartActivity(typeof(MainActivity));
            }

            phone = FindViewById<EditText>(Resource.Id.editText1);
            code = FindViewById<EditText>(Resource.Id.editText2);
        }

        protected override void OnResume()
        {
            base.OnResume();

            FakeSessionDelete = new FakeSessionDelete();
            database = new SQLiteRepository();
            userRepo = new UserRepository(database);
            contactRepo = new ContactRepository(database);
            configRepo = new ConfigRepository(database);
            database.CreateDatabase();            

            loginService = new LoginService();
            contactService = new ContactService();
            errorText = new ErrorText();

            if (_continue)
            {
                speechReco = SpeechRecognizer.CreateSpeechRecognizer(this.ApplicationContext);
                speechReco.SetRecognitionListener(this);
                intentReco = new Intent(RecognizerIntent.ActionRecognizeSpeech);
                intentReco.PutExtra(RecognizerIntent.ExtraLanguagePreference, "es");
                intentReco.PutExtra(RecognizerIntent.ExtraCallingPackage, this.PackageName);
                intentReco.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelWebSearch);
                intentReco.PutExtra(RecognizerIntent.ExtraMaxResults, 1);

                toSpeech = new TextToSpeech(this, this);
            }
        }

        public void OnInit([GeneratedEnum] OperationResult status)
        {
            if (status == OperationResult.Success)
            {
                try
                {
                    toSpeech.SetLanguage(new Locale("es", "ES"));
                    toSpeech.Speak(textToSpeak, QueueMode.Flush, null, null);
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
                if (!record && !textToSpeak.Contains("No hay conexión a Internet") && !textToSpeak.Contains("código de verificación"))
                    StartActivity(typeof(MainActivity));
                else if (!record && textToSpeak.Contains("Ha habido un error"))
                    this.FinishAffinity();
                else if (!record && textToSpeak.Contains("Ha ocurrido un error."))
                    this.FinishAffinity();
                else
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
                textToSpeak = "Lo siento, no le he entendido. ¿Cuál es su número de teléfono?";
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

        public void InitSession(string phone)
        {
            SESSION_PHONE = phone;
            ISharedPreferencesEditor session_editor = sessionPref.Edit();
            session_editor.PutString("user", SESSION_PHONE);
            session_editor.Commit();
        }

        public string LookForSms(DateTime ahora)
        {
            bool receiveSms = false;
            string codigo = "";
            Android.Net.Uri inboxURI = Android.Net.Uri.Parse("content://sms/inbox");
            // Esperar sms
            while (!receiveSms)
            {
                if(ahora.AddMinutes(2) < DateTime.UtcNow)
                {
                    receiveSms = true;
                    codigo = "Ha habido un error";
                    break;
                }
                ContentResolver cr = this.ContentResolver;
                ICursor cursor = cr.Query(inboxURI, null, null, null, null);
                cursor.MoveToFirst();
                double date = cursor.GetDouble(cursor.GetColumnIndex("date"));
                var time = (new DateTime(1970, 1, 1)).AddMilliseconds(date);

                if (cursor != null && cursor.Count > 0)
                {
                    string address = cursor.GetString(cursor.GetColumnIndex("address"));
                    string body = cursor.GetString(cursor.GetColumnIndex("body"));
                    if (!string.IsNullOrEmpty(address) && !string.IsNullOrEmpty(body) && time >= ahora)
                    {
                        if (address.ToUpper().Contains("SMS") || address.ToUpper().Contains("TELEGRAM"))
                        {
                            if (body.Contains("Telegram code"))
                            {
                                codigo = body.Split(' ')[2];
                                receiveSms = true;
                                code.Text = codigo;
                            }
                        }
                    }
                }
            }
            return codigo;
        }

        public void OnResults(Bundle results)
        {
            int countOnResults = 0;
            IEnumerable<string> matches = results.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            string text = "", codigo = "", hash = "";
            bool isRegistered = true;
            TLUser user;
            DateTime ahora = DateTime.UtcNow;
            Android.Net.Uri inboxURI = Android.Net.Uri.Parse("content://sms/inbox");

            foreach (string result in matches)
            {
                countOnResults++;
                try
                {
                    if (countOnResults == 1)
                    {
                        record = true;
                        if (int.TryParse(result[0].ToString(), out int num))
                        {
                            text = result.Replace(" ", string.Empty);
                            if (text.Length == 9)
                            {
                                telephone = "34" + text;
                                phone.Text = telephone;

                                client = loginService.Connect();

                                isRegistered = loginService.IsPhoneRegistered(client, telephone);
                                if (!isRegistered)
                                {
                                    textToSpeak = "No tiene una cuenta registrada. ¿Cuál quiere que sea su nombre de usuario?";
                                    toSpeech = new TextToSpeech(this, this);
                                }
                                else
                                {
                                    if (!client.IsUserAuthorized())
                                    {
                                        hash = loginService.SendCodeRequest(client, telephone);
                                        codigo = LookForSms(ahora);
                                        if(codigo == "Ha habido un error")
                                        {
                                            record = false;
                                            textToSpeak = "Ha habido un error en la recepción del SMS. Por favor, inténtelo en aproximadamente media hora";
                                            toSpeech = new TextToSpeech(this, this);
                                        }
                                        else
                                        {
                                            code.Text = codigo;
                                            user = loginService.MakeAuth(client, telephone, hash, codigo);

                                            try
                                            {
                                                User u = new User
                                                {
                                                    Phone = telephone,
                                                    Username = user.Username,
                                                    Hash = hash,
                                                    Code = codigo,
                                                    SessionExpires = (new DateTime(1970, 1, 1)).AddMilliseconds(client.Session.SessionExpires).AddHours(1),
                                                    AccessHash = user.AccessHash
                                                };
                                                userRepo.InsertUser(u);

                                                configuracion = new Models.Config()
                                                {
                                                    Phone = telephone,
                                                    Voz = true,
                                                    Velocidad = (float)1.0,
                                                    TipoVoz = toSpeech.Voice != null ? toSpeech.Voice.Name : ""
                                                };
                                                configRepo.InsertConfig(configuracion);
                                                bool contactos = contactService.GetContacts(client, contactRepo);
                                                bool contactosBloqueados = contactService.GetBlockedContacts(client, contactRepo);
                                            }                                        
                                            catch(Exception ex) { }
                                            
                                            Vibrator vibrator = (Vibrator)this.GetSystemService(Context.VibratorService);
                                            vibrator.Vibrate(1000);
                                            record = false;
                                            // Creación sesión
                                            InitSession(telephone);

                                            textToSpeak = "Login correcto";
                                            toSpeech = new TextToSpeech(this, this);
                                        }
                                    }
                                    else
                                    {
                                        user = client.Session.TLUser;

                                        try
                                        {
                                            User u = new User
                                            {
                                                Phone = telephone,
                                                Username = user.Username,
                                                Hash = hash,
                                                Code = codigo,
                                                SessionExpires = (new DateTime(1970, 1, 1)).AddMilliseconds(client.Session.SessionExpires).AddHours(1),
                                                AccessHash = user.AccessHash
                                            };
                                            userRepo.InsertUser(u);
                                            configuracion = new Models.Config()
                                            {
                                                Phone = telephone,
                                                Voz = true,
                                                Velocidad = (float)1.0,
                                                TipoVoz = toSpeech.Voice != null ? toSpeech.Voice.Name : ""
                                            };
                                            configRepo.InsertConfig(configuracion);
                                        }
                                        catch(Exception ex) { }
                                        // Creación sesión 
                                        InitSession(telephone);
                                        Vibrator vibrator = (Vibrator)this.GetSystemService(Context.VibratorService);
                                        vibrator.Vibrate(1000);
                                        record = false;
                                        textToSpeak = "Login correcto";
                                        toSpeech = new TextToSpeech(this, this);
                                    }
                                }
                            }
                            else
                            {
                                record = true;
                                textToSpeak = "El número de teléfono no tiene un formato correcto. Por favor, diga un número de teléfono válido";
                                toSpeech = new TextToSpeech(this, this);
                            }
                        }
                        else
                        {
                            client = loginService.Connect();
                            hash = loginService.SendCodeRequest(client, telephone);

                            codigo = LookForSms(ahora);

                            if (int.TryParse(codigo, out int num2))
                            {
                                record = false;
                                textToSpeak = "Ha habido un error esperando el código de verificación. Por favor, inténtelo en 30 minutos";
                                toSpeech = new TextToSpeech(this, this);
                            }
                            else
                            {
                                user = loginService.SignUp(client, telephone, hash, codigo, result, "");
                                user = loginService.MakeAuth(client, telephone, hash, codigo);

                                try
                                {
                                    User u = new User
                                    {
                                        Phone = telephone,
                                        Username = user.Username,
                                        Hash = hash,
                                        Code = codigo,
                                        SessionExpires = (new DateTime(1970, 1, 1)).AddMilliseconds(client.Session.SessionExpires).AddHours(1),
                                        AccessHash = user.AccessHash
                                    };
                                    userRepo.InsertUser(u);

                                    configuracion = new Models.Config()
                                    {
                                        Phone = telephone,
                                        Voz = true,
                                        Velocidad = (float)1.0,
                                        TipoVoz = toSpeech.Voice != null ? toSpeech.Voice.Name : ""
                                    };
                                    configRepo.InsertConfig(configuracion);

                                    bool contactos = contactService.GetContacts(client, contactRepo);
                                    bool contactosBloqueados = contactService.GetBlockedContacts(client, contactRepo);
                                }
                                catch(Exception ex) { }
                                Vibrator vibrator = (Vibrator)this.GetSystemService(Context.VibratorService);
                                vibrator.Vibrate(1000);
                                record = false;

                                // Creación sesión
                                InitSession(telephone);

                                textToSpeak = "Registro y login correctos";
                                toSpeech = new TextToSpeech(this, this);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // TODO: cerrar la aplicación
                    record = false;
                    database.DeleteDataDatabase();
                    FakeSessionDelete.DeleteSession();
                    textToSpeak = "Ha ocurrido un error. Por favor, inténtelo más tarde";
                    toSpeech = new TextToSpeech(this, this);                    
                }
            }
        }

        public void OnRmsChanged(float v) { }

        public void StopItems()
        {
            speechReco.StopListening();
            toSpeech.Stop();
            toSpeech.Shutdown();
        }

        protected override void OnDestroy()
        {
            if(toSpeech != null && speechReco != null)
            {
                StopItems();
            }
            base.OnDestroy();
        }
    }
}