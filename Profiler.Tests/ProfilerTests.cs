namespace Profiler.Tests;

public class ProfilerTests
{
    [Fact]
    public void StartStage_ShouldStartTimer_AndStopWhenScopeEnds()
    {
        var sleepTime = TimeSpan.FromMilliseconds(10);
        var expected = new StageMetrics("test", sleepTime);

        Profiler profiler = new();
        using (profiler.StartStage("test"))
            Thread.Sleep(sleepTime);

        var actual = profiler.Single();
        Assert.Equal(expected.Name, actual.Name);
        Assert.True(expected.Elapsed < actual.Elapsed);
    }

    [Theory]
    [InlineData(50, 10)]
    public void StartStage_ShouldPreserveOrderOfStages(int size, int sleepTimeMs)
    {
        var sleepTime = TimeSpan.FromMilliseconds(sleepTimeMs);
        List<StageMetrics> expected = new(size);
        for (int i = 0; i < size; i++)
            expected.Add(new($"Stage_{Guid.NewGuid()}", sleepTime));

        Profiler profiler = new();
        foreach (var (name, _) in expected)
            using (profiler.StartStage(name))
                Thread.Sleep(sleepTime);

        List<StageMetrics> actual = profiler.ToList();
        Assert.Equal(expected.Select(sm => sm.Name), actual.Select(sm => sm.Name));
        expected
            .Zip(actual)
            .Select((pair, i) => (exp: pair.First, act: pair.Second, i))
            .ToList()
            .ForEach(
                (item) =>
                    Assert.True(
                        item.act.Elapsed >= item.exp.Elapsed,
                        $"Element at index {item.i}: {item.act.Elapsed} is less than {item.exp.Elapsed}"
                    )
            );
    }

    [Fact]
    public void StartStage_ShouldSupportNestedStages()
    {
        var sleepTime = TimeSpan.FromMilliseconds(50);
        List<StageMetrics> expected = [new("outer", sleepTime * 2), new("inner", sleepTime)];

        Profiler profiler = new();
        using (profiler.StartStage("outer"))
        {
            Thread.Sleep(sleepTime);
            using (profiler.StartStage("inner"))
                Thread.Sleep(sleepTime);
        }

        var actual = profiler.ToList();
        Assert.Equal(2, actual.Count);
        Assert.Equal(expected.Select(sm => sm.Name), actual.Select(sm => sm.Name));
        expected
            .Zip(actual)
            .Select((pair, i) => (exp: pair.First, act: pair.Second, i))
            .ToList()
            .ForEach(
                (item) =>
                    Assert.True(
                        item.act.Elapsed >= item.exp.Elapsed,
                        $"Element at index {item.i}: {item.act.Elapsed} is less than {item.exp.Elapsed}"
                    )
            );
    }
}
