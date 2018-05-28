using System;
using DummyXamarin.Models;
using DummyXamarin.Repositories;
using NUnit.Framework;


namespace UnitTestApp
{
    [TestFixture]
    public class UserRepositoryTest
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
        public void GetUser()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            UserRepository userRepo = new UserRepository(con);
            User user = new User
            {
                Phone = "34655095818"
            };
            using (var connection = con.GetConnection())
            {
                userRepo.InsertUser(user);
                var result = userRepo.GetUser();
                Assert.AreEqual("34655095818", result.Phone);
            }
        }

        [Test]
        public void UpdateUser()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            UserRepository userRepo = new UserRepository(con);
            User user = new User
            {
                Phone = "34655095818",
                Username = "paula"
            };
            using (var connection = con.GetConnection())
            {
                userRepo.InsertUser(user);
                userRepo.UpdateUser("pau");
                var result = userRepo.GetUser();
                Assert.AreEqual("pau", result.Username);
            }
        }
    }
}