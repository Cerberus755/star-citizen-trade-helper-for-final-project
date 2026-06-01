using System.Windows;
using System.Windows.Controls;
using StarCitizenTrader.ViewModels;

namespace StarCitizenTrader.Views;

public partial class WishlistView : UserControl
{
    public WishlistView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is WishlistViewModel vm)
            await vm.LoadWishlistAsync();
    }
}
