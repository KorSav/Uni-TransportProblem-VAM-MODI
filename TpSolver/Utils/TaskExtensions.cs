namespace TpSolver.Utils;

static class TaskExtensions
{
    /// <summary>
    /// Almost like <see cref="Parallel.For"/> but provides each task with index range.
    /// </summary>
    /// <param name="iFrom">The starting index (inclusive)</param>
    /// <param name="iTo">The ending index (exclusive)</param>
    /// <param name="func">
    /// A function that takes a start and end index defining a subrange, and returns a result.
    /// The range is represented as [iFrom, iTo)
    /// </param>
    /// <returns>
    /// True if the range was successfully partitioned and tasks were scheduled; false if the range was too small.
    /// </returns>
    public static bool TryRunDistributedRange<TResult>(
        this Task<TResult>[] tasks,
        int iFrom,
        int iTo,
        Func<int, int, TResult> func
    )
    {
        int iCnt = iTo - iFrom;
        if (iCnt < tasks.Length)
            return false;
        int baseChunkSize = iCnt / tasks.Length;
        int residue = iCnt % tasks.Length;
        int curI = iFrom;
        for (int i = 0; i < tasks.Length; i++)
        {
            int chunkSize = baseChunkSize + (i < residue ? 1 : 0);
            int taskIFrom = curI;
            int taskITo = curI + chunkSize;
            curI = taskITo;
            tasks[i] = Task.Run(() => func(taskIFrom, taskITo));
        }
        return true;
    }
}
