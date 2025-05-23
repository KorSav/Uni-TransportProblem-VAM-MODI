namespace TpSolver.Shared;

public class AllocationMatrix
{
    readonly Allocation[,] allocations;
    public int NRows { get; }
    public int NCols { get; }

    public AllocationMatrix(int[,] allocations)
    {
        NRows = allocations.GetLength(0);
        NCols = allocations.GetLength(1);
        this.allocations = new Allocation[NRows, NCols];
        for (int i = 0; i < NRows; i++)
        for (int j = 0; j < NCols; j++)
        {
            this.allocations[i, j] = new(allocations[i, j]);
        }
    }

    public Allocation this[int i, int j]
    {
        get => allocations[i, j];
        set => allocations[i, j] = value;
    }

    internal Allocation this[Point pnt]
    {
        get => allocations[pnt.i, pnt.j];
        set => allocations[pnt.i, pnt.j] = value;
    }

    public long CountBasic()
    {
        long counter = 0;
        for (int i = 0; i < NRows; i++)
        for (int j = 0; j < NCols; j++)
            if (allocations[i, j].IsBasic)
                counter++;

        return counter;
    }
}
