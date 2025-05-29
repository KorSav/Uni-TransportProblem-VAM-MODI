using System.Collections.Concurrent;
using System.Diagnostics;
using TpSolver.Shared;

namespace TpSolver.Solver.Modi.PotentialsCalculator;

class PotCalcParallel(
    Matrix<double> cost,
    AllocationMatrix allocation,
    double[] RPotential,
    double[] CPotential,
    int parDeq
) : PotCalcBase(cost, allocation, RPotential, CPotential)
{
    readonly ConcurrentBag<int> RDone = [];
    readonly ConcurrentBag<int> CDone = [];

    SemaphoreSlim semaphore = new(0);
    readonly int parDeg = parDeq;
    volatile bool isCalcEnded = false;
    volatile int cntWorkRemain;
    volatile int cntNextWork;
    volatile bool isRowIndex;
    ConcurrentQueue<int> toCheck = new();

    protected override void CalcPotentialsFrom(int idx, bool isRPotential)
    {
        RDone.Clear();
        CDone.Clear();
        if (isRPotential)
            RDone.Add(idx);
        else
            CDone.Add(idx);

        toCheck = new();
        toCheck.Enqueue(idx);
        isRowIndex = isRPotential;

        semaphore = new(1);
        cntWorkRemain = 1;
        cntNextWork = 0;
        isCalcEnded = false;

        Task[] tasks = new Task[parDeg];
        for (int i = 0; i < parDeg; i++)
            tasks[i] = Task.Run(CalcPotentialsInParallel);
        Task.WaitAll(tasks);
    }

    private void CalcPotentialsInParallel()
    {
        while (!isCalcEnded)
        {
            semaphore.Wait();
            bool wasIdxInQueue = toCheck.TryDequeue(out int idx);
            if (!wasIdxInQueue) // happens on termination
                continue;

            // find potentials for either col or row in parallel
            List<int> pairedResults = isRowIndex
                ? CalcAllNotCDoneUsing(idx)
                : CalcAllNotRDoneUsing(idx);
            foreach (var potential in pairedResults)
            {
                toCheck.Enqueue(potential);
                Interlocked.Increment(ref cntNextWork);
            }

            // last worker updates counters and starts new cycle
            if (Interlocked.Decrement(ref cntWorkRemain) == 0)
            {
                // semaphore stopped all other workers, no race conditions here
                if (cntNextWork == 0)
                {
                    isCalcEnded = true;
                    semaphore.Release(parDeg);
                    break;
                }
                int cntNextCycleSpins = cntNextWork;
                cntWorkRemain = cntNextWork;
                cntNextWork = 0;
                isRowIndex = !isRowIndex;
                // start cycle should be last command to avoid race conditions
                semaphore.Release(cntNextCycleSpins);
            }
        }
    }

    private List<int> CalcAllNotCDoneUsing(int i)
    {
        Debug.Assert(RDone.Contains(i));
        List<int> res = new(n);
        for (int j = 0; j < n; j++)
        {
            // race condition is OK - recalculate potential should give same result
            if (allocation[i, j].IsBasic && !CDone.Contains(j))
            {
                CDone.Add(j); // add here to minimize race conditions
                res.Add(j);
                CPotential[j] = cost[i, j] - RPotential[i];
            }
        }
        return res;
    }

    private List<int> CalcAllNotRDoneUsing(int j)
    {
        Debug.Assert(CDone.Contains(j));
        List<int> res = new(n);
        for (int i = 0; i < m; i++)
        {
            if (allocation[i, j].IsBasic && !RDone.Contains(i))
            {
                RDone.Add(i);
                res.Add(i);
                RPotential[i] = cost[i, j] - CPotential[j];
            }
        }
        return res;
    }
}
