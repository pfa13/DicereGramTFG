using System.Linq;
using Android.Database.Sqlite;
using DummyXamarin.Interfaces.IRepositories;
using DummyXamarin.Models;

namespace DummyXamarin.Repositories
{
    public class ConfigRepository : IConfigRepository
    {
        ISQLiteRepository _sqlite;

        public ConfigRepository(ISQLiteRepository sqlite)
        {
            _sqlite = sqlite;
        }

        public Models.Config GetConfig()
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    var result = connection.Query<Config>("SELECT * FROM Config");
                    return result.FirstOrDefault();
                }
            }
            catch (SQLiteException ex)
            {
                return null;
            }
        }

        public bool InsertConfig(Config configuracion)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    var result = connection.Insert(configuracion);
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }

        public bool UpdateConfig(Config configuracion)
        {
            try
            {
                using (var connection = _sqlite.GetConnection())
                {
                    var result = connection.Update(configuracion);
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                throw;
            }
        }
    }
}