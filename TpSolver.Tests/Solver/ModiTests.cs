using TpSolver.Shared;
using TpSolver.Solver.Modi;

namespace TpSolver.Tests.Solver;

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

        PotentialsCalculator pc = new(cost, am, RPotential, CPotential);
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
            { 8, 18, 4, 7 },
            { 11, 14, 6, 10 },
            { 6, 12, 8, 9 },
        };
        double[] RExpected = [0, -4, -6];
        double[] CExpected = [12, 18, 10, 7];

        double[] RPotential = new double[3];
        double[] CPotential = new double[4];

        PotentialsCalculator pc = new(cost, am, RPotential, CPotential);
        pc.CalcPotentials();

        Assert.Equal(RExpected, RPotential);
        Assert.Equal(CExpected, CPotential);
    }
}
