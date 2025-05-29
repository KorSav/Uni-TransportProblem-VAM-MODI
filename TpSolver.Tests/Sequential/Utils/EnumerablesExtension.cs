using TpSolver.Shared;

namespace TpSolver.Tests.Sequential.Utils;

static class EnumerablesExtension
{
    /// <summary>
    /// Enumerable with non basic cell distinction
    /// </summary>
    public static IEnumerable<int> AsEnumerableNBDistinct(this AllocationMatrix matrix)
    {
        for (int i = 0; i < matrix.NRows; i++)
        for (int j = 0; j < matrix.NCols; j++)
            yield return matrix[i, j].IsBasic switch
            {
                true => matrix[i, j],
                false => -1,
            };
    }

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
