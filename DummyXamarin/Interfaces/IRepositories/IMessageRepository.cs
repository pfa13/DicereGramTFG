using System;
using System.Collections.Generic;
using DummyXamarin.Models;
using DummyXamarin.Models.Auxiliares;

namespace DummyXamarin.Interfaces.IRepositories
{
    public interface IMessageRepository
    {
        bool InsertMessage(Chat mensaje);
        bool InsertChat(Chat mensaje);
        bool DeleteChat(Contact contact);

        List<Chat> GetMessages();
        List<Chat> GetMessagesByPhoneOrdered(string contactPhone);
        List<Chat> GetMessagesByPhone(string contactPhone);
        List<Chat> GetMessagesByPhoneWithoutSeen(string contactPhone);
        List<Chat> GetMessagesByPhoneWithoutSeen(string contactPhone, int position);
        List<Chat> GetMessagesOrdered();
        List<Chat> GetMessagesNotReaded();
        List<Chat> GetMessagesNotReadedOrderedByContact();
        List<CountChats> CountMessagesNotReaded();        
        List<Chat> GetMessagesByPhoneAndMessage(string phone, string message);
        List<Chat> GetMessagesByPhoneAndDate(string phone, DateTime date);
        List<Chat> GetMessagesByPhoneAndMessage(string phone, string message, int position);
        List<Chat> GetMessagesByPhoneAndDate(string phone, DateTime date, int position);

        bool CheckExistFromContact(string phone);
        bool MarkMessagesAsRead(string phone);
    }
}