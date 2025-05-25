using TpSolver.Perturbation;
using TpSolver.Shared;
using TpSolver.Tests.Utils;

namespace TpSolver.Tests.Perturbation;

public class EpsilonPerturbationTests
{
    [Fact]
    public void Perturb_ShouldPerturb_WhenDegenerousAllocation()
    {
        int[,] allocations =
        {
            { 1, 0, 0, 0 },
            { 0, 3, 0, 0 },
            { 0, 0, 2, 5 },
        };
        AllocationMatrix am = new(allocations);
        double[,] cost =
        {
            { 1, .1, 9, 9 },
            { 1, 1, 9, 2 },
            { 9, 9, 1, 1 },
        };

        int[,] copy = new int[am.NRows, am.NCols];
        Array.Copy(allocations, copy, allocations.Length);
        AllocationMatrix expected = new(copy);
        expected[0, 1] = expected[0, 1].ToBasic();
        expected[1, 3] = expected[1, 3].ToBasic();

        EpsilonPerturbation ep = new(am, cost);

        int basicCnt = am.CountBasic();
        Assert.Equal(4, basicCnt);
        int missing = am.NRows + am.NCols - 1 - basicCnt;
        bool result = ep.TryPerturb(missing);
        Assert.True(result);

        basicCnt = am.CountBasic();
        Assert.Equal(6, basicCnt);
        Assert.Equal(expected.AsEnumerableNBDistinct(), am.AsEnumerableNBDistinct());
    }
}
