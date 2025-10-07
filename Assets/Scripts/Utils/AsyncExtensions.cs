using System.Threading.Tasks;

namespace Utils
{
    public static class AsyncExtensions
    {
        public static async void DoAsync(this Task task)
        {
            await task;
        }
    }
}