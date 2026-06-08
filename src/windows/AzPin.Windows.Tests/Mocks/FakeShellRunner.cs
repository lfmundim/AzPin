using AzPin.Windows.Utilities;

namespace AzPin.Windows.Tests.Mocks;

internal sealed class FakeShellRunner : IShellRunner
{
    public Func<string, string, ShellResult> Handler { get; set; } = (_, _) => new ShellResult { ExitCode = 0 };

    public Task<ShellResult> RunAsync(string executable, string arguments, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Handler(executable, arguments));
    }

    public async Task<string> RunAndReadAsync(string executable, string arguments, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(executable, arguments, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new ShellException("command failed", result.Stderr, result.ExitCode);
        }

        return result.Stdout;
    }
}
