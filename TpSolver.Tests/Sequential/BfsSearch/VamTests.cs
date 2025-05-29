using TpSolver.BfsSearch;
using TpSolver.Shared;
using TpSolver.Tests.Sequential.Utils;

namespace TpSolver.Tests.Sequential.BfsSearch;

public class VamTests
{
    [Fact]
    public void Search_ShouldAllocateCorrectly_WhenSimpleInputProvided()
    {
        double[,] cost =
        {
            { 8, 13, 4, 7 },
            { 11, 14, 6, 10 },
            { 6, 12, 8, 9 },
        };

        int[] supply = [12, 17, 11];
        int[] demand = [10, 10, 10, 10];

        int[,] expected =
        {
            { 0, 2, 0, 10 },
            { 0, 7, 10, 0 },
            { 10, 1, 0, 0 },
        };

        var vam = new VamSeq(new(cost, supply, demand, shouldBalance: false));
        AllocationMatrix actual = vam.Search();
        Assert.Equal(expected.AsEnumerable(), actual.AsEnumerable());
    }

    [Fact]
    public void Search_ShouldAllocateCorrectly_WhenDegeneracyOccurs()
    {
        double[,] cost =
        {
            { 3, 1, 10 },
            { 2, 1, 10 },
            { 10, 10, 2 },
        };

        int[] supply = [10, 10, 10];
        int[] demand = [10, 10, 10];

        int[,] expected =
        {
            { 0, 10, 0 },
            { 10, 0, 0 },
            { 0, 0, 10 },
        };

        var vam = new VamSeq(new(cost, supply, demand));
        AllocationMatrix actual = vam.Search();
        Assert.Equal(expected.AsEnumerable(), actual.AsEnumerable());
    }
}
