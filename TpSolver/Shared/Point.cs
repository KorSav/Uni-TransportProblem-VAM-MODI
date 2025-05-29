namespace TpSolver.Shared;

public readonly record struct Point(int IRow, int ICol)
{
    public static bool operator <(Point p1, Point p2) => p1.IRow <= p2.IRow && p1.ICol < p2.ICol;

    public static bool operator >(Point p1, Point p2) => p2 < p1;
}
