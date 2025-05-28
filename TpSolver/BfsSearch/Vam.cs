using System.Diagnostics;
using Profiling;
using TpSolver.Shared;

namespace TpSolver.BfsSearch;

public class Vam : VamBase
{
    protected readonly double[] rowPenalty;
    protected readonly double[] colPenalty;

    public Vam(TransportProblem tp)
        : base(tp)
    {
        rowPenalty = new double[m];
        colPenalty = new double[n];
    }

    protected override int ArgmaxPenalty(out bool isRow)
    {
        double maxPenalty = -1;
        int idx = -1;

        for (int i = 0; i < m; i++)
        {
            if (rowDone[i])
                continue;
            rowPenalty[i] = CalcRowPenalty(i);
            Debug.Assert(rowPenalty[i] >= 0);
            if (rowPenalty[i] > maxPenalty)
            {
                maxPenalty = rowPenalty[i];
                idx = i;
            }
        }

        for (int j = 0; j < n; j++)
        {
            if (colDone[j])
                continue;
            colPenalty[j] = CalcColPenalty(j);
            Debug.Assert(colPenalty[j] >= 0);
            if (colPenalty[j] > maxPenalty)
            {
                maxPenalty = colPenalty[j];
                idx = m + j;
            }
        }
        Debug.Assert(maxPenalty >= 0); // 0 is possible if one cost remained nondone

        isRow = idx < m;
        return isRow switch
        {
            true => idx,
            false => idx - m,
        };
    }

    protected override Point ArgminColCost(int j)
    {
        double minCost = double.PositiveInfinity;
        int res_i = -1;
        for (int i = 0; i < m; i++)
        {
            if (rowDone[i])
                continue;
            if (tp.Cost[i, j] < minCost)
            {
                minCost = tp.Cost[i, j];
                res_i = i;
            }
        }
        return new(res_i, j);
    }

    protected override Point ArgminRowCost(int i)
    {
        double minCost = double.PositiveInfinity;
        int res_j = -1;
        for (int j = 0; j < n; j++)
        {
            if (colDone[j])
                continue;
            if (tp.Cost[i, j] < minCost)
            {
                minCost = tp.Cost[i, j];
                res_j = j;
            }
        }
        return new(i, res_j);
    }

    protected double CalcColPenalty(int j)
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

    protected double CalcRowPenalty(int i)
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
}
