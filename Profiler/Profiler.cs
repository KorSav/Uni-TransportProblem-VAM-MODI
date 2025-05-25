namespace Profiler;

public class Profiler
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
