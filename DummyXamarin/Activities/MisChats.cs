using System;
using System.Collections.Generic;
using System.Linq;
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
using DummyXamarin.Models.Auxiliares;
using DummyXamarin.Repositories;
using DummyXamarin.Services;
using DummyXamarin.Utils;
using Java.Util;
using TeleSharp.TL;

namespace DummyXamarin
{
    [Activity(Label = "MisChats")]
    public class MisChats : Activity, GestureDetector.IOnGestureListener, IRecognitionListener, TextToSpeech.IOnInitListener
    {
        public static MisChats Instance;
        Intent service;
        string LOG_TAG = "VoiceRecognitionActivity";
        public string textToSpeak = "", accion = "";
        int _swipeThresold = 100, _swipeVelocityThresold = 100, count = 0;
        bool record = true;
        Voice v = null;

        private MyAdapter _adapter;
        private ListView _lv;

        TextToSpeech toSpeech = null;
        SpeechRecognizer speechReco;
        Intent intentReco;
        GestureDetector gestureDetector;

        private List<Chat> _chats;
        private List<CountChats> _chatsNotReaded;
        private SQLiteRepository database;
        private MessageRepository messageRepository;
        private ContactRepository contactRepository;
        ConfigRepository configRepository;

        LoginService loginService;
        MessageService messageService;
        ErrorText errorText;

