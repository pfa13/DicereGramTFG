using DummyXamarin.Models;
using DummyXamarin.Repositories;

namespace DummyXamarin.Interfaces.IServices
{
    public interface IContactService
    {
        bool GetContacts(TLClient client, ContactRepository contactRepository);
        bool GetBlockedContacts(TLClient client, ContactRepository contactRepository);
        bool BlockKnownContact(TLClient client, Contact contact, ContactRepository contactRepository);
        bool UnblockKnownContact(TLClient client, Contact contacto, ContactRepository contactRepository);
        bool DeleteContact(TLClient client, Contact contact, ContactRepository contactRepository);
        bool UpdateContacts(TLClient client, ContactRepository contactRepository);
    }
}