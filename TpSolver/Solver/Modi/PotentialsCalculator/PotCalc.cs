using System.Diagnostics;
using TpSolver.Shared;

namespace TpSolver.Solver.Modi.PotentialsCalculator;

class PotCalc(
    Matrix<double> cost,
    AllocationMatrix allocation,
    double[] RPotential,
    double[] CPotential
) : PotCalcBase(cost, allocation, RPotential, CPotential)
{
    readonly bool[] RDone = new bool[RPotential.Length];
    readonly bool[] CDone = new bool[CPotential.Length];
    readonly Queue<int> toCheck = new(RPotential.Length + CPotential.Length);

    protected override void CalcPotentialsFrom(int idx, bool isRPotential)
    {
        // clear done lists - to allow multiple calls
        for (int i = 0; i < m; i++)
            RDone[i] = false;
        if (isRPotential)
            RDone[idx] = true;

        for (int j = 0; j < n; j++)
            CDone[j] = false;
        if (!isRPotential)
            CDone[idx] = true;

        toCheck.Clear();
        toCheck.Enqueue(isRPotential ? idx : idx + m);
        while (toCheck.TryDequeue(out idx))
        {
            isRPotential = idx < m;
            List<int> calculated = isRPotential
                ? CalcAllNotCDoneUsing(idx)
                : CalcAllNotRDoneUsing(idx - m);
            foreach (var potential in calculated)
                toCheck.Enqueue(potential);
        }
    }

    private List<int> CalcAllNotCDoneUsing(int i)
    {
        Debug.Assert(RDone[i]);
        List<int> res = new(n);
        for (int j = 0; j < n; j++)
        {
            if (allocation[i, j].IsBasic && !CDone[j])
            {
                res.Add(m + j);
                CPotential[j] = cost[i, j] - RPotential[i];
                CDone[j] = true;
            }
        }
        return res;
    }

    private List<int> CalcAllNotRDoneUsing(int j)
    {
        Debug.Assert(CDone[j]);
        List<int> res = new(n);
        for (int i = 0; i < m; i++)
        {
            if (allocation[i, j].IsBasic && !RDone[i])
            {
                res.Add(i);
                RPotential[i] = cost[i, j] - CPotential[j];
                RDone[i] = true;
            }
        }
        return res;
    }
}
