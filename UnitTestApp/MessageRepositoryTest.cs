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
    public class MessageRepositoryTest
    {
        [SetUp]
        public void Setup()
        {            
            
        }


        [TearDown]
        public void Tear()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.DeleteDataDatabase();
        }

        [Test]
        public void GetMessageC1()
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
            Chat m = new Chat
            {
                Id = 0,
                Mensaje = "hola",
                FromTo = "34676681420",
                Send = true,
                Created = DateTime.Now
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                messageRepository.InsertMessage(m);
                var result = messageRepository.GetMessages().Count;
                Assert.AreEqual(1, result);
            }
        }

        [Test]
        public void GetMessageC2()
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
            Chat m = new Chat
            {
                Mensaje = "hola",
                FromTo = "34676681420",
                Send = true,
                Created = DateTime.Now
            };
            Chat m1 = new Chat
            {
                Mensaje = "hola, que tal?",
                FromTo = "34676681420",
                Send = false,
                Created = DateTime.Now.AddSeconds(10)
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                messageRepository.InsertMessage(m);
                messageRepository.InsertMessage(m1);
                var result = messageRepository.GetMessages().Count;
                Assert.AreEqual(2, result);
            }
        }

        [Test]
        public void GetMessageC3()
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
                Created = DateTime.Now
            };
            Chat m1 = new Chat
            {
                Mensaje = "hola, que tal?",
                FromTo = "34676681420",
                Send = false,
                Created = DateTime.Now.AddSeconds(10)
            };
            Chat m2 = new Chat
            {
                Mensaje = "hola, que tal?",
                FromTo = "34666666666",
                Send = false,
                Created = DateTime.Now.AddSeconds(10)
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                contactRepository.InsertContact(c);
                messageRepository.InsertMessage(m);
                messageRepository.InsertMessage(m1);
                messageRepository.InsertMessage(m2);
                var result = messageRepository.GetMessagesByPhone("34676681420").Count;
                Assert.AreEqual(2, result);
            }
        }

        [Test]
        public void GetMessagesOrdered()
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
            Contact c1 = new Contact
            {
                Id = 3,
                FirstName = "Zoe",
                LastName = "",
                Phone = "34677777777"
            };
            Chat m = new Chat
            {
                Mensaje = "hola",
                FromTo = "34676681420",
                Send = true,
                Created = DateTime.Now.AddDays(-4).AddHours(-5),
                Seen = false
            };
            Chat m1 = new Chat
            {
                Mensaje = "hola, que tal?",
                FromTo = "34676681420",
                Send = false,
                Created = DateTime.Now.AddDays(-3).AddHours(-4),
                Seen = false
            };
            Chat m2 = new Chat
            {
                Mensaje = "soy pablo",
                FromTo = "34676681420",
                Send = false,
                Created = DateTime.Now.AddDays(-3).AddHours(-2),
                Seen = false
            };
            Chat m3 = new Chat
            {
                Mensaje = "hola, que tal pau?",
                FromTo = "34666666666",
                Send = false,
                Created = DateTime.Now.AddDays(-3),
                Seen = false
            };            
            Chat m4 = new Chat
            {
                Mensaje = "soy cristina",
                FromTo = "34666666666",
                Send = false,
                Created = DateTime.Now.AddDays(-2),
                Seen = false
            };
            Chat m5 = new Chat
            {
                Mensaje = "soy zoe",
                FromTo = "34677777777",
                Send = false,
                Created = DateTime.Now.AddDays(-2),
                Seen = false
            };
            Chat m6 = new Chat
            {
                Mensaje = "holii",
                FromTo = "34677777777",
                Send = false,
                Created = DateTime.Now.AddDays(-1),
                Seen = false
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                contactRepository.InsertContact(c);
                contactRepository.InsertContact(c1);
                messageRepository.InsertChat(m);
                messageRepository.InsertChat(m1);
                messageRepository.InsertChat(m2);
                messageRepository.InsertChat(m3);
                messageRepository.InsertChat(m4);
                messageRepository.InsertChat(m5);
                messageRepository.InsertChat(m6);
                var result = messageRepository.GetMessagesOrdered();
                Assert.AreEqual(result[0].Mensaje, "holii");
            }
        }

        [Test]
        public void GetMessagesNotReaded()
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
                Mensaje = "hola, que tal?",
                FromTo = "34676681420",
                Send = false,
                Created = DateTime.Now.AddSeconds(10),
                Seen = false
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                messageRepository.InsertMessage(m);
                messageRepository.InsertMessage(m1);
                var result = messageRepository.GetMessagesNotReaded().Count;
                Assert.AreEqual(1, result);
            }
        }

        [Test]
        public void CountMessagesNotReaded()
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
                Send = false,
                Created = DateTime.Now,
                Seen = false
            };
            Chat m1 = new Chat
            {
                Mensaje = "hola, que tal?",
                FromTo = "34676681420",
                Send = false,
                Created = DateTime.Now.AddSeconds(10),
                Seen = false
            };
            Chat m2 = new Chat
            {
                Mensaje = "hola, que tal?",
                FromTo = "34666666666",
                Send = false,
                Created = DateTime.Now.AddSeconds(-60),
                Seen = false
            };
            Chat m3 = new Chat
            {
                Mensaje = "hola, que tal?",
                FromTo = "34666666666",
                Send = false,
                Created = DateTime.Now.AddSeconds(-120),
                Seen = false
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                contactRepository.InsertContact(c);
                messageRepository.InsertMessage(m);
                messageRepository.InsertMessage(m1);
                messageRepository.InsertMessage(m2);
                messageRepository.InsertMessage(m2);
                var result = messageRepository.CountMessagesNotReaded();
                Assert.AreEqual(2, result[0].Counter);
            }
        }

        [Test]
        public void CheckExistFromContact()
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
                messageRepository.InsertMessage(m);
                var result = messageRepository.CheckExistFromContact("34666666666");
                Assert.IsFalse(result);
            }
        }

        [Test]
        public void GetMessagesByPhoneAndMessage()
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
                Mensaje = "que tal estas",
                FromTo = "34676681420",
                Send = true,
                Created = DateTime.Now,
                Seen = true
            };
            Chat m2 = new Chat
            {
                Mensaje = "donde vamos",
                FromTo = "34676681420",
                Send = true,
                Created = DateTime.Now,
                Seen = true
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                messageRepository.InsertMessage(m);
                messageRepository.InsertMessage(m1);
                messageRepository.InsertMessage(m2);
                var result = messageRepository.GetMessagesByPhoneAndMessage("34676681420", "que tal");
                Assert.AreEqual(2, result.Count);
            }
        }

        [Test]
        public void GetMessagesByPhoneAndDate()
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
            Chat m = new Chat
            {
                Mensaje = "hola",
                FromTo = "34676681420",
                Send = true,
                Created = DateTime.Now.AddDays(-4),
                Seen = true
            };
            Chat m1 = new Chat
            {
                Mensaje = "que tal estas",
                FromTo = "34676681420",
                Send = true,
                Created = DateTime.Now.AddDays(-3),
                Seen = true
            };
            Chat m2 = new Chat
            {
                Mensaje = "donde vamos",
                FromTo = "34676681420",
                Send = true,
                Created = DateTime.Now,
                Seen = true
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                messageRepository.InsertChat(m);
                messageRepository.InsertChat(m1);
                messageRepository.InsertChat(m2);
                var result = messageRepository.GetMessagesByPhoneAndDate("34676681420", DateTime.Now.AddDays(-1));
                Assert.AreEqual(1, result.Count);
            }
        }

        [Test]
        public void MarkMessagesAsRead()
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
            Chat m = new Chat
            {
                Mensaje = "hola",
                FromTo = "34676681420",
                Send = true,
                Created = DateTime.Now.AddDays(-4),
                Seen = true
            };
            Chat m1 = new Chat
            {
                Mensaje = "que tal estas",
                FromTo = "34676681420",
                Send = true,
                Created = DateTime.Now.AddDays(-3),
                Seen = false
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                messageRepository.InsertChat(m);
                messageRepository.InsertChat(m1);
                messageRepository.MarkMessagesAsRead("34676681420");
                var result = messageRepository.GetMessagesByPhone("34676681420");
                Assert.AreEqual(2, result.Select(x => x.Seen == true).Count());
            }
        }

        [Test]
        public void DeleteMessages()
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
            Chat m = new Chat
            {
                Mensaje = "hola",
                FromTo = "34676681420",
                Send = true,
                Created = DateTime.Now.AddDays(-4),
                Seen = true
            };
            Chat m1 = new Chat
            {
                Mensaje = "que tal estas",
                FromTo = "34676681420",
                Send = true,
                Created = DateTime.Now.AddDays(-3),
                Seen = false
            };
            using (var connection = con.GetConnection())
            {
                contactRepository.InsertContact(contact);
                messageRepository.InsertChat(m);
                messageRepository.InsertChat(m1);
                messageRepository.DeleteChat(contact);
                var result = messageRepository.GetMessagesByPhone("34676681420");                
                Assert.AreEqual(0, result.Count);
            }
        }
    }
}