using DummyXamarin.Models;

namespace DummyXamarin.Interfaces.IRepositories
{
    public interface IConfigRepository
    {
        Models.Config GetConfig();
        bool InsertConfig(Config configuracion);
        bool UpdateConfig(Config configuracion);
    }
}