using DummyXamarin.Repositories;
using TeleSharp.TL;

namespace DummyXamarin.Interfaces.IServices
{
    public interface IUserService
    {
        bool CheckUsername(TLClient client, string username);
        TLUser UpdateUsername(TLClient client, string username, UserRepository userRepository);
    }
}