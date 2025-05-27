using System.Diagnostics;
using Profiling;
using TpSolver.Shared;

namespace TpSolver.Perturbation;

class EpsilonPerturbationParallel
{
    private readonly AllocationMatrix allocation;
    private readonly Matrix<double> cost;
    private readonly CycleSearcherParallel cs;
    private readonly Matrix<bool> toCheck;
    private readonly int m;
    private readonly int n;
    private readonly ParallelOptions parOpts;

    public Profiler CycleProfiler { get; }
    public Profiler Profiler { get; }

    public EpsilonPerturbationParallel(
        AllocationMatrix allocation,
        Matrix<double> cost,
        ParallelOptions parallelOptions
    )
    {
        this.allocation = allocation;
        parOpts = parallelOptions;
        m = allocation.NRows;
        n = allocation.NCols;
        cs = new(this.allocation, parOpts);
        this.cost = cost;
        toCheck = new bool[m, n];
        CycleProfiler = cs.Profiler;
        Profiler = new();
    }

    public EpsilonPerturbationParallel(
        AllocationMatrix allocation,
        double[,] cost,
        ParallelOptions parallelOptions
    )
        : this(allocation, new Matrix<double>(cost), parallelOptions) { }

    public bool TryPerturb(int pntCount)
    {
        Debug.Assert(pntCount > 0);
        if (pntCount <= 0)
            return false;

        toCheck.Fill((p) => !allocation[p].IsBasic);
        Point? pntMin;
        List<Point>? cycle = null;
        int perturbedCount = 0;
        int cyclesFound = 0;
        for (; perturbedCount < pntCount; perturbedCount++)
        {
            do
            {
                using (Profiler.Measure("par, argmin"))
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
