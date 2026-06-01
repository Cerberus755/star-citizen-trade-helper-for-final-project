using System.Collections.ObjectModel;
using System.Windows.Input;
using Serilog;
using StarCitizenTrader.Helpers;
using StarCitizenTrader.Models;
using StarCitizenTrader.Services;

namespace StarCitizenTrader.ViewModels;

/// Negotiations view — chat with trade counterparties.
public class NegotiationsViewModel : ViewModelBase
{
    private readonly IUexApiService _api;

    private bool _isLoading;
    private bool _hasAuth;
    private Negotiation? _selectedNegotiation;
    private string _newMessage = string.Empty;

    public ObservableCollection<Negotiation> Negotiations { get; } = new();
    public ObservableCollection<NegotiationMessage> Messages { get; } = new();

    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public bool HasAuth { get => _hasAuth; set => SetProperty(ref _hasAuth, value); }

    public Negotiation? SelectedNegotiation
    {
        get => _selectedNegotiation;
        set { SetProperty(ref _selectedNegotiation, value); _ = LoadMessagesAsync(); }
    }

    public string NewMessage
    {
        get => _newMessage;
        set => SetProperty(ref _newMessage, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand SendMessageCommand { get; }

    public NegotiationsViewModel(IUexApiService api)
    {
        _api = api;
        HasAuth = api.HasFullAuth;

        RefreshCommand = new AsyncRelayCommand(LoadNegotiationsAsync);
        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync);
    }

    public async Task LoadNegotiationsAsync()
    {
        if (!_api.HasFullAuth)
        {
            HasAuth = false;
            return;
        }
        HasAuth = true;

        IsLoading = true;
        try
        {
            var negotiations = await _api.GetNegotiationsAsync();
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                Negotiations.Clear();
                foreach (var n in negotiations.OrderByDescending(n => n.DateModified))
                    Negotiations.Add(n);
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load negotiations");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadMessagesAsync()
    {
        if (SelectedNegotiation == null) return;

        IsLoading = true;
        try
        {
            var messages = await _api.GetNegotiationMessagesAsync(SelectedNegotiation.Hash);
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                Messages.Clear();
                foreach (var m in messages.OrderBy(m => m.DateAdded))
                    Messages.Add(m);
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load negotiation messages");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SendMessageAsync()
    {
        if (SelectedNegotiation == null || string.IsNullOrWhiteSpace(NewMessage)) return;

        try
        {
            var msgId = await _api.SendNegotiationMessageAsync(SelectedNegotiation.Hash, NewMessage.Trim());
            if (msgId.HasValue)
            {
                NewMessage = string.Empty;
                await LoadMessagesAsync();
                Log.Information("Sent negotiation message #{MsgId}", msgId.Value);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to send negotiation message");
        }
    }
}