        TLClient client;
        TLUser usuario;
        Contact usuarioToSee = null;
        Models.Config configuracion;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            MisChats.Instance = this;
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.MisChats);
            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);

            database = new SQLiteRepository();
            messageRepository = new MessageRepository(database);
            contactRepository = new ContactRepository(database);
            configRepository = new ConfigRepository(database);
            _chats = messageRepository.GetMessagesOrdered();

            service = new Intent(this, typeof(ReceiveService));
            if (!IsMyServiceRunning(service))
                StartService(service);

            _lv = FindViewById<ListView>(Resource.Id.listView1);
            _adapter = new MyAdapter(this, Resource.Layout.ChatsItem, _chats);
            _lv.Adapter = _adapter;
            _lv.ItemClick += Lv_ItemClick;   
        }        

        void Lv_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            StopItems();
            usuarioToSee = contactRepository.GetContactByName(_chats[e.Position].FromTo);
            var activity = new Intent(this, typeof(SingleChat));
            activity.PutExtra("MyContact", usuarioToSee.Phone);
            StartActivity(activity);
        }

        protected override void OnResume()
        {
            base.OnResume();            

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
                var ch = messageRepository.GetMessages();

                if (ch.Count > 0)
                {
                    _chatsNotReaded = messageRepository.CountMessagesNotReaded();
                    var total = _chatsNotReaded.Sum(x => x.Counter);

                    if (_chatsNotReaded.Count > 0 && _chatsNotReaded.Count <= 5)
                    {
                        if (total != 1)
                            textToSpeak = $"Tiene {total} mensajes nuevos de ";
                        else
                            textToSpeak = $"Tiene ún mensaje nuevo de ";

                        for (var i = 0; i < _chatsNotReaded.Count; i++)
                        {
                            var contact = contactRepository.GetContactByPhone(_chatsNotReaded[i].FromTo);
                            if (_chatsNotReaded.Count > 1 && i == _chatsNotReaded.Count - 1)
                            {
                                if (contact != null)
                                    textToSpeak += $"y {contact.FirstName} {contact.LastName}. ";
                                else
                                    textToSpeak += $"y {_chatsNotReaded[i].FromTo}. ";
                            }
                            else
                            {
                                if (contact != null)
                                    textToSpeak += $"{contact.FirstName} {contact.LastName}, ";
                                else
                                    textToSpeak += $"{_chatsNotReaded[i].FromTo}, ";
                            }
                        }
                        if (_chatsNotReaded.Count != 1)
                            textToSpeak += "¿Quiere leerlos, entrar a una conversación, borrarla, no hacer nada o volver atrás?";
                        else
                            textToSpeak += "¿Quiere leerlo, entrar a una conversación, borrarla, no hacer nada o volver atrás?";
                    }
                    else if (_chatsNotReaded.Count > 5)
                        textToSpeak = $"Tiene {total} mensajes nuevos de más de 5 contactos. ¿Quiere leerlos, entrar a una conversación, borrarla, no hacer nada o volver atrás?";
                    else if (_chatsNotReaded.Count == 0)
                        textToSpeak = $"No tiene mensajes nuevos. ¿Quiere entrar a una conversación, borrarla, no hacer nada o volver atrás?";
                }
                else
                {
                    record = false;
                    textToSpeak = "No tiene ninguna conversación.";
                }
            }
            catch(Exception ex)
            {
                textToSpeak = $"Ha ocurrido un error al obtener sus mensajes nuevos. ¿Quiere entrar a una conversación, borrarla, no hacer nada o volver atrás?";
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
                else if (!record && textToSpeak == "No tiene ninguna conversación.")
                    StartActivity(typeof(MainActivity));
                else if (!record && textToSpeak.ToLower().Contains("los mensajes de"))
                {
                    record = true;
                    textToSpeak = "¿Quiere entrar a una conversación, borrarla, no hacer nada o volver atrás?";
                    toSpeech = new TextToSpeech(this, this);
                }
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
                textToSpeak = "Lo siento, no le he entendido. ¿Quiere entrar a una conversación, borrarla, no hacer nada o volver atrás?";
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
            string quien = "";
            int countOnResults = 0;
            Log.Info(LOG_TAG, "onResults");
            IEnumerable<string> matches = results.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            count++;

            foreach (string result in matches)
            {
                countOnResults++;
                if(countOnResults == 1)
                {
                    record = true;
                    if(count == 1)
                    {                        
                        if (result.ToLower().Contains("entrar"))
                        {
                            accion = "entrar";
                            textToSpeak = "Por favor, diga el nombre del contacto para entrar a su conversación";
                            toSpeech = new TextToSpeech(this, this);
                        }
                        else if (result.ToLower().Contains("borrar"))
                        {
                            accion = "borrar";
                            textToSpeak = "Por favor, diga el nombre del contacto para borrar su conversación";
                            toSpeech = new TextToSpeech(this, this);
                        }   
                        else if (result.ToLower().Contains("leer"))
                        {
                            ReadMessages();
                        }
                        else if (result.ToLower().Contains("nada")) { }
                        else if (result.ToLower().Contains("atras") || result.ToLower().Contains("atrás") || result.ToLower().Contains("volver"))
                        {
                            StopItems();
                            StartActivity(typeof(MainActivity));
                        }
                        else
                        {
                            countOnResults = 0;
                            textToSpeak = "Lo siento, no ha dicho una opción válida. ¿Quiere entrar a una conversación, borrarla, no hacer nada o volver atrás?";
                            toSpeech = new TextToSpeech(this, this);
                        }
                    }
                    else if (count == 2)
                    {
                        try
                        {
                            List<Contact> list = contactRepository.GetContactsByNameWithChat(result);
                            if (list.Count == 0)
                            {
                                count = 1;
                                textToSpeak = $"Lo siento, no existe conversación con {result}. Por favor, diga el nombre de un contacto válido.";
                                // TODO: Comprobar si existe el contacto y preguntar al usuario si quiere enviarle un mensaje
                                toSpeech = new TextToSpeech(this, this);
                            }
                            else if (list.Count > 1)
                            {
                                string hablar = $"Existen {list.Count} contactos que contienen ese nombre: ";
                                for (int i = 0; i < list.Count(); i++)
                                {
                                    if (i == list.Count() - 1)
                                        hablar += $"y {list[i].FirstName} {list[i].LastName}. ";
                                    else
                                        hablar += $"{list[i].FirstName} {list[i].LastName}, ";
                                }
                                hablar += $"Diga el nombre del contacto del que desea {accion}";
                                hablar += accion == "entrar" ? " a la conversación" : " la conversación";
                                textToSpeak = hablar;
                                toSpeech = new TextToSpeech(this, this);
                            }
                            else if (list.Count == 1)
                            {
                                usuarioToSee = list[0];
                                if (accion == "entrar")
                                {
                                    StopItems();
                                    var activity = new Intent(this, typeof(SingleChat));
                                    activity.PutExtra("MyContact", usuarioToSee.Phone);
                                    StartActivity(activity);
                                }
                                else
                                {
                                    DeleteChat();
                                }
                            }
                        }
                        catch(Exception ex)
                        {
                            count = 0;
                            textToSpeak += $"Ha ocurrido un error al {accion}. ¿Quiere entrar a una conversación, borrarla, no hacer nada o volver atrás?";
                            toSpeech = new TextToSpeech(this, this);
                        }
                    }
                    else if(count == 3)
                    {
                        usuarioToSee = contactRepository.GetContactByName(result);
                        if(accion == "entrar")
                        {
                            StopItems();
                            var activity = new Intent(this, typeof(SingleChat));
                            activity.PutExtra("MyContact", usuarioToSee.Phone);
                            StartActivity(activity);
                        }
                        else
                        {
                            DeleteChat();
                        }
                    }
                }
            }
        }

        public void ReadMessages()
        {
            try
            {
                count = 0;
                var list = messageRepository.GetMessagesNotReadedOrderedByContact();
                record = true;
                string last = "";
                int pos = 0, letras = list.Sum(x => x.Mensaje.Length);
                textToSpeak = "";
                if (letras >= 3850)
                    textToSpeak += "Hay demasiados mensajes para leer. ";
                else
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var c = contactRepository.GetContactByPhone(list[i].FromTo);
                        textToSpeak += c != null ? $"Los mensajes de {c.FirstName} {c.LastName} son: " : $"Los mensajes del número {list[i].FromTo} son: ";
                        var listM = list.Where(x => x.FromTo == list[i].FromTo).ToList();
                        last = list[i].FromTo;
                        i = listM.Count();
                        for (int j = 0; j < listM.Count(); j++)
                        {
                            if (j == listM.Count - 1)
                                textToSpeak += $"Y {(j + 1).ToString()}, {listM[j].Mensaje}. ";
                            else
                                textToSpeak += $"{(j + 1).ToString()}, {listM[j].Mensaje}. ";
                        }
                        i = i + pos - 1;
                        pos = i;
                        messageRepository.MarkMessagesAsRead(list[i].FromTo);
                    }
                }
                textToSpeak += "¿Quiere entrar a una conversación, borrarla, no hacer nada o volver atrás?";
                toSpeech = new TextToSpeech(this, this);
            }
            catch(Exception ex)
            {
                count = 0;
                textToSpeak += "Ha ocurrido un error al leer los mensajes. ¿Quiere entrar a una conversación, borrarla, no hacer nada o volver atrás?";
                toSpeech = new TextToSpeech(this, this);
            }
        }

        public void DeleteChat()
        {
            try
            {
                messageRepository.DeleteChat(usuarioToSee);
                count = 0;
                Vibrator vibrator = (Vibrator)this.GetSystemService(Context.VibratorService);
                vibrator.Vibrate(500);
                _chats = messageRepository.GetMessagesOrdered();
                _adapter = new MyAdapter(this, Resource.Layout.ChatsItem, _chats);
                _lv.Adapter = _adapter;
                textToSpeak = $"Conversación con {usuarioToSee.FirstName} {usuarioToSee.LastName} borrada. ¿Quiere entrar a una conversación, borrarla, no hacer nada o volver atrás?";
                toSpeech = new TextToSpeech(this, this);
            }
            catch(Exception ex)
            {
                count = 0;
                textToSpeak += "Ha ocurrido un error al acceder a la base de datos. ¿Quiere entrar a una conversación, borrarla, no hacer nada o volver atrás?";
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
                        StopItems();
                        StartActivity(typeof(MainActivity));
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
        public class BroadcastMisChats : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                var _chats = MisChats.Instance.messageRepository.GetMessagesOrdered();
                var _adapter = new MyAdapter(MisChats.Instance, Resource.Layout.ChatsItem, _chats);
                var _lv = MisChats.Instance.FindViewById<ListView>(Resource.Id.listView1);
                _lv.Adapter = _adapter;
            }
        }
    }
}