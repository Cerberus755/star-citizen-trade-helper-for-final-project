using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Serilog;
using StarCitizenTrader.Helpers;
using StarCitizenTrader.Models;
using StarCitizenTrader.Services;

namespace StarCitizenTrader.ViewModels;

/// Active Listings view — browse, search, sort, and filter marketplace listings.
public class ListingsViewModel : ViewModelBase
{
    private readonly IUexApiService _api;
    private readonly IDatabaseService _db;
    private readonly IMarketplaceMonitorService _monitor;

    private bool _isLoading;
    private string _searchText = string.Empty;
    private string _filterOperation = "All";
    private string _sortProperty = "DateAdded";
    private MarketplaceListing? _selectedListing;
    private int _listingCount;

    public ObservableCollection<MarketplaceListing> Listings { get; } = new();
    public ICollectionView ListingsView { get; }

    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) ListingsView.Refresh(); }
    }
    public string FilterOperation
    {
        get => _filterOperation;
        set { if (SetProperty(ref _filterOperation, value)) ListingsView.Refresh(); }
    }
    public string SortProperty
    {
        get => _sortProperty;
        set { if (SetProperty(ref _sortProperty, value)) ApplySorting(); }
    }
    public MarketplaceListing? SelectedListing
    {
        get => _selectedListing;
        set => SetProperty(ref _selectedListing, value);
    }
    public int ListingCount { get => _listingCount; set => SetProperty(ref _listingCount, value); }

    public List<string> OperationFilters { get; } = new() { "All", "Buy", "Sell" };
    public List<string> SortOptions { get; } = new() { "DateAdded", "Price", "Title", "Votes" };

    public ICommand RefreshCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand AddToWishlistCommand { get; }

    public ListingsViewModel(IUexApiService api, IDatabaseService db, IMarketplaceMonitorService monitor)
    {
        _api = api;
        _db = db;
        _monitor = monitor;

        ListingsView = CollectionViewSource.GetDefaultView(Listings);
        ListingsView.Filter = FilterListings;

        RefreshCommand = new AsyncRelayCommand(LoadListingsAsync);
        SearchCommand = new RelayCommand(() => ListingsView.Refresh());
        AddToWishlistCommand = new AsyncRelayCommand(AddSelectedToWishlistAsync);

        _monitor.DataRefreshed += async (_, _) => await LoadListingsAsync();
    }

    public async Task LoadListingsAsync()
    {
        IsLoading = true;
        try
        {
            var listings = await _db.GetCachedListingsAsync();
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                Listings.Clear();
                foreach (var l in listings) Listings.Add(l);
                ListingCount = listings.Count;
                ApplySorting();
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load listings");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool FilterListings(object obj)
    {
        if (obj is not MarketplaceListing listing) return false;

        // Operation filter
        if (FilterOperation != "All")
        {
            if (!listing.Operation.Equals(FilterOperation, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // Text search
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLower();
            return listing.Title.ToLower().Contains(search) ||
                   listing.UserUsername.ToLower().Contains(search) ||
                   (listing.Location?.ToLower().Contains(search) ?? false) ||
                   (listing.Description?.ToLower().Contains(search) ?? false);
        }

        return true;
    }

    private void ApplySorting()
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            ListingsView.SortDescriptions.Clear();
            var direction = SortProperty == "Price" || SortProperty == "Votes"
                ? ListSortDirection.Descending
                : (SortProperty == "Title" ? ListSortDirection.Ascending : ListSortDirection.Descending);

            ListingsView.SortDescriptions.Add(new SortDescription(SortProperty, direction));
        });
    }

    private async Task AddSelectedToWishlistAsync()
    {
        if (SelectedListing == null) return;

        var item = new WishlistItem
        {
            IdItem = SelectedListing.IdItem,
            ItemName = SelectedListing.Title,
            Operation = SelectedListing.IsSellOrder ? "buy" : "sell",
            Currency = SelectedListing.Currency,
            NotifyOnMatch = true,
            IsActive = true
        };

        await _db.AddWishlistItemAsync(item);
        Log.Information("Added to wishlist: {ItemName}", item.ItemName);
    }
}
