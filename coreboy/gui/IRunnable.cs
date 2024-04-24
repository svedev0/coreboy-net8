using System.Threading;

namespace coreboy.gui
{
    public interface IRunnable
    {
        void Run(CancellationToken token);
    }
}