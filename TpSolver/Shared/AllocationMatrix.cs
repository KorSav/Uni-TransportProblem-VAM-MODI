namespace TpSolver.Shared;

/// <summary>
/// Represents allocations as custom matrix with custom type.
/// </summary>
/// <remarks>
/// Not using <see cref="int?"/>, because it occupies 2x memory and arithmetic will be forced to use '?? 0'
/// </remarks>
public class AllocationMatrix : Matrix<AllocationValue>
{
    public AllocationMatrix(int[,] allocations)
        : base(new AllocationValue[allocations.GetLength(0), allocations.GetLength(1)])
    {
        Fill((i, j) => new(allocations[i, j]));
    }

    public AllocationMatrix(AllocationMatrix am)
        : base(new AllocationValue[am.m, am.n])
    {
        Fill(p => am[p]);
    }

    public int CountBasic()
    {
        int counter = 0;
        for (int i = 0; i < m; i++)
        for (int j = 0; j < n; j++)
            if (data[i, j].IsBasic)
                counter++;

        return counter;
    }

    public double CalcTotalCost(Matrix<double> cost)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(NRows, cost.NRows, nameof(NRows));
        ArgumentOutOfRangeException.ThrowIfNotEqual(NCols, cost.NCols, nameof(NCols));
        double total = 0;
        for (int i = 0; i < m; i++)
        for (int j = 0; j < n; j++)
            total += cost[i, j] * data[i, j];
        return total;
    }

    internal void Pivot(List<Point> cycle)
    {
        // if min == 0, proceed because it changes list of basic cells
        int min = MinOfCycleAtOddIndexes(cycle);
        int nonBasicMetCount = 0;
        this[cycle[0]] += new AllocationValue(min);
        this[cycle[0]] = this[cycle[0]].ToBasic();
        for (int i = 1; i < cycle.Count; i++)
        {
            if (i % 2 == 0)
                this[cycle[i]] += new AllocationValue(min);
            else
                this[cycle[i]] -= new AllocationValue(min);

            // avoid degeneracy
            var current = this[cycle[i]];
            if (!current.IsBasic)
                if (nonBasicMetCount++ > 0)
                    this[cycle[i]] = current.ToBasic();
        }
    }

    private int MinOfCycleAtOddIndexes(List<Point> cycle)
    {
        int min = int.MaxValue;
        for (int i = 1; i < cycle.Count; i++)
            if (i % 2 == 1)
            {
                int val = this[cycle[i]];
                if (val < min)
                    min = val;
            }
        return min;
    }
}
