using System.Threading.Tasks;

namespace Metamatic {
    public interface ITaskDriven {
        Task GetTask();
        void Cancel();
    }
}