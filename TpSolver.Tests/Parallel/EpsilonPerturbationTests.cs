using TpSolver.BfsSearch;
using TpSolver.CycleSearch;
using TpSolver.Perturbation;
using TpSolver.Shared;
using TpSolver.Tests.Utils;
using static TpSolver.Perturbation.EpsilonPerturbation;

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

        var epSeq = new EpsilonPerturbation(expected, degenerousTp.Cost) { Profiler = new() };
        var isSeqSuccess = epSeq.TryPerturb(perturbCount);

        var epPar = new EpsilonPerturbationParallel(actual, degenerousTp.Cost, 6)
        {
            Profiler = new(),
        };
        var isParSuccess = epPar.TryPerturb(perturbCount);
        epPar.CycleSearcher = new CycleSearcherParallel(actual, 6) { Profiler = new() };

        Assert.Equal(isSeqSuccess, isParSuccess);
        var seqT = epSeq.Profiler[Stages.Total].Elapsed;
        var parT = epPar.Profiler[Stages.Total].Elapsed;
        var speedup = seqT / parT;

        var costSeq = expected.CalcTotalCost(degenerousTp.Cost);
        var costPar = actual.CalcTotalCost(degenerousTp.Cost);
        Assert.True(costPar == costSeq);
        Assert.Equal(expected.AsEnumerableNBDistinct(), actual.AsEnumerableNBDistinct());
        Assert.True(speedup > 1.2, $"Speedup is too small - {speedup}");
        Console.WriteLine($"[Perf perturb] Size={size}, S={speedup}");
    }
}
