using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    [Activity(Label = "SingleChat")]
    public class SingleChat : Activity, GestureDetector.IOnGestureListener, IRecognitionListener, TextToSpeech.IOnInitListener
    {
        public static SingleChat Instance;
        Intent service;
        string LOG_TAG = "VoiceRecognitionActivity";
        public string textToSpeak = "";
        int _swipeThresold = 100;
        int _swipeVelocityThresold = 100;
        private int count = 0, posicion = -1, posicionSinLeer = -1;
        private string extra, mensaje, accion, accionLeer, fecha, mensajeSearch;
        private bool record = true;
        Voice v = null;

        public MyAdapterSingle _adapter;
        private ListView _lv;
        private TextView nombre;

        TextToSpeech toSpeech = null;
        SpeechRecognizer speechReco;
        Intent intentReco;
        GestureDetector gestureDetector;

        private List<Chat> _chats;
        private Contact contact;
        Models.Config configuracion;
        private SQLiteRepository database;
        private MessageRepository messageRepository;
        private ContactRepository contactRepository;
        ConfigRepository configRepository;

        LoginService loginService;
        MessageService messageService;
        ErrorText errorText;

        TLClient client;
        TLUser usuario;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            SingleChat.Instance = this;
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.SingleChat);
            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);

            database = new SQLiteRepository();
            messageRepository = new MessageRepository(database);
            contactRepository = new ContactRepository(database);

            service = new Intent(this, typeof(ReceiveService));
            if (!IsMyServiceRunning(service))
                StartService(service);

            extra = Intent.GetStringExtra("MyContact");
            var c = contactRepository.GetContactByPhone(extra);
            nombre = FindViewById<TextView>(Resource.Id.nombre);
            nombre.Text = c.FirstName + " " + c.LastName;
            _lv = FindViewById<ListView>(Resource.Id.listView1);
            _adapter = new MyAdapterSingle(this, Resource.Layout.SingleChatItem, GetChats());
            _lv.Adapter = _adapter;
        }

        private List<Chat> GetChats()
        {            
            _chats = messageRepository.GetMessagesByPhone(extra);
            return _chats;
        }

        protected override void OnResume()
        {
            base.OnResume();

            configRepository = new ConfigRepository(database);
            configuracion = configRepository.GetConfig();            

            loginService = new LoginService();
            messageService = new MessageService();
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

            try
            {
                _chats = messageRepository.GetMessagesByPhoneWithoutSeen(extra);
                contact = contactRepository.GetContactByPhone(extra);
                messageRepository.MarkMessagesAsRead(extra);
                var total = _chats.Sum(x => x.Mensaje.Length);
                if (_chats.Count > 0)
                {
                    textToSpeak = $"Los mensajes nuevos de {contact.FirstName} {contact.LastName} son: ";
                    if (total < 3900)
                    {
                        for (int i = 0; i < _chats.Count; i++)
                        {
                            int j = i + 1;
                            if (i == _chats.Count - 1)
                                textToSpeak += $" y {j.ToString()} {_chats[i].Mensaje}";
                            else
                                textToSpeak += $"{j.ToString()} {_chats[i].Mensaje}, ";
                        }
                        textToSpeak += ". ¿Quiere responder?";
                        accion = "responder";
                    }
                    else
                    {
                        LeerMensajesSinLeer();
                    }
                }
                else
                {
                    textToSpeak = "¿Quiere leer desde una fecha, buscar por un mensaje, no hacer nada o volver atrás?";
                    accion = "leer";
                }
            }
            catch(Exception ex)
            {
                textToSpeak = "Ha ocurrido un error al acceder a la base de datos. ¿Quiere leer desde una fecha, buscar por un mensaje, no hacer nada o volver atrás?";
                accion = "leer";
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

                if (record)
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
                accion = "leer";
                textToSpeak = "Lo siento, no le he entendido. ¿Quiere leer desde una fecha, buscar por un mensaje, no hacer nada o volver atrás?";
                toSpeech = new TextToSpeech(this, this);
            }
            else if(error == SpeechRecognizerError.Network || error == SpeechRecognizerError.NetworkTimeout)
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
            int countOnResults = 0;
            Log.Info(LOG_TAG, "onResults");
            IEnumerable<string> matches = results.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            count++;

            foreach (string result in matches)
            {
                countOnResults++;
                if (countOnResults == 1)
                {
                    record = true;
                    if (count == 1)
                    {
                        if (accion == "responder" && (result.ToLower() == "si" || result.ToLower() == "sí"))
                        {
                            textToSpeak = "¿Qué mensaje quiere enviar?";
                            toSpeech = new TextToSpeech(this, this);
                        }
                        else if (accion == "responder" && result.ToLower() == "no")
                        {
                            count = 0;
                            textToSpeak = "¿Quiere leer desde una fecha, buscar por un mensaje, no hacer nada o volver atrás?";
                            accion = "leer";
                            toSpeech = new TextToSpeech(this, this);
                        }
                        else if (accion == "leer" && result.ToLower().Contains("fecha"))
                        {
                            accionLeer = "fecha";
                            textToSpeak = "Diga la fecha desde la que quiere leer";
                            toSpeech = new TextToSpeech(this, this);
                        }
                        else if (accion == "leer" && result.ToLower().Contains("buscar"))
                        {
                            accionLeer = "buscar";
                            textToSpeak = "¿Qué mensaje quiere buscar?";
                            toSpeech = new TextToSpeech(this, this);
                        }
                        else if (accion == "leyendo" && (result.ToLower() == "si" || result.ToLower() == "sí"))
                        {
                            LeerMensajes();
                        }
                        else if (accion.Contains("leyendo") && result.ToLower() == "no")
                        {
                            StopItems();
                            StartActivity(typeof(MisChats));
                        }
                        else if (result.ToLower().Contains("atras") || result.ToLower().Contains("atrás") || result.ToLower().Contains("volver"))
                        {
                            StopItems();
                            StartActivity(typeof(MisChats));
                        }
                        else if (accion == "leer" && result.ToLower().Contains("nada"))
                        {
                        }
                        else
                        {
                            record = true;
                            count = 0;
                            accion = "leer";
                            textToSpeak = "Lo siento, no ha dado una respuesta válida. ¿Quiere leer desde una fecha, buscar por un mensaje, no hacer nada o volver atrás?";
                            toSpeech = new TextToSpeech(this, this);
                        }
                    }
                    else if(count == 2)
                    {
                        if(accion == "responder")
                        {
                            mensaje = result;
                            textToSpeak = "Su mensaje es: " + mensaje + ". ¿Quiere enviarlo?";
                            toSpeech = new TextToSpeech(this, this);
                        }
                        else if(accion == "leer" && accionLeer == "fecha")
                        {
                            //fecha = result;
                            //DateTime date = ConvertDateTime();
                            //_chats = messageRepository.GetMessagesByPhoneAndDate(contact.Phone, date);
                            //_adapter.Clear();
                            //_adapter.AddAll(_chats);
                            //_adapter.NotifyDataSetChanged();
                            //LeerMensajes();
                        }
                        else if (accion == "leer" && accionLeer == "buscar")
                        {
                            try
                            {
                                mensajeSearch = result;
                                _chats = messageRepository.GetMessagesByPhoneAndMessage(contact.Phone, mensajeSearch);
                                _adapter.Clear();
                                _adapter.AddAll(_chats);
                                _adapter.NotifyDataSetChanged();
                                LeerMensajes();
                            }
                            catch(Exception ex)
                            {
                                count = 0;
                                accion = "leer";
                                record = true;
                                textToSpeak = "Ha ocurrido un error al acceder a la base de datos. Por favor, inténtelo más tarde. ¿Quiere leer desde una fecha, buscar por un mensaje, no hacer nada o volver atrás?";
                                toSpeech = new TextToSpeech(this, this);
                            }
                        }
                    }
                    else if(count == 3)
                    {
                        if(accion == "responder")
                        {
                            if (result.ToLower() == "si" || result.ToLower() == "sí")
                            {
                                try
                                {
                                    TLAbsUpdates updates = null;
                                    updates = messageService.SendMessage(client, contact.Id, mensaje);
                                    MensajeEnviado(updates, contact.Phone, mensaje);
                                }
                                catch(Exception ex)
                                {
                                    count = 0;
                                    accion = "leer";
                                    record = true;
                                    textToSpeak = "Ha ocurrido un error al enviar el mensaje. Por favor, inténtelo más tarde. ¿Quiere leer desde una fecha, buscar por un mensaje, no hacer nada o volver atrás?";
                                    toSpeech = new TextToSpeech(this, this);
                                }
                            }
                            else if (result.ToLower() == "no")
                            {
                                count = 0;
                                textToSpeak = "¿Quiere leer desde una fecha, buscar por un mensaje, no hacer nada o volver atrás?";
                                accion = "leer";
                                toSpeech = new TextToSpeech(this, this);
                            }
                        }
                    }
                }
            }
        }

        public void MensajeEnviado(TLAbsUpdates updates, string fromPhone, string mensaje)
        {
            if (updates != null)
            {
                Chat m = new Chat
                {
                    FromTo = fromPhone,
                    Mensaje = mensaje,
                    Send = true,
                    Created = DateTime.Now,
                    Seen = true
                };
                messageRepository.InsertMessage(m);
                Vibrator vibrator = (Vibrator)this.GetSystemService(Context.VibratorService);
                vibrator.Vibrate(1000);
                _adapter = new MyAdapterSingle(this, Resource.Layout.SingleChatItem, GetChats());
                _lv.Adapter = _adapter;
                count = 0;
                textToSpeak = "Mensaje enviado. ¿Quiere leer desde una fecha, buscar por un mensaje, no hacer nada o volver atrás?";
                toSpeech = new TextToSpeech(this, this);
            }
            else
            {
                count = 0;
                textToSpeak = "No se ha podido enviar el mensaje. Inténtelo más tarde. ¿Quiere leer desde una fecha, buscar por un mensaje, no hacer nada o volver atrás?";
                toSpeech = new TextToSpeech(this, this);                
            }
        }

        public void LeerMensajes()
        {
            posicion++;
            string de = "", depost = "";
            List<Chat> l = new List<Chat>();
            if (accionLeer == "fecha")
            {
                DateTime date = ConvertDateTime();
                l = messageRepository.GetMessagesByPhoneAndDate(contact.Phone, date, posicion*10);
            }
            else if (accionLeer == "buscar")
                l = messageRepository.GetMessagesByPhoneAndMessage(contact.Phone, mensajeSearch, posicion*10);
            
            if(posicion == 0 && l.Count == 0)
            {
                count = 0;
                record = true;
                accion = "leer";
                string hablar = accionLeer == "fecha" ? "desde esa fecha. " : "que concuerden con su mensaje. ";
                textToSpeak = $"No existen mensajes {hablar}. ¿Quiere leer desde una fecha, buscar por un mensaje, no hacer nada o volver atrás?";
                toSpeech = new TextToSpeech(this, this);
            }   
            else
            {
                if (l.Count > 0)
                {
                    record = false;
                    for (int i = 0; i < l.Count; i++)
                    {
                        de = !l[i].Send ? $"{contact.FirstName} {contact.LastName}: " : "Tú: ";
                        if (i == 0)
                            textToSpeak = de + l[i].Mensaje;
                        else
                        {
                            if (depost == de)
                                textToSpeak = $". {l[i].Mensaje}";
                            else
                                textToSpeak = $". {de} {l[i].Mensaje}";
                        }
                        depost = de;
                        toSpeech = new TextToSpeech(this, this);
                    }
                    accion = "leyendo";
                    count = 0;
                    record = true;
                    textToSpeak = "¿Quiere continuar leyendo?";
                    toSpeech = new TextToSpeech(this, this);
                }
                else
                {
                    count = 0;
                    accion = "leer";
                    record = true;
                    textToSpeak = "He terminado. ¿Quiere leer desde una fecha, buscar por un mensaje, no hacer nada o volver atrás?";
                    toSpeech = new TextToSpeech(this, this);
                }
            }
        }

        public void LeerMensajesSinLeer()
        {
            posicionSinLeer++;
            string de = "", depost = "";
            List<Chat> l = new List<Chat>();
            l = messageRepository.GetMessagesByPhoneWithoutSeen(contact.Phone);
            
            if (l.Count > 0)
            {
                for(int i = 0; i < l.Count; i++)
                {
                    record = false;
                    textToSpeak += $"{(i + 1).ToString()}. ";
                    textToSpeak += $"{l[i].Mensaje}. ";
                    toSpeech = new TextToSpeech(this, this);
                }
                count = 0;
                record = true;
                textToSpeak += ". ¿Quiere responder?";
                accion = "responder";
                toSpeech = new TextToSpeech(this, this);
            }
        }

        public DateTime ConvertDateTime()
        {
            DateTime date;
            if (fecha == "hoy")
                date = DateTime.Today;
            else if (fecha == "ayer")
                date = DateTime.Today.AddDays(-1);
            else if (fecha == "anteayer")
                date = DateTime.Today.AddDays(-2);
            else
            {
                var f = fecha.Replace(" de ", "-");
                DateTime.TryParse(f, out date);
                date = DateTime.Parse(date.ToString("yyyy-MM-dd HH:MM:ss"));
            }
            return date;
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
                    { // Right or left
                        StopItems();
                        StartActivity(typeof(MisChats));
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

        [BroadcastReceiver]
        public class BroadcastSingle : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                var _adapter = new MyAdapterSingle(SingleChat.Instance, Resource.Layout.SingleChatItem, SingleChat.Instance.GetChats());
                var _lv = SingleChat.Instance.FindViewById<ListView>(Resource.Id.listView1);
                _lv.Adapter = _adapter;
            }
        }
    }
}