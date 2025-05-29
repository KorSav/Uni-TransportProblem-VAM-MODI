using Profiling;
using TpSolver.BfsSearch;
using TpSolver.CycleSearch;
using TpSolver.Perturbation;
using TpSolver.Shared;
using TpSolver.Solver.Modi.PotentialsCalculator;

namespace TpSolver.Solver.Modi;

public abstract class ModiSolverBase(TransportProblem tp)
{
    protected readonly TransportProblem tp = tp;
    protected readonly double[] RPotential = new double[tp.Supply.Length];
    protected readonly int m = tp.Supply.Length;
    protected readonly double[] CPotential = new double[tp.Demand.Length];
    protected readonly int n = tp.Demand.Length;
    protected AllocationMatrix sln = null!;
    public Profiler? Profiler { get; set; }

    public static class Stages
    {
        public const string Total = nameof(Total);
        public const string BfsSearch = nameof(BfsSearch);
        public const string Perturbation = nameof(Perturbation);
        public const string PotentialsCalculation = nameof(PotentialsCalculation);
        public const string MinDiffNonBasicCost_Potentials = nameof(MinDiffNonBasicCost_Potentials);
        public const string CycleSearch = nameof(CycleSearch);
        public const string Pivot = nameof(Pivot);
    }

    public AllocationMatrix? Solve(TransportProblem tp)
    {
        using var _ = Profiler?.Measure(Stages.Total) ?? Profiler.NoOp();

        using (Profiler?.Measure(Stages.BfsSearch) ?? Profiler.NoOp())
            sln = CreateBfsSearcher().Search();

        int perturbCount = m + n - 1 - sln.Count(static a => a.IsBasic);
        if (perturbCount > 0) // sln is degenerate
        {
            bool isNonDegenerate = CreateEpsilonPerturbation(sln, tp.Cost).TryPerturb(perturbCount);
            if (!isNonDegenerate)
                return null; // should never happen
        }

        PotCalcBase pc = CreatePotentialsCalculator(sln, tp.Cost, RPotential, CPotential);
        CycleSearcher cs = CreateCycleSearcher(sln);
        PntDiffPotential min;
        List<Point>? cycle;
        do
        {
            using (Profiler?.Measure(Stages.PotentialsCalculation) ?? Profiler.NoOp())
                pc.CalcPotentials();

            using (Profiler?.Measure(Stages.MinDiffNonBasicCost_Potentials) ?? Profiler.NoOp())
                min = MinDiffNBCostPotential();
            if (min.diff >= 0)
                break; // sln is optimal

            using (Profiler?.Measure(Stages.CycleSearch) ?? Profiler.NoOp())
                cycle = cs.SearchClosed(min.pnt);
            if (cycle is null) // math states that cycle will be always found
                throw new InvalidOperationException("Failed to find cycle during sln optimization");

            using (Profiler?.Measure(Stages.Pivot) ?? Profiler.NoOp())
                sln.Pivot(cycle);
        } while (true);

        return sln;
    }

    protected abstract VamBase CreateBfsSearcher();
    protected abstract EpsilonPerturbation CreateEpsilonPerturbation(
        AllocationMatrix am,
        Matrix<double> cost
    );
    private protected abstract PotCalcBase CreatePotentialsCalculator(
        AllocationMatrix am,
        Matrix<double> cost,
        double[] RPotential,
        double[] CPotential
    );
    protected abstract CycleSearcher CreateCycleSearcher(AllocationMatrix am);
    protected abstract PntDiffPotential MinDiffNBCostPotential();

    protected readonly struct PntDiffPotential(Point pnt, double diff)
    {
        public readonly Point pnt = pnt;
        public readonly double diff = diff;

        public static PntDiffPotential MaxValue =>
            new(new Point(int.MaxValue, int.MaxValue), double.PositiveInfinity);

        public static bool operator <(PntDiffPotential a, PntDiffPotential b) =>
            a.diff < b.diff || (a.diff == b.diff && a.pnt < b.pnt);

        public static bool operator >(PntDiffPotential a, PntDiffPotential b) => b < a;
    }
}
