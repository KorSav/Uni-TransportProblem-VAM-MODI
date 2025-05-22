using System.Diagnostics;

namespace VamLib;

public class Vam
{
    double[] rowPenalty;
    double[] colPenalty;
    int m;
    int n;
    int[,] allocation;
    bool[] rowDone;
    bool[] colDone;
    double[,] cost;
    int[] supply; // represented as rows
    int[] demand; // represented as cols

    public Vam(double[,] cost, int[] supply, int[] demand)
    {
        this.cost = cost;
        this.supply = supply;
        this.demand = demand;

        m = supply.Length;
        n = demand.Length;
        allocation = new int[m, n];

        rowPenalty = new double[m];
        rowDone = new bool[m];

        colPenalty = new double[n];
        colDone = new bool[n];
    }

    public int[,] Solve()
    {
        while (!AllDone())
        {
            // Compute penalties
            rowPenalty = new double[m];
            colPenalty = new double[n];
            double maxPenalty = -1;
            long idx = -1; // for m + n not to cause overflow

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

            Debug.Assert(maxPenalty != 0);

            // Find minimum cost cell in selected row/col
            (int i_min, int j_min) = (idx < m) switch
            {
                true => ArgminRowCost((int)idx),
                false => ArgminColCost((int)(idx - m)),
            };

            // Allocate
            MaxPossibleAllocate(i_min, j_min);
            rowDone[i_min] = supply[i_min] == 0;
            colDone[j_min] = demand[j_min] == 0;
        }
        return allocation;
    }

    private bool AllDone() =>
        rowDone.All(rowMark => rowMark is true) && colDone.All(colMark => colMark is true);

    private void MaxPossibleAllocate(int i_min, int j_min)
    {
        int quantity = Math.Min(supply[i_min], demand[j_min]);
        allocation[i_min, j_min] = quantity;
        supply[i_min] -= quantity;
        demand[j_min] -= quantity;
    }

    private (int, int) ArgminColCost(int j)
    {
        double minCost = double.PositiveInfinity;
        int res_i = -1;
        int res_j = -1;
        for (int i = 0; i < m; i++)
        {
            if (!rowDone[i] && cost[i, j] < minCost)
            {
                minCost = cost[i, j];
                res_i = i;
                res_j = j;
            }
        }
        return (res_i, res_j);
    }

    private (int, int) ArgminRowCost(int i)
    {
        double minCost = double.PositiveInfinity;
        int res_i = -1;
        int res_j = -1;
        for (int j = 0; j < n; j++)
        {
            if (colDone[j])
                continue;
            if (cost[i, j] < minCost)
            {
                minCost = cost[i, j];
                res_i = i;
                res_j = j;
            }
        }
        return (res_i, res_j);
    }

    private double CalcColPenalty(int j)
    {
        double min1 = double.PositiveInfinity;
        double min2 = double.PositiveInfinity;
        for (int i = 0; i < m; i++)
        {
            if (!rowDone[i])
            {
                UpdateMins(ref min1, ref min2, cost[i, j]);
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
                UpdateMins(ref min1, ref min2, cost[i, j]);
            }
        }
        return CalcPenalty(min1, min2);
    }

    private static double CalcPenalty(double min1, double min2)
    {
        Debug.Assert(min1 < min2);
        if (min1 == double.PositiveInfinity && min2 == double.PositiveInfinity)
            return double.NaN; // will throw debug assertion error (check if this even possible)
        if (min2 == double.PositiveInfinity)
            return min1;
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
