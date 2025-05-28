using System.Collections;

namespace Profiling;

public record StageMetrics(string Name, TimeSpan Elapsed);

/// <summary>
/// A stage-based profiler for measuring cumulative execution times of named code regions.
/// </summary>
/// <remarks>
/// It is NOT thread-safe and should only be used from a single thread.
/// </remarks>
public class Profiler : IEnumerable<StageMetrics>
{
    private readonly Dictionary<string, StageTimer> stages = [];
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
            return new StageScope(value);

        StageTimer st = new(name);
        stages.Add(name, st);
        stagesOrder.Add(name);
        return new StageScope(st);
    }

    public StageMetrics this[string name] => new(name, stages[name].TotalElapsed);

    public IEnumerator<StageMetrics> GetEnumerator()
    {
        foreach (var stageName in stagesOrder)
        {
            StageTimer st = stages[stageName];
            yield return new(stageName, st.TotalElapsed);
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
