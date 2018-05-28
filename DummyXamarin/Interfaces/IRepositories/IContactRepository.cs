using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using DummyXamarin.Models;

namespace DummyXamarin.Interfaces.IRepositories
{
    public interface IContactRepository
    {        
        List<Contact> GetContacts();
        List<Contact> GetContactsByName(string name);
        Contact GetContactByName(string name);
        List<string> GetContactsName();
        Contact GetContactById(int id);
        Contact GetContactByPhone(string phone);
        List<Contact> GetContactsByNameWithChat(string name);

        List<string> GetContactsStatus();

        bool InsertContact(Contact contact);
        bool UpdateContact(Contact contact);
        bool DeleteContact(Contact contact);
        bool DeleteContacts();

        List<Contact> GetBlocked();
        List<string> GetBlockedName();       
    }
}