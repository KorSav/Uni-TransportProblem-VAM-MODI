using System.Diagnostics;

namespace Profiling;

class StageTimer(string name)
{
    private readonly Stopwatch sw = new();
    public string StageName { get; } = name;
    public TimeSpan TotalElapsed { get; private set; } = TimeSpan.Zero;

    public void Start() => sw.Restart();

    public void Stop()
    {
        sw.Stop();
        TotalElapsed += sw.Elapsed;
    }
}
