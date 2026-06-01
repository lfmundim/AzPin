using AzPin.Windows.Utilities;

namespace AzPin.Windows.Tests.Tests;

public class ShellRunnerTests
{
    [Fact]
    public async Task RunAsync_ReturnsStdout_OnZeroExit()
    {
        var runner = new ShellRunner();
        var result = await runner.RunAsync("/bin/sh", "-c \"printf hello\"");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("hello", result.Stdout);
    }

    [Fact]
    public async Task RunAndReadAsync_ThrowsShellException_OnNonZeroExit()
    {
        var runner = new ShellRunner();

        var ex = await Assert.ThrowsAsync<ShellException>(() => runner.RunAndReadAsync("/bin/sh", "-c \"echo boom 1>&2; exit 7\""));

        Assert.Equal(7, ex.ExitCode);
        Assert.Contains("boom", ex.Stderr);
    }
}
