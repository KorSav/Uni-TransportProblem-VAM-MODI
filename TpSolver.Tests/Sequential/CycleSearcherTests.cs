using TpSolver.CycleSearch;
using TpSolver.Shared;

namespace TpSolver.Tests.Sequential;

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
        Point pnt = new(2, 3);

        CycleSearcher cs = new(new(allocations));
        List<Point>? actual = cs.SearchClosed(pnt);
        List<Point> expected = new(4) { pnt };
        Assert.NotNull(actual);
        expected.AddRange(
            (actual[1] == new Point(2, 1)) switch
            {
                true => [new(2, 1), new(0, 1), new(0, 3)],
                false => [new(0, 3), new(0, 1), new(2, 1)],
            }
        );
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
        Point pnt = new(2, 1);

        CycleSearcher cs = new(new(allocations));
        List<Point>? actual = cs.SearchClosed(pnt);
        Assert.Null(actual);
    }

    [Fact]
    public void Search_ShouldFindCycle_WhenCalledMultipleTimes()
    {
        int[,] allocations =
        {
            { 0, 2, 0, 10 },
            { 0, 7, 10, 0 },
            { 10, 1, 0, 0 },
        };
        Point pnt = new(1, 0);

        CycleSearcher cs = new(new(allocations));
        List<Point>? actual = cs.SearchClosed(pnt);
        List<Point> expected = new(4) { pnt };
        Assert.NotNull(actual);
        expected.AddRange(
            (actual[1] == new Point(2, 0)) switch
            {
                true => [new(2, 0), new(2, 1), new(1, 1)],
                false => [new(1, 1), new(2, 1), new(2, 0)],
            }
        );
        Assert.Equal(expected, actual);

        expected.Clear();
        pnt = new(0, 2);
        actual = cs.SearchClosed(pnt);
        expected.Add(pnt);
        Assert.NotNull(actual);
        expected.AddRange(
            (actual[1] == new Point(0, 1)) switch
            {
                true => [new(0, 1), new(1, 1), new(1, 2)],
                false => [new(1, 2), new(1, 1), new(0, 1)],
            }
        );
        Assert.Equal(expected, actual);
    }
}
