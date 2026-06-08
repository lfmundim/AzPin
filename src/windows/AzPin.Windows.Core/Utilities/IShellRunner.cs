namespace AzPin.Windows.Utilities;

public interface IShellRunner
{
    Task<ShellResult> RunAsync(string executable, string arguments, CancellationToken cancellationToken = default);
    Task<string> RunAndReadAsync(string executable, string arguments, CancellationToken cancellationToken = default);
}
