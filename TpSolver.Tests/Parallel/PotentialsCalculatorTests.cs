using System.Security.Authentication.ExtendedProtection;
using TpSolver.Shared;
using TpSolver.Solver.Modi;
using TpSolver.Tests.Utils;

namespace TpSolver.Tests.Parallel;

public class PotentialsCalculatorTests
{
    [Theory]
    // [InlineData(200)] speedup is too small
    [InlineData(300)] // ~1.4
    [InlineData(400)] // ~1.7
    public void CalcPotentials_ShouldBeCorrect(int size)
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
        Console.WriteLine($"[Perf potentials] Size={size}, S={speedup}");
    }
}
