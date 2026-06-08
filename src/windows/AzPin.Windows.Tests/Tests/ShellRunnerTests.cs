using AzPin.Windows.Utilities;

namespace AzPin.Windows.Tests.Tests;

public class ShellRunnerTests
{
    [Fact]
    public async Task RunAsync_ReturnsStdout_OnZeroExit()
    {
        var runner = new ShellRunner();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var command = ShellCommand(
            "-NoProfile -NonInteractive -Command \"[Console]::Out.Write('hello')\"",
            "-c \"printf hello\"");

        var result = await runner.RunAsync(command.Executable, command.Arguments, cts.Token);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("hello", result.Stdout);
    }

    [Fact]
    public async Task RunAndReadAsync_ThrowsShellException_OnNonZeroExit()
    {
        var runner = new ShellRunner();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var command = ShellCommand(
            "-NoProfile -NonInteractive -Command \"[Console]::Error.WriteLine('boom'); exit 7\"",
            "-c \"echo boom 1>&2; exit 7\"");

        var ex = await Assert.ThrowsAsync<ShellException>(() => runner.RunAndReadAsync(command.Executable, command.Arguments, cts.Token));

        Assert.Equal(7, ex.ExitCode);
        Assert.Contains("boom", ex.Stderr);
    }

    private static (string Executable, string Arguments) ShellCommand(string windowsArguments, string unixArguments)
    {
        return OperatingSystem.IsWindows()
            ? ("powershell.exe", windowsArguments)
            : ("/bin/sh", unixArguments);
    }
}
