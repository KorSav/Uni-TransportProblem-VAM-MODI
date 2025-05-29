using TpSolver.Shared;

namespace TpSolver.Tests.Sequential.Utils;

public static class AllocationValidation
{
    public static bool IsSupplyPerRowCorrect(AllocationMatrix allocation, int[] supply)
    {
        for (int i = 0; i < supply.Length; i++)
        {
            int rowSum = 0;
            for (int j = 0; j < allocation.NCols; j++)
                rowSum += allocation[i, j];
            if (rowSum != supply[i])
                return false;
        }
        return true;
    }

    public static bool IsDemandPerColCorrect(AllocationMatrix allocation, int[] demand)
    {
        for (int j = 0; j < demand.Length; j++)
        {
            int colSum = 0;
            for (int i = 0; i < allocation.NRows; i++)
                colSum += allocation[i, j];
            if (colSum != demand[j])
                return false;
        }
        return true;
    }
}
