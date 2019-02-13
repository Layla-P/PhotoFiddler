using System.Threading.Tasks;

namespace PhotoFiddler.Helpers
{
    public interface IPhotoProcessor
    {
        Task<string> Process(string incomingImageUrl, string sid, string host);
        
    }
}