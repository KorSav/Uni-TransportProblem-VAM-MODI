namespace Profiling.Tests;

public class ProfilerTests
{
    [Fact]
    public void StartStage_ShouldStartTimer_AndStopWhenScopeEnds()
    {
        var sleepTime = TimeSpan.FromMilliseconds(10);
        var expected = new StageMetrics("test", sleepTime, 1);

        Profiler profiler = new();
        using (profiler.Measure("test"))
            Thread.Sleep(sleepTime);

        var actual = profiler.Single();
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.HitCount, actual.HitCount);
        Assert.True(expected.TotalElapsed < actual.TotalElapsed);
    }

    [Theory]
    [InlineData(50, 10)]
    public void StartStage_ShouldPreserveOrderOfStages(int size, int sleepTimeMs)
    {
        var sleepTime = TimeSpan.FromMilliseconds(sleepTimeMs);
        List<StageMetrics> expected = new(size);
        for (int i = 0; i < size; i++)
            expected.Add(new($"Stage_{Guid.NewGuid()}", sleepTime, 1));

        Profiler profiler = new();
        foreach (var (name, _, _) in expected)
            using (profiler.Measure(name))
                Thread.Sleep(sleepTime);

        List<StageMetrics> actual = profiler.ToList();
        Assert.Equal(expected.Select(sm => sm.Name), actual.Select(sm => sm.Name));
        expected
            .Zip(actual)
            .Select((pair, i) => (exp: pair.First, act: pair.Second, i))
            .ToList()
            .ForEach(
                (item) =>
                {
                    Assert.True(
                        item.act.TotalElapsed >= item.exp.TotalElapsed,
                        $"Element at index {item.i}: {item.act.TotalElapsed} is less than {item.exp.TotalElapsed}"
                    );
                    Assert.Equal(item.exp.HitCount, item.act.HitCount);
                }
            );
    }

    [Fact]
    public void StartStage_ShouldSupportNestedStages()
    {
        var sleepTime = TimeSpan.FromMilliseconds(50);
        List<StageMetrics> expected = [new("outer", sleepTime * 2, 1), new("inner", sleepTime, 1)];

        Profiler profiler = new();
        using (profiler.Measure("outer"))
        {
            Thread.Sleep(sleepTime);
            using (profiler.Measure("inner"))
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
                {
                    Assert.True(
                        item.act.TotalElapsed >= item.exp.TotalElapsed,
                        $"Element at index {item.i}: {item.act.TotalElapsed} is less than {item.exp.TotalElapsed}"
                    );
                    Assert.Equal(item.exp.HitCount, item.act.HitCount);
                }
            );
    }
}
