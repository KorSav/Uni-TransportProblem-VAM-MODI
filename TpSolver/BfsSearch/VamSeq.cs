using System.Diagnostics;
using TpSolver.Shared;

namespace TpSolver.BfsSearch;

public class VamSeq(TransportProblem tp) : Vam(tp)
{
    protected override int ArgmaxPenalty(out bool isRow)
    {
        double maxPenalty = -1;
        int idx = -1;

        for (int i = 0; i < m; i++)
        {
            if (rowDone[i])
                continue;
            rowPenalty[i] = CalcRowPenalty(i);
            Debug.Assert(rowPenalty[i] >= 0);
            if (rowPenalty[i] > maxPenalty)
            {
                maxPenalty = rowPenalty[i];
                idx = i;
            }
        }

        for (int j = 0; j < n; j++)
        {
            if (colDone[j])
                continue;
            colPenalty[j] = CalcColPenalty(j);
            Debug.Assert(colPenalty[j] >= 0);
            if (colPenalty[j] > maxPenalty)
            {
                maxPenalty = colPenalty[j];
                idx = m + j;
            }
        }
        Debug.Assert(maxPenalty >= 0); // 0 is possible if one cost remained nondone

        isRow = idx < m;
        return isRow switch
        {
            true => idx,
            false => idx - m,
        };
    }

    protected override Point ArgminColCost(int j)
    {
        double minCost = double.PositiveInfinity;
        int res_i = -1;
        for (int i = 0; i < m; i++)
        {
            if (rowDone[i])
                continue;
            if (tp.Cost[i, j] < minCost)
            {
                minCost = tp.Cost[i, j];
                res_i = i;
            }
        }
        return new(res_i, j);
    }

    protected override Point ArgminRowCost(int i)
    {
        double minCost = double.PositiveInfinity;
        int res_j = -1;
        for (int j = 0; j < n; j++)
        {
            if (colDone[j])
                continue;
            if (tp.Cost[i, j] < minCost)
            {
                minCost = tp.Cost[i, j];
                res_j = j;
            }
        }
        return new(i, res_j);
    }
}
