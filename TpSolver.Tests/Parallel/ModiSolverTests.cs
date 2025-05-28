using System.Security.Authentication.ExtendedProtection;
using TpSolver.Shared;
using TpSolver.Solver.Modi;
using TpSolver.Tests.Utils;

namespace TpSolver.Tests.Parallel;

public class ModiSolverTests
{
    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    public void ArgminNBCostDiffPotential_ShouldBeCorrect(int size)
    {
        TransportProblem tp = TransportProblem.GenerateRandom(size, size);

        var seq = new ModiSolver(tp);
        var par = new ModiSolverParallel(tp, new() { MaxDegreeOfParallelism = 6 });

        AllocationMatrix? expected = seq.Solve(out int _);
        AllocationMatrix? actual = par.Solve(out int _);
        Assert.NotNull(expected);
        Assert.NotNull(actual);
        Assert.Equal(expected.AsEnumerableNBDistinct(), actual.AsEnumerableNBDistinct());

        var seqT = seq.Profiler.Skip(2).First().Elapsed;
        var parT = par.Profiler.Skip(2).First().Elapsed;
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

        var seq = new ModiSolver(tp);
        var par = new ModiSolverParallel(tp, new() { MaxDegreeOfParallelism = 6 });

        AllocationMatrix? expected = seq.Solve(out int _);
        AllocationMatrix? actual = par.Solve(out int _);
        Assert.NotNull(expected);
        Assert.NotNull(actual);
        Assert.Equal(expected.AsEnumerableNBDistinct(), actual.AsEnumerableNBDistinct());

        var seqT = seq.Profiler.First().Elapsed;
        var parT = par.Profiler.First().Elapsed;
        var speedup = seqT / parT;
        Assert.True(speedup > 1.2, $"Speedup is too small - {speedup}");
        Console.WriteLine($"[Perf modi solve] Size={size}, S={speedup}");
    }
}
