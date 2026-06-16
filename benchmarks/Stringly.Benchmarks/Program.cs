using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Stringly;
using Stringly.Emoji;
using static Stringly.Say;
using static Stringly.Emoji.Start;

// Run all (default job):   dotnet run -c Release --project benchmarks/Stringly.Benchmarks -- --filter '*'
// Quick pass (short job):  dotnet run -c Release --project benchmarks/Stringly.Benchmarks -- --filter '*' --job short
BenchmarkSwitcher.FromAssembly(typeof(PhraseBenchmarks).Assembly).Run(args);

/// <summary>
/// The scenarios target the library's stated hot path: short emoji descriptions built and
/// rendered in tight loops (logs, UI).
/// </summary>
[MemoryDiagnoser]
public class PhraseBenchmarks
{
    // A phrase built once and rendered many times — the common "build, then log repeatedly" shape.
    private readonly Phrase _prebuilt = Fire().Rocket().Hundred();

    /// <summary>Build a 3-token emoji chain and render it (cold render every time).</summary>
    [Benchmark]
    public string Build_And_Render() => Fire().Rocket().Hundred().ToString();

    /// <summary>Render a phrase that was built earlier — hits the memoised path after the first call.</summary>
    [Benchmark]
    public string Render_Prebuilt() => _prebuilt.ToString();

    /// <summary>Render the same phrase 1000 times — the "log it in a loop" scenario memoisation targets.</summary>
    [Benchmark]
    public int Render_1000x()
    {
        var total = 0;
        for (var i = 0; i < 1000; i++)
        {
            total += _prebuilt.ToString().Length;
        }

        return total;
    }

    /// <summary>Build a longer ASCII chain (10 tokens) and render.</summary>
    [Benchmark]
    public string Build_Ascii_Chain() =>
        H().e().l().l().o().Space().W().o().r().d().ToString();

    /// <summary>Concatenate two phrases via the + operator.</summary>
    [Benchmark]
    public string Concat_Two_Phrases() => (Fire().Rocket() + Say.Text("!!!")).ToString();
}
