namespace TpSolver.Shared;

public class Matrix<T>
{
    protected T[,] data;
    protected int m => data.GetLength(0);
    public int NRows => data.GetLength(0);
    protected int n => data.GetLength(1);
    public int NCols => data.GetLength(1);

    public Matrix(Matrix<T> m) => data = m.data;

    public Matrix(T[,] matrix)
    {
        data = matrix;
        if (m > int.MaxValue / n)
            throw new ArgumentException(
                $"Matrix size [{m}, {n}] is too large. Amount of elments in matrix should not cause signed integer overflow"
            );
    }

    public T this[Point pnt]
    {
        get => data[pnt.IRow, pnt.ICol];
        set => data[pnt.IRow, pnt.ICol] = value;
    }

    public T this[int i, int j]
    {
        get => data[i, j];
        set => data[i, j] = value;
    }

    public static implicit operator Matrix<T>(T[,] matrix) => new(matrix);

    public void Fill(Func<Point, T> generator)
    {
        for (int i = 0; i < m; i++)
        for (int j = 0; j < n; j++)
            data[i, j] = generator(new(i, j));
    }

    // duplicate code to make inlining chance higher
    public void Fill(Func<int, int, T> generator)
    {
        for (int i = 0; i < m; i++)
        for (int j = 0; j < n; j++)
            data[i, j] = generator(i, j);
    }

    public int Count(Func<T, bool> predicate)
    {
        int count = 0;
        for (int i = 0; i < m; i++)
        for (int j = 0; j < n; j++)
            if (predicate(data[i, j]))
                count++;
        return count;
    }
}
