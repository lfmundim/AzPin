using AzPin.Windows.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace AzPin.Windows.MainWindow.Pages;

public sealed partial class BrowsePage : Page
{
    public BrowseViewModel ViewModel { get; }

    public BrowsePage()
    {
        ViewModel = App.Services.GetRequiredService<BrowseViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (ViewModel.Subscriptions.Count == 0)
            await ViewModel.LoadSubscriptionsAsync();
    }
}

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

public sealed class InvertBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is not null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

public sealed class PinGlyphConverter : IValueConverter
{
    // Filled pin vs outline pin (Segoe Fluent Icons)
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? "" : "";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

public sealed class PinForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is true)
            return Application.Current.Resources["AccentFillColorDefaultBrush"] as Brush
                   ?? new SolidColorBrush(Colors.Blue);
        return Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush
               ?? new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

public sealed class ChevronGlyphConverter : IValueConverter
{
    // ChevronDown when expanded, ChevronRight when collapsed
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? "" : "";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
