using TpSolver.Shared;

namespace TpSolver.BfsSearch;

/// <summary>
/// Tries to execute at most <paramref name="parallelizationDegree"/> tasks in parallel.
/// <para>
/// Int.Max removes limit.
/// </para>
/// </summary>
public class VamParallel(TransportProblem tp, int parallelizationDegree) : Vam(tp)
{
    const int indexNotSet = -1;
    readonly int parDeg = parallelizationDegree;

    protected override int ArgmaxPenalty(out bool isRow)
    {
        // despite double is used for penalties
        // it is possible for two exactly same penalties occur
        // especially, when many numbers are randomly generated
        // in this case algorithm assures to choose maximum:
        //   - if in the same vector: with lowest index
        //   - if in different vectors: row dominate over column
        int ir = indexNotSet;
        int ic = indexNotSet;
        void findMaxCompound(int iCompound)
        {
            bool isRow = iCompound < m;
            if (isRow)
            {
                int i = iCompound;
                if (rowDone[i])
                    return;
                rowPenalty[i] = CalcRowPenalty(i);
                AtomicUpdateMaxPenalty(ref ir, rowPenalty, i);
                return;
            }
            int j = iCompound - m;
            if (colDone[j])
                return;
            colPenalty[j] = CalcColPenalty(j);
            AtomicUpdateMaxPenalty(ref ic, colPenalty, j);
        }
        Parallel.For(0, m + n, new() { MaxDegreeOfParallelism = parDeg }, findMaxCompound);

        if (ir == indexNotSet)
        {
            isRow = false;
            return ic;
        }
        if (ic == indexNotSet)
        {
            isRow = true;
            return ir;
        }
        isRow = rowPenalty[ir] >= colPenalty[ic];

        return isRow ? ir : ic;
    }

    private static void AtomicUpdateMaxPenalty(ref int curMax, double[] penalty, int i)
    {
        if (indexNotSet == Interlocked.CompareExchange(ref curMax, i, indexNotSet))
            return;
        int imaxBeforeCheck;
        do
        {
            imaxBeforeCheck = curMax;
            if (
                penalty[i] < penalty[imaxBeforeCheck]
                || penalty[i] == penalty[imaxBeforeCheck] && i > imaxBeforeCheck
            )
                return;
        } while (imaxBeforeCheck != Interlocked.CompareExchange(ref curMax, i, imaxBeforeCheck));
    }

    protected override Point ArgminColCost(int j)
    {
        int minCompound = indexNotSet;
        Parallel.For(
            0,
            m,
            new() { MaxDegreeOfParallelism = parDeg },
            (i) => AtomicUpdateArgminCost(ref minCompound, i * n + j)
        );
        return new(minCompound / n, minCompound % n);
    }

    protected override Point ArgminRowCost(int i)
    {
        int minCompound = indexNotSet;
        Parallel.For(
            0,
            n,
            new() { MaxDegreeOfParallelism = parDeg },
            (j) => AtomicUpdateArgminCost(ref minCompound, i * n + j)
        );
        return new(minCompound / n, minCompound % n);
    }

    private void AtomicUpdateArgminCost(ref int minCompound, int iCompound)
    {
        Point pnt = new(iCompound / n, iCompound % n);
        if (rowDone[pnt.IRow] || colDone[pnt.ICol])
            return;
        if (indexNotSet == Interlocked.CompareExchange(ref minCompound, iCompound, indexNotSet))
            return;
        int iminBeforeCheck;
        do
        {
            iminBeforeCheck = minCompound;
            Point min = new(minCompound / n, minCompound % n);
            if (tp.Cost[pnt] > tp.Cost[min] || (tp.Cost[pnt] == tp.Cost[min] && pnt > min))
                return;
        } while (
            iminBeforeCheck
            != Interlocked.CompareExchange(ref minCompound, iCompound, iminBeforeCheck)
        );
    }
}
