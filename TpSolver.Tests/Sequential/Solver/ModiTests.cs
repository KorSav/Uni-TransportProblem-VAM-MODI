using TpSolver.BfsSearch;
using TpSolver.Shared;
using TpSolver.Solver.Modi;
using TpSolver.Solver.Modi.PotentialsCalculator;
using TpSolver.Tests.Sequential.Utils;
using static TpSolver.Solver.Modi.ModiSolver;

namespace TpSolver.Tests.Sequential.Solver;

public class ModiTests
{
    [Fact]
    public void CalcPotentials_ShouldBeCorrect_Simple()
    {
        AllocationMatrix am = new(
            new int[,]
            {
                { 1, 1, 0, 0 },
                { 0, 1, 1, 0 },
                { 0, 0, 1, 1 },
                { 0, 0, 0, 1 },
            }
        );
        double[,] cost =
        {
            { 1, 1, 9, 9 },
            { 9, 2, 2, 9 },
            { 9, 9, 3, 3 },
            { 9, 9, 9, 4 },
        };
        double[] RExpected = [0, 1, 2, 3];
        double[] CExpected = [1, 1, 1, 1];

        double[] RPotential = new double[4];
        double[] CPotential = new double[4];

        PotCalcSeq pc = new(am, cost, RPotential, CPotential);
        pc.CalcPotentials();

        Assert.Equal(RExpected, RPotential);
        Assert.Equal(CExpected, CPotential);
    }

    [Fact]
    public void CalcPotentials_ShouldBeCorrect_FromPaper()
    {
        AllocationMatrix am = new(
            new int[,]
            {
                { 0, 2, 0, 10 },
                { 0, 7, 10, 0 },
                { 10, 1, 0, 0 },
            }
        );
        double[,] cost =
        {
            { 8, 13, 4, 7 },
            { 11, 14, 6, 10 },
            { 6, 12, 8, 9 },
        };
        double[] RExpected = [0, 1, -1];
        double[] CExpected = [7, 13, 5, 7];

        double[] RPotential = new double[3];
        double[] CPotential = new double[4];

        PotCalcSeq pc = new(am, cost, RPotential, CPotential);
        pc.CalcPotentials();

        Assert.Equal(RExpected, RPotential);
        Assert.Equal(CExpected, CPotential);
    }

    [Fact]
    public void Solve_ShouldFindOptimum_WhenNoCycleIsTaken()
    {
        double[,] cost =
        {
            { 1, 1, 9, 9 },
            { 9, 2, 2, 9 },
            { 9, 9, 3, 3 },
            { 9, 9, 9, 4 },
        };
        int[] supply = [10, 10, 10, 5];
        int[] demand = [5, 10, 10, 10];

        AllocationMatrix expected = new(
            new int[,]
            {
                { 5, 5, 0, 0 },
                { 0, 5, 5, 0 },
                { 0, 0, 5, 5 },
                { 0, 0, 0, 5 },
            }
        );

        TransportProblem tp = new(cost, supply, demand, true);
        AllocationMatrix? actual = new ModiSolverSeq(tp).Solve();
        Assert.NotNull(actual);
        Assert.Equal(expected.AsEnumerableNBDistinct(), actual.AsEnumerableNBDistinct());
    }

    [Fact]
    public void Solve_ShouldFindOptimum_WhenDegenerousProblem_NoCycle()
    {
        double[,] cost =
        {
            { 1, 9, 9, 9 },
            { 9, 2, 9, 9 },
            { 9, 9, 3, 9 },
            { 9, 9, 9, 4 },
        };
        int[] supply = [10, 10, 10, 10];
        int[] demand = [10, 10, 10, 10];

        AllocationMatrix expected = new(
            new int[,]
            {
                { 10, 0, 0, 0 },
                { 0, 10, 0, 0 },
                { 0, 0, 10, 0 },
                { 0, 0, 0, 10 },
            }
        );

        TransportProblem tp = new(cost, supply, demand, true);
        AllocationMatrix? actual = new ModiSolverSeq(tp).Solve();
        Assert.NotNull(actual);
        Assert.Equal(expected.AsEnumerable(), actual.AsEnumerable());
    }

