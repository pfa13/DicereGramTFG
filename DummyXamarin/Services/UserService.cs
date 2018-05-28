using System;
using System.Threading.Tasks;
using DummyXamarin.Interfaces.IServices;
using DummyXamarin.Repositories;
using TeleSharp.TL;
using TeleSharp.TL.Account;

namespace DummyXamarin.Services
{
    public class UserService : IUserService
    {
        public bool CheckUsername(TLClient client, string username)
        {
            try
            {
                TLRequestCheckUsername req = new TLRequestCheckUsername
                {
                    Username = username
                };
                var l = Task.Run(() => client.SendRequestAsync<bool>(req));
                l.Wait();
                if (l.Result)
                    return true;
                else
                    return false;
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        public TLUser UpdateUsername(TLClient client, string username, UserRepository userRepository)
        {
            try
            {
                TLUser user = null;
                TLRequestUpdateUsername request = new TLRequestUpdateUsername
                {
                    Username = username
                };
                var u = Task.Run(() => client.SendRequestAsync<TLUser>(request));
                u.Wait();
                user = u.Result;
                userRepository.UpdateUser(username);
                return user;
            }
            catch (Exception ex)
            {
                throw;
            }            
        }
    }
}