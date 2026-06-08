using AzPin.Windows.Utilities;

namespace AzPin.Windows.Tests.Tests;

public class ShellRunnerTests
{
    [Fact]
    public async Task RunAsync_ReturnsStdout_OnZeroExit()
    {
        var runner = new ShellRunner();
        var command = ShellCommand(
            "/c \"set /p dummy=hello<nul\"",
            "-c \"printf hello\"");

        var result = await runner.RunAsync(command.Executable, command.Arguments);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("hello", result.Stdout);
    }

    [Fact]
    public async Task RunAndReadAsync_ThrowsShellException_OnNonZeroExit()
    {
        var runner = new ShellRunner();
        var command = ShellCommand(
            "/c \"echo boom 1>&2 & exit /b 7\"",
            "-c \"echo boom 1>&2; exit 7\"");

        var ex = await Assert.ThrowsAsync<ShellException>(() => runner.RunAndReadAsync(command.Executable, command.Arguments));

        Assert.Equal(7, ex.ExitCode);
        Assert.Contains("boom", ex.Stderr);
    }

    private static (string Executable, string Arguments) ShellCommand(string windowsArguments, string unixArguments)
    {
        return OperatingSystem.IsWindows()
            ? ("cmd.exe", windowsArguments)
            : ("/bin/sh", unixArguments);
    }
}
