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
    public void Search_ShouldBeCorrect(int size)
    {
        TransportProblem tp = TransportProblem.GenerateRandom(size, size - 1);

        var expected = new Vam(tp).Search(out Profiler profilerSeq);
        var actual = new VamParallel(tp, new() { MaxDegreeOfParallelism = 6 }).Search(
            out Profiler profilerPar
        );
        var speedup = profilerSeq.First().Elapsed / profilerPar.First().Elapsed;

        var costSeq = expected.CalcTotalCost(tp.Cost);
        var costPar = actual.CalcTotalCost(tp.Cost);
        Assert.True(costPar == costSeq);
        Assert.Equal(expected.AsEnumerable(), actual.AsEnumerable());
        Assert.True(speedup > 1.2);
        Console.WriteLine($"[Perf] Size={size}, S={speedup}");
    }
}
