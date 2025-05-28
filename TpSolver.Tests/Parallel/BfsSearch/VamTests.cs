using Profiling;
using TpSolver.BfsSearch;
using TpSolver.Tests.Utils;

namespace TpSolver.Tests.Parallel.BfsSearch;

public class VamTests
{
    [Theory]
    [InlineData(200)]
    [InlineData(300)]
    [InlineData(500)]
    public void Search_ShouldBeCorrect_SquareTP(int size)
    {
        TransportProblem tp = TransportProblem.GenerateRandom(size, size);

        var vamSeq = new Vam(tp);
        var vamPar = new VamParallel(tp, new() { MaxDegreeOfParallelism = 6 });
        var expected = vamSeq.Search();
        var actual = vamPar.Search();
        var speedup = vamSeq.Profiler.First().Elapsed / vamPar.Profiler.First().Elapsed;

        var costSeq = expected.CalcTotalCost(tp.Cost);
        var costPar = actual.CalcTotalCost(tp.Cost);
        Assert.True(costPar == costSeq);
        Assert.Equal(expected.AsEnumerable(), actual.AsEnumerable());
        Assert.True(speedup > 1.2);
        Console.WriteLine($"[Perf vam] Size={size}, S={speedup}");
    }

    [Theory]
    [InlineData(3, 200)]
    [InlineData(3, 300)]
    [InlineData(3, 500)]
    public void Search_ShouldBeCorrect_OneSideSmallTP(int m, int n)
    {
        TransportProblem tp = TransportProblem.GenerateRandom(m, n);

        var vamSeq = new Vam(tp);
        var vamPar = new VamParallel(tp, new() { MaxDegreeOfParallelism = 6 });
        var expected = vamSeq.Search();
        var actual = vamPar.Search();
        var speedup = vamSeq.Profiler.First().Elapsed / vamPar.Profiler.First().Elapsed;

        var costSeq = expected.CalcTotalCost(tp.Cost);
        var costPar = actual.CalcTotalCost(tp.Cost);
        Assert.True(costPar == costSeq);
        Assert.Equal(expected.AsEnumerable(), actual.AsEnumerable());
    }
}
