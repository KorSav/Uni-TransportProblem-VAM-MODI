using TpSolver.Shared;
using TpSolver.Tests.Utils;

namespace TpSolver.Tests;

public class AllocationMatrixTests
{
    [Fact]
    public void Allocation_ShouldEncapsulateEmptyBasicCells()
    {
        int[,] allocations =
        {
            { 1, 0, 3 },
            { 0, 2, 0 },
            { 4, 0, 0 },
        };
        int i = 0,
            j = 1;
        AllocationMatrix am = new(allocations);
        Assert.Equal(allocations.AsEnumerable(), am.AsEnumerable());

        Assert.Equal(0, am[i, j]);
        Assert.False(am[i, j].IsBasic);
        am[i, j] = am[i, j].ToBasic();

        Assert.Equal(0, am[i, j]);
        Assert.True(am[i, j].IsBasic);
        am[i, j] = new(8);
    }

    [Fact]
    public void CountBasic_ShouldCountBasicCellsCorrectly()
    {
        AllocationMatrix am = new(
            new int[,]
            {
                { 1, 0, 0, 0 },
                { 2, 0, 3, 0 },
                { 0, 4, 0, 5 },
            }
        );
        int bc = am.CountBasic();
        Assert.Equal(5, bc);

        am[0, 3] = am[0, 3].ToBasic();
        bc = am.CountBasic();
        Assert.Equal(6, bc);
    }
}
