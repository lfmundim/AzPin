using Microsoft.Win32;

namespace AzPin.Windows.Utilities;

public static class AppSettings
{
    private const string RegKeyPath = @"Software\AzPin";
    private const string OnboardingValueName = "HasCompletedOnboarding";

    public static bool IsOnboardingCompleted()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegKeyPath);
            return key?.GetValue(OnboardingValueName) is 1;
        }
        catch { return false; }
    }

    public static void SetOnboardingCompleted(bool value)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegKeyPath);
            key?.SetValue(OnboardingValueName, value ? 1 : 0, RegistryValueKind.DWord);
        }
        catch { }
    }
}
