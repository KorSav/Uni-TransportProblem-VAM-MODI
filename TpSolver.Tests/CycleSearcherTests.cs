using TpSolver.Shared;

namespace TpSolver.Tests;

public class CycleSearcherTests
{
    [Fact]
    public void Search_ShouldFindCycle_WhenItExists()
    {
        int[,] allocations =
        {
            { 0, 2, 0, 10 },
            { 0, 7, 10, 0 },
            { 10, 1, 0, 0 },
        };
        int i = 2;
        int j = 3;
        CycleSearcher cs = new(allocations);
        List<Point>? actual = cs.SearchClosed(i, j);
        List<Point> expected = [new(i, j), new(2, 1), new(0, 1), new(0, 3)];
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Search_ShouldReturnNull_WhenNoCycle()
    {
        int[,] allocations =
        {
            { 1, 0, 0, 0 },
            { 0, 1, 0, 0 },
            { 1, 0, 0, 1 },
        };
        int i = 2;
        int j = 1;
        CycleSearcher cs = new(allocations);
        List<Point>? actual = cs.SearchClosed(i, j);
        Assert.Null(actual);
    }
}
