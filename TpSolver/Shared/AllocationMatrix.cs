namespace TpSolver.Shared;

public class AllocationMatrix
{
    // Simply use int? instead of custom struct results in 2x memory occupation
    // And arithmetic will be forced to use '?? 0'
    readonly AllocationValue[,] allocations;
    private int m;
    public int NRows => m;
    private int n;
    public int NCols => n;

    public AllocationMatrix(int[,] allocations)
    {
        m = allocations.GetLength(0);
        n = allocations.GetLength(1);
        this.allocations = new AllocationValue[m, n];
        for (int i = 0; i < m; i++)
        for (int j = 0; j < n; j++)
        {
            this.allocations[i, j] = new(allocations[i, j]);
        }
    }

    public AllocationValue this[int i, int j]
    {
        get => allocations[i, j];
        set => allocations[i, j] = value;
    }

    internal AllocationValue this[Point pnt]
    {
        get => allocations[pnt.i, pnt.j];
        set => allocations[pnt.i, pnt.j] = value;
    }

    public int CountBasic()
    {
        int counter = 0;
        for (int i = 0; i < m; i++)
        for (int j = 0; j < n; j++)
            if (allocations[i, j].IsBasic)
                counter++;

        return counter;
    }

    public double CalcTotalCost(double[,] cost)
    {
        double total = 0;
        for (int i = 0; i < m; i++)
        for (int j = 0; j < n; j++)
            total += cost[i, j] * allocations[i, j];
        return total;
    }
}
