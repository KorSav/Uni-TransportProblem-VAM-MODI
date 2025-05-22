namespace VamLib.Tests;

public class VamSolverTests
{
    [Fact]
    public void Solve_ShouldAllocateCorrectly_WhenSimpleInputProvided()
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

        var vam = new Vam(cost, supply, demand);
        int[,] actual = vam.Solve();
        Assert.Equal(expected, actual);
    }
}
