using TpSolver.CycleSearch;
using TpSolver.Shared;
using TpSolver.Utils;

namespace TpSolver.Perturbation;

class EpsilonPerturbationParallel : EpsilonPerturbation
{
    public override CycleSearcher CycleSearcher { get; set; }
    private readonly int parDeg;

    public EpsilonPerturbationParallel(
        AllocationMatrix allocation,
        Matrix<double> cost,
        int parallelizationDegree
    )
        : base(allocation, cost)
    {
        CycleSearcher = new CycleSearcherParallel(allocation, parallelizationDegree);
        parDeg = parallelizationDegree;
    }

    protected override Point? ArgminCostInCheckList()
    {
        var tasks = new Task<Point?>[parDeg];
        if (!tasks.TryRunDistributedRange(0, m * n, ArgminCostToCheckInRange))
            return ArgminCostToCheckInRange(0, m * n) ?? new(-1, -1); // should never be null
        Task.WaitAll(tasks);
        Point? globalMin = null;
        foreach (var task in tasks)
            globalMin = Min(globalMin, task.Result);
        return globalMin;
    }

    private Point? Min(Point? a, Point? b)
    {
        if (a is null)
            return b;
        if (b is null)
            return a;
        return (cost[a.Value] < cost[b.Value] || cost[a.Value] == cost[b.Value] && a < b) ? a : b;
    }

    private Point? ArgminCostToCheckInRange(int fromInc, int toExc)
    {
        Point? min = null;
        double costMin = double.PositiveInfinity;
        for (int compound = fromInc; compound < toExc; compound++)
        {
            int i = compound / n;
            int j = compound % n;
            double cost = this.cost[i, j];
            if (toCheck[i, j] && cost < costMin)
            {
                costMin = cost;
                min = new(i, j);
            }
        }
        return min;
    }
}
