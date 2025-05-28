using System.Diagnostics;
using System.Runtime.CompilerServices;
using Profiling;
using TpSolver.Shared;

namespace TpSolver.BfsSearch;

public class VamParallel
{
    const int indexNotSet = -1;
    double[] rowPenalty;
    double[] colPenalty;
    int m;
    int n;
    AllocationMatrix allocation;
    bool[] rowDone; // or supply
    bool[] colDone; // or demand
    TransportProblem tp;
    int[] supply; // copies that will be modified
    int[] demand;
    readonly ParallelOptions parOpts;
    public Profiler Profiler { get; private init; } = new();

    /// <summary>
    /// Tries to execute at most <paramref name="parallelizationDegree"/> tasks in parallel.
    /// <para>
    /// Int.Max removes limit.
    /// </para>
    /// </summary>
    public VamParallel(TransportProblem tp, ParallelOptions parallelOptions)
    {
        parOpts = parallelOptions;
        this.tp = tp;
        supply = (int[])tp.Supply.Clone();
        demand = (int[])tp.Demand.Clone();

        m = supply.Length;
        n = demand.Length;
        allocation = new(new int[m, n]);

        rowPenalty = new double[m];
        rowDone = new bool[m];

        colPenalty = new double[n];
        colDone = new bool[n];
    }

    public AllocationMatrix Search()
    {
        using var _ = Profiler.Measure("Total");
        int doneCount = 0;
        while (doneCount != rowDone.Length + colDone.Length)
        {
            int idx = ArgmaxPenalty(out bool isRow);
            (int i_min, int j_min) = isRow ? ArgminRowCost(idx) : ArgminColCost(idx);

            // Allocate maximally allowed
            int quantity = Math.Min(supply[i_min], demand[j_min]);
            allocation[i_min, j_min] = new(quantity);
            supply[i_min] -= quantity;
            demand[j_min] -= quantity;

            // Update done lists
            if (supply[i_min] == 0)
            {
                rowDone[i_min] = true;
                doneCount++;
            }
            if (demand[j_min] == 0)
            {
                colDone[j_min] = true;
                doneCount++;
            }
        }
        return allocation;
    }

    private int ArgmaxPenalty(out bool isRow)
    {
        // despite double is used for penalties
        // it is possible for two exactly same penalties occur
        // especially, when many numbers are randomly generated
        // in this case algorithm assures to choose maximum:
        //   - if in the same vector: with lowest index
        //   - if in different vectors: row dominate over column
        int ir = indexNotSet;
        void findMaxInRow(int i)
        {
            // take batch index
            // find points from and points to
            // calculate two local minimums
            if (rowDone[i])
                return;
            rowPenalty[i] = CalcRowPenalty(i);
            AtomicUpdateMaxPenaltyFrom(ref ir, i, rowPenalty);
        }
        Parallel.For(0, m, parOpts, findMaxInRow);

        int ic = indexNotSet;
        void findMaxInCol(int j)
        {
            if (colDone[j])
                return;
            colPenalty[j] = CalcColPenalty(j);
            AtomicUpdateMaxPenaltyFrom(ref ic, j, colPenalty);
        }
        Parallel.For(0, n, parOpts, findMaxInCol);

        if (ir == indexNotSet)
        {
            isRow = false;
            return ic;
        }
        if (ic == indexNotSet)
        {
            isRow = true;
            return ir;
        }
        isRow = rowPenalty[ir] >= colPenalty[ic];

        return isRow ? ir : ic;
    }

    private void AtomicUpdateMaxPenaltyFrom(ref int curMax, int i, double[] penalty)
    {
        if (indexNotSet == Interlocked.CompareExchange(ref curMax, i, indexNotSet))
            return;
        int imaxBeforeCheck;
        do
        {
            imaxBeforeCheck = curMax;
            if (
                penalty[i] < penalty[imaxBeforeCheck]
                || penalty[i] == penalty[imaxBeforeCheck] && i > imaxBeforeCheck
            )
                return;
        } while (imaxBeforeCheck != Interlocked.CompareExchange(ref curMax, i, imaxBeforeCheck));
    }

    private (int, int) ArgminColCost(int j)
    {
        int imin = indexNotSet;
        void AtomicUpdateArgmin(int i)
        {
            if (rowDone[i])
                return;
            if (indexNotSet == Interlocked.CompareExchange(ref imin, i, indexNotSet))
                return;
            int iminBeforeCheck;
            do
            {
                iminBeforeCheck = imin;
                if (tp.Cost[i, j] >= tp.Cost[iminBeforeCheck, j])
                    return;
            } while (iminBeforeCheck != Interlocked.CompareExchange(ref imin, i, iminBeforeCheck));
        }
        Parallel.For(0, m, parOpts, AtomicUpdateArgmin);
        return (imin, j);
    }

    private (int, int) ArgminRowCost(int i)
    {
        int jmin = indexNotSet;
        void AtomicUpdateArgmin(int j)
        {
            if (colDone[j])
                return;
            if (indexNotSet == Interlocked.CompareExchange(ref jmin, j, indexNotSet))
                return;
            int jminBeforeCheck;
            do
            {
                jminBeforeCheck = jmin;
                if (tp.Cost[i, j] >= tp.Cost[i, jminBeforeCheck])
                    return;
            } while (jminBeforeCheck != Interlocked.CompareExchange(ref jmin, j, jminBeforeCheck));
        }
        Parallel.For(0, n, parOpts, AtomicUpdateArgmin);
        return (i, jmin);
    }

    #region Not changed methods

    private double CalcColPenalty(int j)
    {
        double min1 = double.PositiveInfinity;
        double min2 = double.PositiveInfinity;
        for (int i = 0; i < m; i++)
        {
            if (!rowDone[i])
            {
                UpdateMins(ref min1, ref min2, tp.Cost[i, j]);
            }
        }
        return CalcPenalty(min1, min2);
    }

    private double CalcRowPenalty(int i)
    {
        double min1 = double.PositiveInfinity;
        double min2 = double.PositiveInfinity;
        for (int j = 0; j < n; j++)
        {
            if (!colDone[j])
            {
                UpdateMins(ref min1, ref min2, tp.Cost[i, j]);
            }
        }
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

    #endregion
}
