using System.Diagnostics;
using TpSolver.Shared;

namespace TpSolver.BfsSearch;

public class Vam
{
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

    public Vam(TransportProblem tp)
    {
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

    private (int, int) ArgminColCost(int j)
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
        return (res_i, j);
    }

    private (int, int) ArgminRowCost(int i)
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
        return (i, res_j);
    }

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
}
