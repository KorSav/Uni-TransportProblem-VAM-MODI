using TpSolver.Shared;

namespace TpSolver.Tests;

static class EnumerablesExtension
{
    public static IEnumerable<int> AsEnumerable(this AllocationMatrix matrix)
    {
        for (int i = 0; i < matrix.NRows; i++)
        for (int j = 0; j < matrix.NCols; j++)
            yield return matrix[i, j];
    }

    public static IEnumerable<int> AsEnumerable(this int[,] matrix)
    {
        for (int i = 0; i < matrix.GetLength(0); i++)
        for (int j = 0; j < matrix.GetLength(1); j++)
            yield return matrix[i, j];
    }
}
