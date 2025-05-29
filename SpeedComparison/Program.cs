// See https://aka.ms/new-console-template for more information
using TpSolver;
using TpSolver.Solver.Modi;

int[] sizes = [100, 200, 400, 600, 800];
int[] procCount = [2, 4, 6, 8, 12];

foreach (int size in sizes)
{
    TransportProblem tp = TransportProblem.GenerateRandom(size, size);

    ModiSolver ms = new ModiSolverSeq(tp) { Profiler = new() };

    ms.Solve();
    System.Console.WriteLine($"[Perf seq, size {size}]");
    foreach (var stage in ms.Profiler.OrderByDescending(s => s.TotalElapsed))
        System.Console.WriteLine($"{stage.Name}: {stage.TotalElapsed}");

    System.Console.WriteLine();

    foreach (int pc in procCount)
    {
        ms = new ModiSolverParallel(tp, pc) { Profiler = new() };
        ms.Solve();
        System.Console.WriteLine($"[Perf par, size {size} / {pc}]");
        foreach (var stage in ms.Profiler.OrderByDescending(s => s.TotalElapsed))
            System.Console.WriteLine($"{stage.Name}: {stage.TotalElapsed}");
        System.Console.WriteLine();
    }
}
