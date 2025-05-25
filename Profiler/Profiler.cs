using System.Collections;

namespace Profiler;

public record StageMetrics(string Name, TimeSpan Elapsed);

public class Profiler : IEnumerable<StageMetrics>
{
    private readonly Dictionary<string, StageTimer> stages = [];
    private readonly List<string> stagesOrder = [];

    public IDisposable StartStage(string name)
    {
        if (stages.TryGetValue(name, out StageTimer? value))
            return new StageScope(value);

        StageTimer st = new(name);
        stages.Add(name, st);
        stagesOrder.Add(name);
        return new StageScope(st);
    }

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
}
