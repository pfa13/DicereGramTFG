using System.IO;

namespace DummyXamarin.Utils
{
    class FakeSessionDelete
    {
        public bool DeleteSession()
        {
            string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string file = Path.Combine(documentsPath, "session.dat");

            File.Delete(file);
            if (!File.Exists(file))
                return true;
            else
                return false;
        }
    }
}