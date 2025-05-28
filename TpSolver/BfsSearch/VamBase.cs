using Profiling;
using TpSolver.Shared;

namespace TpSolver.BfsSearch;

public abstract class VamBase
{
    protected readonly int m;
    protected readonly int n;
    protected readonly AllocationMatrix allocation;
    protected readonly bool[] rowDone; // or supply
    protected readonly bool[] colDone; // or demand
    protected readonly TransportProblem tp;
    protected readonly int[] supply; // copies that will be modified
    protected readonly int[] demand;

    public Profiler? Profiler { get; set; }

    public static class Stages
    {
        public const string Total = nameof(Total);
        public const string ArgmaxPenalty = nameof(ArgmaxPenalty);
        public const string Argmin = nameof(Argmin);
    }

    protected VamBase(TransportProblem tp)
    {
        this.tp = tp;
        supply = (int[])tp.Supply.Clone();
        demand = (int[])tp.Demand.Clone();

        m = supply.Length;
        n = demand.Length;
        allocation = new(new int[m, n]);

        rowDone = new bool[m];
        colDone = new bool[n];
    }

    public AllocationMatrix Search()
    {
        int idx;
        bool isRow;
        Point min;

        using var _ = Profiler?.Measure(Stages.Total) ?? Profiler.NoOp();
        int doneCount = 0;
        while (doneCount != rowDone.Length + colDone.Length)
        {
            using (Profiler?.Measure(Stages.ArgmaxPenalty) ?? Profiler.NoOp())
                idx = ArgmaxPenalty(out isRow);
            using (Profiler?.Measure(Stages.Argmin))
                min = isRow ? ArgminRowCost(idx) : ArgminColCost(idx);

            // Allocate maximally allowed
            int maxAlloc = Math.Min(supply[min.IRow], demand[min.ICol]);
            allocation[min] = new(maxAlloc);
            supply[min.IRow] -= maxAlloc;
            demand[min.ICol] -= maxAlloc;

            // Update done lists
            if (supply[min.IRow] == 0)
            {
                rowDone[min.IRow] = true;
                doneCount++;
            }
            if (demand[min.ICol] == 0)
            {
                colDone[min.ICol] = true;
                doneCount++;
            }
        }
        return allocation;
    }

    protected abstract int ArgmaxPenalty(out bool isRow);
    protected abstract Point ArgminRowCost(int i);
    protected abstract Point ArgminColCost(int j);
}
