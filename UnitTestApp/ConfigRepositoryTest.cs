using System.Linq;
using DummyXamarin.Repositories;
using NUnit.Framework;
using DummyXamarin.Models;

namespace UnitTestApp
{
    [TestFixture]
    public class ConfigRepositoryTest
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
        public void GetConfig()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            ConfigRepository configRepository = new ConfigRepository(con);
            Config c = new Config()
            {
                Phone = "34676681420",
                Voz = false,
                Velocidad = (float)0.5
            };
            using (var connection = con.GetConnection())
            {
                configRepository.InsertConfig(c);
                var result = configRepository.GetConfig();
                Assert.AreEqual(false, result.Voz);
            }
        }

        [Test]
        public void UpdateConfig()
        {
            SQLiteRepository con = new SQLiteRepository();
            con.CreateDatabase();
            ConfigRepository configRepository = new ConfigRepository(con);
            Config c = new Config()
            {
                Phone = "34676681420",
                Voz = false,
                Velocidad = (float)0.5
            };
            using (var connection = con.GetConnection())
            {
                configRepository.InsertConfig(c);
                c.Voz = true;
                configRepository.UpdateConfig(c);
                var result = configRepository.GetConfig();
                Assert.AreEqual(true, result.Voz);
            }
        }
    }
}