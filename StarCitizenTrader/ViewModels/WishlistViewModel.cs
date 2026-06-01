using System.Collections.ObjectModel;
using System.Windows.Input;
using Serilog;
using StarCitizenTrader.Helpers;
using StarCitizenTrader.Models;
using StarCitizenTrader.Services;

namespace StarCitizenTrader.ViewModels;

/// Wishlist view — manage tracked items and view matches.
public class WishlistViewModel : ViewModelBase
{
    private readonly IDatabaseService _db;
    private readonly IUexApiService _api;

    private bool _isLoading;
    private WishlistItem? _selectedItem;
    private string _newItemName = string.Empty;
    private int _newItemId;
    private string _newOperation = "buy";
    private double? _newMaxPrice;
    private double? _newMinPrice;
    private string _newNotes = string.Empty;
    private bool _isAddingItem;

    public ObservableCollection<WishlistItem> WishlistItems { get; } = new();
    public ObservableCollection<WishlistMatch> RecentMatches { get; } = new();
    public ObservableCollection<PriceAverage> PriceComparisons { get; } = new();

    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public WishlistItem? SelectedItem
    {
        get => _selectedItem;
        set { SetProperty(ref _selectedItem, value); _ = LoadPriceComparisonAsync(); }
    }
    public string NewItemName { get => _newItemName; set => SetProperty(ref _newItemName, value); }
    public int NewItemId { get => _newItemId; set => SetProperty(ref _newItemId, value); }
    public string NewOperation { get => _newOperation; set => SetProperty(ref _newOperation, value); }
    public double? NewMaxPrice { get => _newMaxPrice; set => SetProperty(ref _newMaxPrice, value); }
    public double? NewMinPrice { get => _newMinPrice; set => SetProperty(ref _newMinPrice, value); }
    public string NewNotes { get => _newNotes; set => SetProperty(ref _newNotes, value); }
    public bool IsAddingItem { get => _isAddingItem; set => SetProperty(ref _isAddingItem, value); }

    public ICommand RefreshCommand { get; }
    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand ToggleActiveCommand { get; }
    public ICommand ShowAddFormCommand { get; }
    public ICommand CancelAddCommand { get; }

    public WishlistViewModel(IDatabaseService db, IUexApiService api)
    {
        _db = db;
        _api = api;

        RefreshCommand = new AsyncRelayCommand(LoadWishlistAsync);
        AddItemCommand = new AsyncRelayCommand(AddItemAsync);
        RemoveItemCommand = new AsyncRelayCommand(RemoveSelectedAsync);
        ToggleActiveCommand = new AsyncRelayCommand(ToggleActiveAsync);
        ShowAddFormCommand = new RelayCommand(() => IsAddingItem = true);
        CancelAddCommand = new RelayCommand(() => { IsAddingItem = false; ClearNewItemForm(); });
    }

    public async Task LoadWishlistAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _db.GetWishlistAsync();
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                WishlistItems.Clear();
                foreach (var item in items) WishlistItems.Add(item);
            });

            var matches = await _db.GetRecentMatchesAsync(20);
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                RecentMatches.Clear();
                foreach (var m in matches) RecentMatches.Add(m);
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load wishlist");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AddItemAsync()
    {
        if (string.IsNullOrWhiteSpace(NewItemName)) return;

        var item = new WishlistItem
        {
            IdItem = NewItemId,
            ItemName = NewItemName.Trim(),
            Operation = NewOperation,
            MaxPrice = NewMaxPrice,
            MinPrice = NewMinPrice,
            Currency = "UEC",
            NotifyOnMatch = true,
            IsActive = true,
            Notes = string.IsNullOrWhiteSpace(NewNotes) ? null : NewNotes.Trim()
        };

        var id = await _db.AddWishlistItemAsync(item);
        item.Id = id;

        System.Windows.Application.Current?.Dispatcher.Invoke(() => WishlistItems.Insert(0, item));

        ClearNewItemForm();
        IsAddingItem = false;
        Log.Information("Added wishlist item: {ItemName}", item.ItemName);
    }

    private async Task RemoveSelectedAsync()
    {
        if (SelectedItem == null) return;

        await _db.DeleteWishlistItemAsync(SelectedItem.Id);
        System.Windows.Application.Current?.Dispatcher.Invoke(() => WishlistItems.Remove(SelectedItem));
        SelectedItem = null;
    }

    private async Task ToggleActiveAsync()
    {
        if (SelectedItem == null) return;

        SelectedItem.IsActive = !SelectedItem.IsActive;
        await _db.UpdateWishlistItemAsync(SelectedItem);
        OnPropertyChanged(nameof(SelectedItem));

        // Refresh list to show updated state
        await LoadWishlistAsync();
    }

    private async Task LoadPriceComparisonAsync()
    {
        if (SelectedItem == null || SelectedItem.IdItem <= 0) return;

        try
        {
            var averages = await _api.GetPriceAveragesAsync(SelectedItem.IdItem);
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                PriceComparisons.Clear();
                foreach (var avg in averages) PriceComparisons.Add(avg);
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load price comparison for item {ItemId}", SelectedItem.IdItem);
        }
    }

    private void ClearNewItemForm()
    {
        NewItemName = string.Empty;
        NewItemId = 0;
        NewOperation = "buy";
        NewMaxPrice = null;
        NewMinPrice = null;
        NewNotes = string.Empty;
    }
}
