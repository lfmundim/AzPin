using System.Diagnostics;

namespace AzPin.Windows.Utilities;

public class ShellResult
{
    public string Stdout { get; init; } = string.Empty;
    public string Stderr { get; init; } = string.Empty;
    public int ExitCode { get; init; }
}

public class ShellException(string message, string stderr, int exitCode)
    : Exception(message)
{
    public string Stderr { get; } = stderr;
    public int ExitCode { get; } = exitCode;
}

public class ShellRunner : IShellRunner
{
    public async Task<ShellResult> RunAsync(
        string executable,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return new ShellResult
        {
            Stdout = await stdoutTask,
            Stderr = await stderrTask,
            ExitCode = process.ExitCode
        };
    }

    public async Task<string> RunAndReadAsync(
        string executable,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(executable, arguments, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new ShellException(
                $"'{executable} {arguments}' exited with code {result.ExitCode}: {result.Stderr}",
                result.Stderr,
                result.ExitCode);
        }

        return result.Stdout;
    }
}
