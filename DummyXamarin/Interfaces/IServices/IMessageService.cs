using DummyXamarin.Repositories;
using TeleSharp.TL;

namespace DummyXamarin.Interfaces.IServices
{
    public interface IMessageService
    {
        bool SendTyping(TLClient client, int id, long accesshash);
        TLAbsUpdates SendMessage(TLClient client, int id, string message);
        int ReceiveMessages(TLClient client, MessageRepository messageRepository);
    }
}