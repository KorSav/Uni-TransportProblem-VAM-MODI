using System.Diagnostics;
using TpSolver.Shared;

namespace TpSolver.Perturbation;

public class EpsilonPerturbation
{
    private readonly AllocationMatrix allocation;
    private readonly double[,] cost;
    private readonly CycleSearcher cs;

    public EpsilonPerturbation(AllocationMatrix allocation, double[,] cost)
    {
        this.allocation = allocation;
        cs = new(this.allocation);
        this.cost = cost;
    }

    public bool TryPerturb(int pntToFindCount)
    {
        Debug.Assert(pntToFindCount > 0);
        if (pntToFindCount <= 0)
            return true;
        Point? pntMin;
        List<Point>? cycle;
        int foundCount = 0;
        for (; foundCount < pntToFindCount; foundCount++)
        {
            double lowerBound = double.NegativeInfinity;
            do
            {
                pntMin = ArgminCostNonBasicAbove(lowerBound);
                if (pntMin is null) // all non basic cells were tried
                    return false;
                lowerBound = cost[pntMin.Value.i, pntMin.Value.j];

                cycle = cs.SearchClosed(pntMin.Value);
            } while (cycle is not null);
            allocation[pntMin.Value] = allocation[pntMin.Value].AsBasic();
        }
        return true;
    }

    private Point? ArgminCostNonBasicAbove(double lowerBound)
    {
        Point? pnt = null;
        double costMin = double.PositiveInfinity;
        for (int i = 0; i < allocation.NRows; i++)
        for (int j = 0; j < allocation.NCols; j++)
        {
            double cost = this.cost[i, j];
            if (lowerBound < cost && cost < costMin && !allocation[i, j].IsBasic)
            {
                costMin = cost;
                pnt = new(i, j);
            }
        }
        return pnt;
    }
}
