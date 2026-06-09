using AzPin.Windows.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AzPin.Windows.MainWindow.Pages;

public sealed partial class OnboardingPage : UserControl
{
    private readonly OnboardingViewModel _vm;

    public OnboardingPage(OnboardingViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        vm.PropertyChanged += (_, _) => DispatcherQueue.TryEnqueue(Refresh);
        Refresh();
    }

    private void Refresh()
    {
        StepsPanel.Children.Clear();
        StepsPanel.Children.Add(BuildStep("Azure CLI installed", _vm.CliState, _vm.CliHelpText));
        StepsPanel.Children.Add(BuildStep("Signed in (az login)", _vm.AuthState, _vm.AuthHelpText));
        StepsPanel.Children.Add(BuildStep("Subscription accessible", _vm.SubscriptionState, _vm.SubscriptionHelpText));
    }

    private static StackPanel BuildStep(string label, OnboardingViewModel.StepState state, string? helpText)
    {
        var row = new StackPanel { Spacing = 4 };

        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

        if (state == OnboardingViewModel.StepState.Checking)
        {
            header.Children.Add(new ProgressRing { Width = 16, Height = 16, IsActive = true });
        }
        else
        {
            var (glyph, color) = state switch
            {
                OnboardingViewModel.StepState.Pass => ("", Colors.Green),
                OnboardingViewModel.StepState.Fail => ("", Colors.Red),
                _ => ("", Colors.Gray)
            };
            header.Children.Add(new TextBlock
            {
                Text = glyph,
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 16,
                Foreground = new SolidColorBrush(color),
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        header.Children.Add(new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center
        });

        row.Children.Add(header);

        if (!string.IsNullOrEmpty(helpText))
        {
            row.Children.Add(new TextBlock
            {
                Text = helpText,
                Foreground = new SolidColorBrush(Colors.OrangeRed),
                FontSize = 12,
                Margin = new Thickness(26, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
        }

        return row;
    }
}
