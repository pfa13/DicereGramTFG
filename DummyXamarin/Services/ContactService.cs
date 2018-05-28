using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DummyXamarin.Interfaces.IServices;
using DummyXamarin.Models;
using DummyXamarin.Repositories;
using TeleSharp.TL;
using TeleSharp.TL.Contacts;

namespace DummyXamarin.Services
{
    public class ContactService : IContactService
    {
        public bool GetContacts(TLClient client, ContactRepository contactRepo)
        {
            try
            {
                bool output = false;
                var c = Task.Run(() => client.GetContactsAsync());
                c.Wait();
                var contacts = c.Result.Users.ToList();
                output = true;
                foreach (var co in contacts)
                {
                    var cc = co as TLUser;
                    Contact contacto = new Contact
                    {
                        Phone = string.IsNullOrEmpty(cc.Phone) ? "" : cc.Phone,
                        Id = cc.Id,
                        FirstName = string.IsNullOrEmpty(cc.FirstName) ? "" : cc.FirstName,
                        LastName = string.IsNullOrEmpty(cc.LastName) ? "" : cc.LastName,
                        Username = string.IsNullOrEmpty(cc.Username) ? "" : cc.Username,
                        Status = cc.Status == null ? "" : cc.Status.ToString().Split('.')[2].ToString().Substring(12).ToString(),
                        AccessHash = cc.AccessHash.Value,
                        Blocked = false
                    };
                    contactRepo.InsertContact(contacto);
                }
                return output;
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        public bool GetBlockedContacts(TLClient client, ContactRepository contactRepository)
        {
            try
            {
                bool output = false;
                TLRequestGetBlocked req = new TLRequestGetBlocked();
                var l = Task.Run(() => client.SendRequestAsync<TLAbsBlocked>(req));
                l.Wait();
                List<TLAbsUser> list = new List<TLAbsUser>();
                var c = l.Result as TLBlocked;
                list = c.Users.ToList();
                output = true;
                foreach (var co in list)
                {
                    var cc = co as TLUser;
                    Contact contacto = new Contact
                    {
                        Phone = string.IsNullOrEmpty(cc.Phone) ? "" : cc.Phone,
                        Id = cc.Id,
                        FirstName = string.IsNullOrEmpty(cc.FirstName) ? "" : cc.FirstName,
                        LastName = string.IsNullOrEmpty(cc.LastName) ? "" : cc.LastName,
                        Username = string.IsNullOrEmpty(cc.Username) ? "" : cc.Username,
                        Status = cc.Status == null ? "" : cc.Status.ToString().Split('.')[2].ToString().Substring(12).ToString(),
                        AccessHash = cc.AccessHash.Value,
                        Blocked = true
                    };
                    contactRepository.InsertContact(contacto);
                }
                return output;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool BlockKnownContact(TLClient client, Contact contact, ContactRepository contactRepository)
        {
            try
            {
                TLRequestBlock req = new TLRequestBlock
                {
                    Id = new TLInputUser
                    {
                        UserId = contact.Id,
                        AccessHash = contact.AccessHash
                    }
                };
                var result = Task.Run(() => client.SendRequestAsync<bool>(req));
                result.Wait();
                if (result.Result)
                {
                    contact.Blocked = true;
                    contactRepository.UpdateContact(contact);
                }
                return result.Result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool UnblockKnownContact(TLClient client, Contact contacto, ContactRepository contactRepository)
        {
            try
            {
                TLRequestUnblock req = new TLRequestUnblock
                {
                    Id = new TLInputUser
                    {
                        UserId = contacto.Id,
                        AccessHash = contacto.AccessHash
                    }
                };
                var result = Task.Run(() => client.SendRequestAsync<bool>(req));
                result.Wait();
                if (result.Result)
                {
                    contacto.Blocked = false;
                    contactRepository.UpdateContact(contacto);
                }
                return result.Result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool DeleteContact(TLClient client, Contact contact, ContactRepository contactRepository)
        {
            try
            {
                TLRequestDeleteContact req = new TLRequestDeleteContact
                {
                    Id = new TLInputUser
                    {
                        UserId = contact.Id,
                        AccessHash = contact.AccessHash
                    }
                };
                var result = Task.Run(() => client.SendRequestAsync<object>(req));
                result.Wait();
                contactRepository.DeleteContact(contact);
                return true;
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        public bool UpdateContacts(TLClient client, ContactRepository contactRepository)
        {            
            try
            {
                contactRepository.DeleteContacts();
                return GetContacts(client, contactRepository);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}