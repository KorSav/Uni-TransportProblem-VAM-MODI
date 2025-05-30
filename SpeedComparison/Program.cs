using System.Text;
using Profiling;
using TpSolver;
using TpSolver.Solver.Modi;

int[] sizes = [100, 200, 400, 600, 800];
int[] procCounts = [2, 4, 6, 8, 12];
int passCount = 20;

string csvPath = "benchmark_results.csv";
using StreamWriter writer = new(csvPath, false, Encoding.UTF8);

Console.WriteLine("Warming up with size 400...");
TransportProblem tpWarm = TransportProblem.GenerateRandom(400, 400);
var dummySeq = new ModiSolverSeq(tpWarm);
dummySeq.Solve();
var dummyPar = new ModiSolverParallel(tpWarm, 6);
dummyPar.Solve();
Console.WriteLine("Warm-up complete.\n");

foreach (int size in sizes)
{
    Console.WriteLine($"Starting benchmark for size {size}...");
    var (profilerSeq, profilerPar) = RunBenchmarkPasses(size);
    WriteBenchmarkResults(writer, size, profilerSeq, profilerPar);
    writer.WriteLine();
    writer.Flush();
    Console.WriteLine($"Finished benchmark for size {size}.\n");
}

(Profiler seq, Profiler[] par) RunBenchmarkPasses(int size)
{
    Profiler profilerSeq = new();
    Profiler[] profilersPar = new Profiler[procCounts.Length];
    for (int i = 0; i < profilersPar.Length; i++)
        profilersPar[i] = new();

    for (int pass = 0; pass < passCount; pass++)
    {
        Console.WriteLine($"Pass {pass + 1}/{passCount} for size {size}...");
        bool runSeqFirst = pass % 2 == 0;
        TransportProblem tp = TransportProblem.GenerateRandom(size, size);

        if (runSeqFirst)
        {
            Console.WriteLine("Running sequential solver...");
            RunAndRecordSequential(tp, profilerSeq);
            Console.WriteLine("Running parallel solver...");
            RunAndRecordParallel(tp, profilersPar);
        }
        else
        {
            Console.WriteLine("Running parallel solver...");
            RunAndRecordParallel(tp, profilersPar);
            Console.WriteLine("Running sequential solver...");
            RunAndRecordSequential(tp, profilerSeq);
        }
    }

    return (profilerSeq, profilersPar);
}

string DetermineTimeUnit(IEnumerable<TimeSpan> allTimes)
{
    TimeSpan maxTime = allTimes.Max();
    if (maxTime.Minutes > 0)
        return "min";
    if (maxTime.Seconds > 0)
        return "s";
    return "ms";
}

double AvgTimeBasedOnTimeUnit(TimeSpan ts, string timeUnit) =>
    timeUnit switch
    {
        "min" => ts.TotalMinutes / passCount,
        "s" => ts.TotalSeconds / passCount,
        "ms" => ts.TotalMilliseconds / passCount,
        _ => ts.TotalSeconds / passCount,
    };

void WriteBenchmarkResults(
    StreamWriter writer,
    int size,
    Profiler profilerSeq,
    Profiler[] profilersPar
)
{
    var allTimes = profilerSeq
        .Select(sm => sm.TotalElapsed / passCount)
        .Concat(profilersPar.SelectMany(prof => prof.Select(sm => sm.TotalElapsed / passCount)));
    var timeUnit = DetermineTimeUnit(allTimes);

    writer.WriteLine($"Benchmark for size {size} ({timeUnit})");
    writer.Write("Stage (" + timeUnit + ");Sequential");

    foreach (int pc in procCounts)
        writer.Write($";Parallel-{pc}");
    writer.WriteLine();

    foreach (var stage in profilerSeq.Skip(1).Append(profilerSeq.First())) // for total to be last row
    {
        writer.Write(stage.Name);
        writer.Write($";{AvgTimeBasedOnTimeUnit(stage.TotalElapsed, timeUnit)}");
        foreach (var prof in profilersPar)
            writer.Write($";{AvgTimeBasedOnTimeUnit(prof[stage.Name].TotalElapsed, timeUnit)}");
        writer.WriteLine();
        writer.Flush();
        Console.WriteLine($"Written results for stage: {stage.Name}");
    }
}

void RunAndRecordSequential(TransportProblem tp, Profiler profiler)
{
    ModiSolver ms = new ModiSolverSeq(tp) { Profiler = profiler };
    ms.Solve();
}

void RunAndRecordParallel(TransportProblem tp, Profiler[] profilers)
{
    for (int i = 0; i < procCounts.Length; i++)
    {
        int pc = procCounts[i];
        ModiSolver ms = new ModiSolverParallel(tp, pc) { Profiler = profilers[i] };
        ms.Solve();
    }
}
