using TpSolver.BfsSearch;
using TpSolver.Perturbation;
using TpSolver.Shared;
using TpSolver.Tests.Utils;

namespace TpSolver.Tests.Parallel;

public class EpsilonPerturbationTests
{
    [Theory]
    [InlineData(200)]
    [InlineData(400)]
    public void Perturb_ShouldBeCorrect_SquareTP(int size)
    {
        TPValueLimits limits = new(
            supplyLowerBound: 10,
            supplyUpperBound: 11,
            demandLowerBound: 10,
            demandUpperBound: 11
        );
        TransportProblem degenerousTp = TransportProblem.GenerateRandom(size, size, limits);

        var expected = new Vam(degenerousTp).Search();
        AllocationMatrix actual = new(expected);
        int perturbCount = actual.NRows + actual.NCols - 1 - actual.Count(a => a.IsBasic);

        var ep = new EpsilonPerturbation(expected, degenerousTp.Cost);
        var isSeqSuccess = ep.TryPerturb(perturbCount);

        var ep2 = new EpsilonPerturbationParallel(
            actual,
            degenerousTp.Cost,
            new() { MaxDegreeOfParallelism = 6 }
        );
        var isParSuccess = ep2.TryPerturb(perturbCount);

        Assert.Equal(isSeqSuccess, isParSuccess);
        var s1 = ep.Profiler.First();
        var s2 = ep2.Profiler.First();
        var seqT = ep.Profiler.First().Elapsed;
        var parT = ep2.Profiler.First().Elapsed;
        var speedup = seqT / parT;

        var costSeq = expected.CalcTotalCost(degenerousTp.Cost);
        var costPar = actual.CalcTotalCost(degenerousTp.Cost);
        Assert.True(costPar == costSeq);
        Assert.Equal(expected.Count(a => a.IsBasic), actual.Count(a => a.IsBasic));
        Assert.Equal(expected.AsEnumerableNBDistinct(), actual.AsEnumerableNBDistinct());
        Assert.True(speedup > 1.2);
        Console.WriteLine($"[Perf perturb] Size={size}, S={speedup}");
        var orderedTimings = ep2.CycleProfiler.OrderByDescending(sm => sm.Elapsed);
    }
}
