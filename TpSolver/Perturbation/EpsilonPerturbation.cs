using System.Diagnostics;
using TpSolver.Shared;

namespace TpSolver.Perturbation;

public class EpsilonPerturbation
{
    private readonly AllocationMatrix allocation;
    private readonly double[,] cost;
    private readonly CycleSearcher cs;
    private readonly bool[,] toCheck;
    private readonly int m;
    private readonly int n;

    public EpsilonPerturbation(AllocationMatrix allocation, double[,] cost)
    {
        this.allocation = allocation;
        m = allocation.NRows;
        n = allocation.NCols;
        cs = new(this.allocation);
        this.cost = cost;
        toCheck = new bool[m, n];
    }

    public bool TryPerturb(int pntCount)
    {
        Debug.Assert(pntCount > 0);
        if (pntCount <= 0)
            return true;

        for (int i = 0; i < m; i++)
        for (int j = 0; j < n; j++)
            toCheck[i, j] = !allocation[i, j].IsBasic;
        Point? pntMin;
        List<Point>? cycle;
        int perturbedCount = 0;
        int checkedCount = 0;
        for (; perturbedCount < pntCount; perturbedCount++)
        {
            pntMin = null;
            checkedCount = toCheck.Cast<bool>().Count(p => p);
            do
            {
                pntMin = ArgminCostInCheckList();
                if (pntMin is null) // all non basic cells were tried
                    return false;
                cycle = cs.SearchClosed(pntMin.Value);
                if (cycle is not null) // dont check points that are known to have cycle
                    toCheck[pntMin.Value.i, pntMin.Value.j] = false;
            } while (cycle is not null);
            allocation[pntMin.Value] = allocation[pntMin.Value].ToBasic();
            toCheck[pntMin.Value.i, pntMin.Value.j] = false;
        }
        return true;
    }

    private Point? ArgminCostInCheckList()
    {
        Point? pnt = null;
        double costMin = double.PositiveInfinity;

        for (int i = 0; i < allocation.NRows; i++)
        for (int j = 0; j < allocation.NCols; j++)
        {
            double cost = this.cost[i, j];
            if (toCheck[i, j] && cost < costMin)
            {
                costMin = cost;
                pnt = new(i, j);
            }
        }
        return pnt;
    }
}
