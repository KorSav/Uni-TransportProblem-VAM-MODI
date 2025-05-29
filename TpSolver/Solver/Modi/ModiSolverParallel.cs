using TpSolver.BfsSearch;
using TpSolver.CycleSearch;
using TpSolver.Perturbation;
using TpSolver.Shared;
using TpSolver.Solver.Modi.PotentialsCalculator;
using TpSolver.Utils;

namespace TpSolver.Solver.Modi;

public class ModiSolverParallel(TransportProblem tp, int maxDegreeOfParallelism)
    : ModiSolverBase(tp)
{
    protected int parDeg = maxDegreeOfParallelism;

    protected override PntDiffPotential MinDiffNBCostPotential()
    {
        PntDiffPotential globalMin;
        var tasks = new Task<PntDiffPotential?>[parDeg];
        if (!tasks.TryRunDistributedRange(0, m * n, ArgminNonBasicCostDiffPotentialInRange))
            return ArgminNonBasicCostDiffPotentialInRange(0, m * n) ?? PntDiffPotential.MaxValue; // should never be null
        Task.WaitAll(tasks);
        globalMin = PntDiffPotential.MaxValue;
        foreach (var task in tasks)
        {
            PntDiffPotential? localMin = task.Result;
            if (localMin is null)
                continue;
            if (localMin < globalMin)
                globalMin = localMin.Value;
        }
        // The problem assures that globalMin will be always found
        return globalMin;
    }

    private PntDiffPotential? ArgminNonBasicCostDiffPotentialInRange(int iFrom, int iTo)
    {
        double min = double.PositiveInfinity;
        Point? pntMin = null;
        for (int compound = iFrom; compound < iTo; compound++)
        {
            int i = compound / n;
            int j = compound % n;
            double costDiffPotential = tp.Cost[i, j] - RPotential[i] - CPotential[j];
            if (!sln[i, j].IsBasic && costDiffPotential < min)
            {
                min = costDiffPotential;
                pntMin = new(i, j);
            }
        }
        if (pntMin is null)
            return null;
        return new(pntMin.Value, min);
    }

    protected override VamBase CreateBfsSearcher() => new VamParallel(tp, parDeg);

    protected override CycleSearcher CreateCycleSearcher(AllocationMatrix am) =>
        new CycleSearcherParallel(am, parDeg);

    protected override EpsilonPerturbation CreateEpsilonPerturbation(
        AllocationMatrix am,
        Matrix<double> cost
    ) => new EpsilonPerturbationParallel(am, cost, parDeg);

    private protected override PotCalcBase CreatePotentialsCalculator(
        AllocationMatrix am,
        Matrix<double> cost,
        double[] RPotential,
        double[] CPotential
    ) => new PotCalcParallel(am, cost, RPotential, CPotential, parDeg);
}
