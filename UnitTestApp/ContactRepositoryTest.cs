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
using DummyXamarin.Repositories;
using NUnit.Framework;

namespace UnitTestApp
{
    [TestFixture]
    public class ContactRepositoryTest
    {
        [SetUp]
        public void Setup() { }


        [TearDown]
        public void Tear()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.DeleteDataDatabase();
        }

        [Test]
        public void GetContactC1()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            ContactRepository contactRepository = new ContactRepository(con);
            Contact contact = new Contact
            {
                Id = 1,
                FirstName = "Pablo",
                LastName = "Corral",
                Phone = "34676681420"
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                var result = contactRepository.GetContacts().Count;
                Assert.AreEqual(1, result);
            }
        }

        [Test]
        public void GetContactC2()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            ContactRepository contactRepository = new ContactRepository(con);
            Contact contact = new Contact
            {
                Id = 1,
                FirstName = "Pablo",
                LastName = "Corral",
                Phone = "34676681420"
            };
            Contact contact1 = new Contact
            {
                Id = 2,
                FirstName = "Aa",
                LastName = "Mama",
                Phone = "34692511479"
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                contactRepository.InsertContact(contact1);
                var result = contactRepository.GetContacts().Count;
                Assert.AreNotEqual(1, result);
            }
        }

        [Test]
        public void GetContactsByName()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            ContactRepository contactRepository = new ContactRepository(con);
            Contact contact = new Contact
            {
                Id = 1,
                FirstName = "Pablo",
                LastName = "Corral",
                Phone = "34676681420"
            };
            Contact contact1 = new Contact
            {
                Id = 2,
                FirstName = "Aa",
                LastName = "Mama",
                Phone = "34692511479"
            };
            Contact contact2 = new Contact
            {
                Id = 3,
                FirstName = "Pablo",
                LastName = "No existe",
                Phone = "34666666666"
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                contactRepository.InsertContact(contact1);
                contactRepository.InsertContact(contact2);
                var result = contactRepository.GetContactsByName("Pablo");

                Assert.AreEqual(2, result.Count);
            }
        }

        [Test]
        public void GetContactByName()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            ContactRepository contactRepository = new ContactRepository(con);
            Contact contact = new Contact
            {
                Id = 1,
                FirstName = "Pablo",
                LastName = "Corral",
                Phone = "34676681420"
            };
            Contact contact1 = new Contact
            {
                Id = 2,
                FirstName = "Aa",
                LastName = "Mama",
                Phone = "34692511479"
            };
            Contact contact2 = new Contact
            {
                Id = 3,
                FirstName = "Pablo",
                LastName = "No existe",
                Phone = "34666666666"
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                contactRepository.InsertContact(contact1);
                contactRepository.InsertContact(contact2);
                var result = contactRepository.GetContactByName("Pablo Corral");

                Assert.AreEqual(1, result.Id);
            }
        }

        [Test]
        public void GetContactsName()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            ContactRepository contactRepository = new ContactRepository(con);
            Contact contact = new Contact
            {
                Id = 1,
                FirstName = "Pablo",
                LastName = "Corral",
                Phone = "34676681420"
            };
            Contact contact1 = new Contact
            {
                Id = 2,
                FirstName = "Aa",
                LastName = "Mama",
                Phone = "34692511479"
            };
            Contact contact2 = new Contact
            {
                Id = 3,
                FirstName = "Pablo",
                LastName = "No existe",
                Phone = "34666666666"
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                contactRepository.InsertContact(contact1);
                contactRepository.InsertContact(contact2);
                var result = contactRepository.GetContactsName();

                Assert.AreEqual("Aa Mama", result[0]);
            }
        }

        [Test]
        public void GetContactsStatus()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            ContactRepository contactRepository = new ContactRepository(con);
            Contact contact = new Contact
            {
                Id = 1,
                FirstName = "Pablo",
                LastName = "Corral",
                Phone = "34676681420",
                Status = "Offline"
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                var result = contactRepository.GetContactsStatus();
                Assert.AreEqual("Pablo Corral - Offline", result[0]);
            }
        }

        [Test]
        public void UpdateContacts()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            ContactRepository contactRepository = new ContactRepository(con);
            Contact contact = new Contact
            {
                Id = 1,
                FirstName = "Pablo",
                LastName = "Corral",
                Phone = "34676681420",
                Status = "Offline"
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                contact.FirstName = "Pableras";
                contactRepository.UpdateContact(contact);
                var result = contactRepository.GetContacts();
                Assert.AreEqual("Pableras", result[0].FirstName);
            }
        }

        [Test]
        public void GetBlockedContacts()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            ContactRepository contactRepository = new ContactRepository(con);
            Contact contact = new Contact
            {
                Id = 1,
                FirstName = "Pablo",
                LastName = "Corral",
                Phone = "34676681420",
                Status = "Offline",
                Blocked = false
            };
            Contact contact1 = new Contact
            {
                Id = 1,
                FirstName = "Pableras",
                LastName = "Corral",
                Phone = "34666666666",
                Status = "Offline",
                Blocked = true
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                contactRepository.InsertContact(contact1);
                var result = contactRepository.GetBlocked();
                Assert.AreEqual("Pableras", result[0].FirstName);
            }
        }

        [Test]
        public void GetBlockedNames()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            ContactRepository contactRepository = new ContactRepository(con);
            Contact contact = new Contact
            {
                Id = 1,
                FirstName = "Pablo",
                LastName = "Corral",
                Phone = "34676681420",
                Status = "Offline",
                Blocked = false
            };
            Contact contact1 = new Contact
            {
                Id = 1,
                FirstName = "Pableras",
                LastName = "Corral",
                Phone = "34666666666",
                Status = "Offline",
                Blocked = true
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                contactRepository.InsertContact(contact1);
                var result = contactRepository.GetBlockedName();
                Assert.AreEqual("Pableras Corral", result[0]);
            }
        }

        [Test]
        public void DeleteContact()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            ContactRepository contactRepository = new ContactRepository(con);
            Contact contact = new Contact
            {
                Id = 1,
                FirstName = "Pablo",
                LastName = "Corral",
                Phone = "34676681420",
                Status = "Offline",
                Blocked = false
            };
            Contact contact1 = new Contact
            {
                Id = 1,
                FirstName = "Pableras",
                LastName = "Corral",
                Phone = "34666666666",
                Status = "Offline",
                Blocked = true
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                contactRepository.InsertContact(contact1);
                var result = contactRepository.DeleteContact(contact1);
                var list = contactRepository.GetContacts();
                Assert.AreEqual(1, list.Count);
            }
        }

        [Test]
        public void DeleteContacts()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            ContactRepository contactRepository = new ContactRepository(con);
            Contact contact = new Contact
            {
                Id = 1,
                FirstName = "Pablo",
                LastName = "Corral",
                Phone = "34676681420",
                Status = "Offline",
                Blocked = false
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                var result = contactRepository.DeleteContacts();
                var list = contactRepository.GetContacts();
                Assert.AreEqual(0, list.Count);
            }
        }

        [Test]
        public void GetContactById()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            ContactRepository contactRepository = new ContactRepository(con);
            Contact contact = new Contact
            {
                Id = 1,
                FirstName = "Pablo",
                LastName = "Corral",
                Phone = "34676681420",
                Status = "Offline",
                Blocked = false
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                var result = contactRepository.GetContactById(1);
                Assert.AreEqual("Pablo", result.FirstName);
            }
        }

        [Test]
        public void GetContactByPhone()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            ContactRepository contactRepository = new ContactRepository(con);
            Contact contact = new Contact
            {
                Id = 1,
                FirstName = "Pablo",
                LastName = "Corral",
                Phone = "34676681420",
                Status = "Offline",
                Blocked = false
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                var result = contactRepository.GetContactByPhone("34676681420");
                Assert.AreEqual("Pablo", result.FirstName);
            }
        }

        [Test]
        public void GetContactsByNameWithChat()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            MessageRepository messageRepository = new MessageRepository(con);
            ContactRepository contactRepository = new ContactRepository(con);
            Contact contact = new Contact
            {
                Id = 1,
                FirstName = "Pablo",
                LastName = "Corral",
                Phone = "34676681420"
            };
            Contact c = new Contact
            {
                Id = 2,
                FirstName = "Cristina",
                LastName = "Gambin",
                Phone = "34666666666"
            };
            Chat m = new Chat
            {
                Mensaje = "hola",
                FromTo = "34676681420",
                Send = true,
                Created = DateTime.Now,
                Seen = true
            };
            Chat m1 = new Chat
            {
                Mensaje = "hola",
                FromTo = "34666666666",
                Send = true,
                Created = DateTime.Now,
                Seen = true
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                contactRepository.InsertContact(c);
                messageRepository.InsertMessage(m);
                messageRepository.InsertMessage(m1);
                var result = contactRepository.GetContactsByNameWithChat("Pablo");
                Assert.AreEqual(1, result.Count);
            }
        }

        [Test]
        public void GetContactsByNameWithChatC2()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            MessageRepository messageRepository = new MessageRepository(con);
            ContactRepository contactRepository = new ContactRepository(con);
            Contact contact = new Contact
            {
                Id = 1,
                FirstName = "Pablo",
                LastName = "Corral",
                Phone = "34676681420"
            };
            Contact c = new Contact
            {
                Id = 2,
                FirstName = "Dani",
                LastName = "Pablo",
                Phone = "34666666666"
            };
            Chat m = new Chat
            {
                Mensaje = "hola",
                FromTo = "34676681420",
                Send = true,
                Created = DateTime.Now,
                Seen = true
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                contactRepository.InsertContact(c);
                messageRepository.InsertMessage(m);
                var result = contactRepository.GetContactsByNameWithChat("Pablo");
                Assert.AreEqual(1, result.Count);
            }
        }

        [Test]
        public void GetContactsByNameWithChatC3()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            MessageRepository messageRepository = new MessageRepository(con);
            ContactRepository contactRepository = new ContactRepository(con);
            Contact contact = new Contact
            {
                Id = 1,
                FirstName = "Pablo",
                LastName = "Corral",
                Phone = "34676681420"
            };
            Contact c = new Contact
            {
                Id = 2,
                FirstName = "Dani",
                LastName = "Pablo",
                Phone = "34666666666"
            };
            Chat m = new Chat
            {
                Mensaje = "hola",
                FromTo = "34676681420",
                Send = true,
                Created = DateTime.Now,
                Seen = true
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                contactRepository.InsertContact(c);
                messageRepository.InsertMessage(m);
                var result = contactRepository.GetContactsByNameWithChat("Dani Pablo");
                Assert.AreEqual(0, result.Count);
            }
        }
    }
}