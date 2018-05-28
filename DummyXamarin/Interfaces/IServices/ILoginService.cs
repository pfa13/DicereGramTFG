using TeleSharp.TL;

namespace DummyXamarin.Interfaces.IServices
{
    public interface ILoginService
    {
        TLClient Connect();
        bool IsPhoneRegistered(TLClient client, string phone);
        string SendCodeRequest(TLClient client, string phone);
        TLUser MakeAuth(TLClient client, string phone, string hash, string code);
        TLUser SignUp(TLClient client, string phone, string hash, string code, string firstname, string lastname);
        bool Logout(TLClient client);
    }
}