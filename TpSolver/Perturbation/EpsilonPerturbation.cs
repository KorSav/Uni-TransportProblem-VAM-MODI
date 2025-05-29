using System.Diagnostics;
using Profiling;
using TpSolver.CycleSearch;
using TpSolver.Shared;

namespace TpSolver.Perturbation;

public class EpsilonPerturbation
{
    protected readonly AllocationMatrix allocation;
    protected readonly Matrix<double> cost;
    protected readonly Matrix<bool> toCheck;
    protected readonly int m;
    protected readonly int n;

    public virtual CycleSearcher CycleSearcher { get; set; }
    public Profiler? Profiler { get; set; }

    public static class Stages
    {
        public const string Total = nameof(Total);
        public const string Argmin = nameof(Argmin);
        public const string CycleSearch = nameof(CycleSearch);
    }

    public EpsilonPerturbation(AllocationMatrix allocation, Matrix<double> cost)
    {
        this.allocation = allocation;
        m = allocation.NRows;
        n = allocation.NCols;
        CycleSearcher = new(this.allocation);
        this.cost = cost;
        toCheck = new bool[m, n];
    }

    public bool TryPerturb(int pntCount)
    {
        using var _ = Profiler?.Measure(Stages.Total) ?? Profiler.NoOp();
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
                using (Profiler?.Measure(Stages.Argmin) ?? Profiler.NoOp())
                    pntMin = ArgminCostInCheckList();
                if (pntMin is null) // all non basic cells were tried
                    return false;

                using (Profiler?.Measure(Stages.CycleSearch) ?? Profiler.NoOp())
                    cycle = CycleSearcher.SearchClosed(pntMin.Value);
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

    protected virtual Point? ArgminCostInCheckList()
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
