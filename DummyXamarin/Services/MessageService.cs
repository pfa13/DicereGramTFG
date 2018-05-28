using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DummyXamarin.Interfaces.IServices;
using DummyXamarin.Models;
using DummyXamarin.Repositories;
using TeleSharp.TL;
using TeleSharp.TL.Messages;

namespace DummyXamarin.Services
{
    public class MessageService : IMessageService
    {
        MessageRepository messageRepository;

        public bool SendTyping(TLClient client, int id, long accesshash)
        {
            try
            {
                var sendtyping = Task.Run(() => client.SendTypingAsync(new TLInputPeerUser() { UserId = id, AccessHash = accesshash }));
                sendtyping.Wait();
                return sendtyping.Result;
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        public TLAbsUpdates SendMessage(TLClient client, int id, string message)
        {
            try
            {
                var sendm = Task.Run(() => client.SendMessageAsync(new TLInputPeerUser() { UserId = id }, message));
                sendm.Wait();
                return sendm.Result;
            }
            catch(Exception ex)
            {
                throw;
            }
        }            
        
        public int ReceiveMessages(TLClient client, MessageRepository messageRepository)
        {
            TLInputPeerUser target = null;
            bool hayNuevos = false;
            int messageCount = 0;
            int firstMessage = 0;
            try
            {
                var dialogs = GetUserDialogs(client);
                IEnumerable<TLDialog> listDialogs = dialogs.Dialogs.ToList().Where(x => (x.UnreadCount > 0));

                foreach (var dia in listDialogs)
                {
                    hayNuevos = true;
                    if (dia.Peer is TLPeerUser)
                    {
                        var peer = dia.Peer as TLPeerUser;
                        var chat = dialogs.Users.ToList().OfType<TLUser>().First(x => x.Id == peer.UserId);
                        target = new TLInputPeerUser { UserId = chat.Id, AccessHash = (long)chat.AccessHash };
                        var hist = GetHistory(client, target, dia);
                        if (hist is TLMessagesSlice)
                        {
                            var h = hist as TLMessagesSlice;
                            var history = h.Messages.ToList();
                            for (var i = 0; i < history.Count; i++)
                            {
                                var mens = history[i] as TLMessage;
                                if (i == 0)
                                    firstMessage = mens.Id;
                                if (!mens.Out)
                                {
                                    Chat c = new Chat
                                    {
                                        Created = (new DateTime(1970, 1, 1)).AddSeconds((double)mens.Date).AddHours(1),
                                        FromTo = chat.Phone,
                                        Mensaje = mens.Message,
                                        Send = false,
                                        Seen = false
                                    };
                                    messageRepository.InsertChat(c);
                                    messageCount++;
                                }
                            }
                        }
                        else if (hist is TLMessages)
                        {
                            var h = hist as TLMessages;
                            var history = h.Messages.ToList();
                            for (var i = 0; i < history.Count; i++)
                            {
                                var mens = history[i] as TLMessage;
                                if (i == 0)
                                    firstMessage = mens.Id;
                                if (!mens.Out)
                                {
                                    Chat c = new Chat
                                    {
                                        Created = (new DateTime(1970, 1, 1)).AddSeconds((double)mens.Date).AddHours(1),
                                        FromTo = chat.Phone,
                                        Mensaje = mens.Message,
                                        Send = false,
                                        Seen = false
                                    };
                                    messageRepository.InsertMessage(c);
                                    messageCount++;
                                }
                            }
                        }                        
                    }
                    if (firstMessage > 0)
                    {
                        var readed = new TeleSharp.TL.Messages.TLRequestReadHistory
                        {
                            Peer = target
                        };
                        var affectedMessages = Task.Run(() => client.SendRequestAsync<TLAffectedMessages>(readed));
                        affectedMessages.Wait();
                        var resultado = affectedMessages.Result;
                    }
                }
                return messageCount;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        private TLDialogs GetUserDialogs(TLClient client)
        {
            try
            {
                var dialo = Task.Run(() => client.GetUserDialogsAsync());
                dialo.Wait();
                var dialogs = dialo.Result as TLDialogs;
                return dialogs;
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        private TLAbsMessages GetHistory(TLClient client, TLInputPeerUser target, TLDialog dia)
        {
            try
            {
                var historyAsync = Task.Run(() => client.GetHistoryAsync(target, 0, -1, dia.UnreadCount));
                historyAsync.Wait();
                var hist = historyAsync.Result;
                return hist;
            }
            catch(Exception ex)
            {
                return null;
            }
        }
    }
}