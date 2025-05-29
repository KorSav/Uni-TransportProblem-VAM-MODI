using System.Diagnostics;
using Profiling;
using TpSolver.Shared;

namespace TpSolver.BfsSearch;

public abstract class Vam
{
    protected readonly int m;
    protected readonly int n;
    protected readonly AllocationMatrix allocation;
    protected readonly bool[] rowDone; // or supply
    protected readonly bool[] colDone; // or demand
    protected readonly TransportProblem tp;
    protected readonly int[] supply; // copies that will be modified
    protected readonly int[] demand;
    protected readonly double[] rowPenalty;
    protected readonly double[] colPenalty;

    public Profiler? Profiler { get; set; }

    public static class Stages
    {
        public const string Total = nameof(Total);
        public const string ArgmaxPenalty = nameof(ArgmaxPenalty);
        public const string Argmin = nameof(Argmin);
    }

    protected Vam(TransportProblem tp)
    {
        this.tp = tp;
        supply = (int[])tp.Supply.Clone();
        demand = (int[])tp.Demand.Clone();

        m = supply.Length;
        n = demand.Length;
        allocation = new(new int[m, n]);

        rowDone = new bool[m];
        colDone = new bool[n];

        rowPenalty = new double[m];
        colPenalty = new double[n];
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

    protected double CalcColPenalty(int j)
    {
        double min1 = double.PositiveInfinity;
        double min2 = double.PositiveInfinity;
        for (int i = 0; i < m; i++)
            if (!rowDone[i])
                UpdateMins(ref min1, ref min2, tp.Cost[i, j]);
        return CalcPenalty(min1, min2);
    }

    protected double CalcRowPenalty(int i)
    {
        double min1 = double.PositiveInfinity;
        double min2 = double.PositiveInfinity;
        for (int j = 0; j < n; j++)
            if (!colDone[j])
                UpdateMins(ref min1, ref min2, tp.Cost[i, j]);
        return CalcPenalty(min1, min2);
    }

    private double CalcPenalty(double min1, double min2)
    {
        Debug.Assert(min1 <= min2);
        Debug.Assert(!(min1 == double.PositiveInfinity && min2 == double.PositiveInfinity)); // should not happen, because row/col is not done yet
        // => the one should have undone cells in it

        if (min2 == double.PositiveInfinity)
            return 0; // only one value remained in row/col, no penalty
        return min2 - min1;
    }

    private static void UpdateMins(ref double min1, ref double min2, double val)
    {
        if (val < min2)
            min2 = val;
        if (min2 < min1)
            (min1, min2) = (min2, min1);
    }
}