    [Fact]
    public void Solve_ShouldFindOptimum_WhenCyclesAreTaken_ExampleFromPaper1()
    {
        double[,] cost =
        {
            { 8, 13, 4, 7 },
            { 11, 14, 6, 10 },
            { 6, 12, 8, 9 },
        };
        int[] supply = [12, 17, 11];
        int[] demand = [10, 10, 10, 10];

        VamSeq vam = new(new(cost, supply, demand, true));
        AllocationMatrix bfs = vam.Search(); // was considered in previous tests

        TransportProblem tp = new(cost, supply, demand);
        ModiSolverSeq ms = new(tp) { Profiler = new() };
        AllocationMatrix? optimal = ms.Solve();

        Assert.NotNull(optimal);
        Assert.Equal(1, ms.Profiler[Stages.Pivot].HitCount);
        Assert.True(optimal.CalcTotalCost(cost) < bfs.CalcTotalCost(cost));
        Assert.Equal(324, optimal.CalcTotalCost(cost));
    }

    [Fact]
    public void Solve_ShouldFindOptimum_WhenCyclesAreTaken_ExampleFromPaper2()
    {
        double[,] cost =
        {
            { 2, 4, 6 },
            { 3, 8, 7 },
            { 4, 3, 8 },
            { 4, 6, 3 },
            { 2, 6, 5 },
            { 0, 0, 0 },
        };
        int[] supply = [75, 345, 180, 90, 210, 700];
        int[] demand = [850, 300, 450];

        AllocationMatrix paperResult = new(
            new int[,]
            {
                { 75, 0, 0 },
                { 345, 0, 0 },
                { 0, 180, 0 },
                { 0, 0, 90 },
                { 210, 0, 0 },
                { 220, 120, 360 },
            }
        );

        TransportProblem tp = new(cost, supply, demand);
        ModiSolverSeq ms = new(tp) { Profiler = new() };
        AllocationMatrix? actual = ms.Solve();
        Assert.NotNull(actual);
        Assert.True(ms.Profiler[Stages.Pivot].HitCount >= 1);
        Assert.True(actual.CalcTotalCost(cost) <= paperResult.CalcTotalCost(cost));
        Assert.Equal(paperResult.AsEnumerable(), actual.AsEnumerable());
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    public void Solve_ShouldFindCorrectSolution_WhenBigProblem(int size)
    {
        TransportProblem tp = TransportProblem.GenerateRandom(size, size);

        ModiSolverSeq ms = new(tp) { Profiler = new() };
        AllocationMatrix? actual = ms.Solve();
        Assert.NotNull(actual);
        Assert.True(ms.Profiler[Stages.Pivot].HitCount >= 1);
        Assert.True(AllocationValidation.IsDemandPerColCorrect(actual, tp.Demand));
        Assert.True(AllocationValidation.IsSupplyPerRowCorrect(actual, tp.Supply));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    public void Solve_ShouldFindCorrectSolution_WhenBigHighlyDegenerousProblem(int size)
    {
        double[,] cost = new double[size, size];
        int[] supply = new int[size];
        int[] demand = new int[size];
        Random rnd = new();

        for (int i = 0; i < size; i++)
        {
            supply[i] = 1;
            demand[i] = 1;
            for (int j = 0; j < size; j++)
            {
                cost[i, j] = rnd.NextDouble();
            }
        }

        TransportProblem tp = new(cost, supply, demand);
        ModiSolverSeq ms = new(tp) { Profiler = new() };
        AllocationMatrix? actual = ms.Solve();
        Assert.NotNull(actual);
        Assert.True(ms.Profiler[Stages.Pivot].HitCount >= 1);
        Assert.True(AllocationValidation.IsDemandPerColCorrect(actual, demand));
        Assert.True(AllocationValidation.IsSupplyPerRowCorrect(actual, supply));
    }
}
