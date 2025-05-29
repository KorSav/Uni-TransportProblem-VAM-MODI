using System.Diagnostics;
using Profiling;
using TpSolver.CycleSearch;
using TpSolver.Shared;

namespace TpSolver.Perturbation;

class EpsilonPerturbation
{
    private readonly AllocationMatrix allocation;
    private readonly Matrix<double> cost;
    private readonly CycleSearcher cs;
    private readonly Matrix<bool> toCheck;
    private readonly int m;
    private readonly int n;

    public Profiler CycleProfiler { get; }
    public Profiler Profiler { get; }

    public EpsilonPerturbation(AllocationMatrix allocation, Matrix<double> cost)
    {
        this.allocation = allocation;
        m = allocation.NRows;
        n = allocation.NCols;
        Profiler = new();
        CycleProfiler = new();
        cs = new(this.allocation) { Profiler = CycleProfiler };
        this.cost = cost;
        toCheck = new bool[m, n];
    }

    public EpsilonPerturbation(AllocationMatrix allocation, double[,] cost)
        : this(allocation, new Matrix<double>(cost)) { }

    public bool TryPerturb(int pntCount)
    {
        Debug.Assert(pntCount > 0);
        if (pntCount <= 0)
            return true;

        toCheck.Fill((p) => !allocation[p].IsBasic);
        Point? pntMin;
        List<Point>? cycle;
        int perturbedCount = 0;
        int cyclesFound = 0;
        for (; perturbedCount < pntCount; perturbedCount++)
        {
            do
            {
                using (Profiler.Measure("seq, argmin"))
                    pntMin = ArgminCostInCheckList();
                if (pntMin is null) // all non basic cells were tried
                    return false;
                cycle = cs.SearchClosed(pntMin.Value);
                if (cycle is not null)
                { // dont check points that are known to have cycle
                    toCheck[pntMin.Value] = false;
                    cyclesFound++;
                }
            } while (cycle is not null);
            allocation[pntMin.Value] = allocation[pntMin.Value].ToBasic();
            toCheck[pntMin.Value] = false;
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
