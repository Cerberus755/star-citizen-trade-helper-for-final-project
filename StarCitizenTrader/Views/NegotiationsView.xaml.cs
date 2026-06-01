using System.Windows;
using System.Windows.Controls;
using StarCitizenTrader.ViewModels;

namespace StarCitizenTrader.Views;

public partial class NegotiationsView : UserControl
{
    public NegotiationsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is NegotiationsViewModel vm)
            await vm.LoadNegotiationsAsync();
    }
}
