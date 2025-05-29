using System.Collections;

namespace Profiling;

public record StageMetrics(string Name, TimeSpan TotalElapsed, int HitCount);

/// <summary>
/// A stage-based profiler for measuring cumulative execution times of named code regions.
/// </summary>
/// <remarks>
/// It is NOT thread-safe and should only be used from a single thread.
/// </remarks>
public class Profiler : IEnumerable<StageMetrics>
{
    private readonly Dictionary<string, StageTimer> stages = [];
    private readonly Dictionary<string, int> hitCounts = [];
    private readonly List<string> stagesOrder = [];

    /// <summary>
    /// Begins measuring time of the named stage.
    /// <para>
    /// If the stage has already been measured, the elapsed time will be accumulated.
    /// </para>
    /// </summary>
    /// <returns>IDisposable that stops timer on disposal.</returns>
    public IDisposable Measure(string name)
    {
        if (stages.TryGetValue(name, out StageTimer? value))
        {
            hitCounts[name]++;
            return new StageScope(value);
        }

        StageTimer st = new(name);
        stages.Add(name, st);
        hitCounts[name] = 1;
        stagesOrder.Add(name);
        return new StageScope(st);
    }

    public StageMetrics this[string name] => new(name, stages[name].TotalElapsed, hitCounts[name]);

    public IEnumerator<StageMetrics> GetEnumerator()
    {
        foreach (var name in stagesOrder)
        {
            StageTimer st = stages[name];
            yield return new(name, st.TotalElapsed, hitCounts[name]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private class StageScope : IDisposable
    {
        private readonly StageTimer st;

        public StageScope(StageTimer st)
        {
            this.st = st;
            st.Start();
        }

        public void Dispose() => st.Stop();
    }

    public static IDisposable NoOp() => new NoOpDisposable();

    private class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
