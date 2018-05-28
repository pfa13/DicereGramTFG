using System;
using System.Threading.Tasks;
using DummyXamarin.Interfaces.IServices;
using TeleSharp.TL;
using TeleSharp.TL.Auth;

namespace DummyXamarin.Services
{
    public class LoginService : ILoginService
    {
        private int api_id = 00;
        private string api_hash = "xx";

        public TLClient Connect()
        {
            try
            {
                FakeSessionStore session = new FakeSessionStore();
                TLClient client = new TLClient(api_id, api_hash, session, "session");
                var t = Task.Run(() => client.ConnectAsync());
                t.Wait();
                return client;
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        public bool IsPhoneRegistered(TLClient client, string phone)
        {
            try
            {
                var r = Task.Run(() => client.IsPhoneRegisteredAsync(phone));
                r.Wait();
                return r.Result;
            }
            catch(Exception ex)
            {
                throw;
            }
        }
        
        public string SendCodeRequest(TLClient client, string phone)
        {
            try
            {
                var ha = Task.Run(() => client.SendCodeRequestAsync(phone));
                //var ha = client.SendCodeRequestAsync(phone);
                ha.Wait();
                return ha.Result;
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        public TLUser MakeAuth(TLClient client, string phone, string hash, string code)
        {
            try
            {
                var auth = Task.Run(() => client.MakeAuthAsync(phone, hash, code));
                auth.Wait();
                return auth.Result;
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        public TLUser SignUp(TLClient client, string phone, string hash, string code, string firstname, string lastname)
        {
            try
            {
                var sign = Task.Run(() => client.SignUpAsync(phone, hash, code, firstname, lastname));
                sign.Wait();
                return sign.Result;
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        public bool Logout(TLClient client)
        {
            try
            {
                TLRequestLogOut req = new TLRequestLogOut();
                var result = Task.Run(() => client.SendRequestAsync<bool>(req));
                result.Wait();
                if (result.Result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}