using TpSolver.Shared;
using TpSolver.Solver.Modi;
using TpSolver.Tests.Sequential.Utils;
using static TpSolver.Solver.Modi.ModiSolver;

namespace TpSolver.Tests.Parallel;

public class ModiSolverTests
{
    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    public void ArgminNBCostDiffPotential_ShouldBeCorrect(int size)
    {
        TransportProblem tp = TransportProblem.GenerateRandom(size, size);

        var seq = new ModiSolverSeq(tp) { Profiler = new() };
        var par = new ModiSolverParallel(tp, 6) { Profiler = new() };

        AllocationMatrix? expected = seq.Solve();
        AllocationMatrix? actual = par.Solve();
        Assert.NotNull(expected);
        Assert.NotNull(actual);
        Assert.Equal(expected.AsEnumerableNBDistinct(), actual.AsEnumerableNBDistinct());

        var seqT = seq.Profiler[Stages.MinDiffNonBasicCost_Potentials].TotalElapsed;
        var parT = par.Profiler[Stages.MinDiffNonBasicCost_Potentials].TotalElapsed;
        var speedup = seqT / parT;
        Assert.True(speedup > 1.2, $"Speedup is too small - {speedup}");
        Console.WriteLine($"[Perf modi argmin] Size={size}, S={speedup}");
    }

    [Theory]
    [InlineData(200)]
    [InlineData(300)]
    public void Solve_ShouldBeCorrect(int size)
    {
        TransportProblem tp = TransportProblem.GenerateRandom(size, size);

        var seq = new ModiSolverSeq(tp) { Profiler = new() };
        var par = new ModiSolverParallel(tp, 6) { Profiler = new() };

        AllocationMatrix? expected = seq.Solve();
        AllocationMatrix? actual = par.Solve();
        Assert.NotNull(expected);
        Assert.NotNull(actual);
        Assert.Equal(expected.AsEnumerableNBDistinct(), actual.AsEnumerableNBDistinct());

        var seqT = seq.Profiler.First().TotalElapsed;
        var parT = par.Profiler.First().TotalElapsed;
        var speedup = seqT / parT;
        Assert.True(speedup > 1.2, $"Speedup is too small - {speedup}");
        Console.WriteLine($"[Perf modi solve] Size={size}, S={speedup}");
    }
}
