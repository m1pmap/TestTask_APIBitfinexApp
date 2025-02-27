using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TestTask.BLL.Interfaces;
using TestTask.BLL.Models;
using TestTask.BLL.Services;
using TestTask.Models;

namespace TestTask;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow() { InitializeComponent(); }
    private readonly ITestConnector _testConnectorService;
    private ObservableCollection<Trade> Trades { get; set; } = new();
    private ObservableCollection<Candle> Candles { get; set; } = new();

    private ObservableCollection<Trade> SubscribedTrades { get; set; } = new();
    private ObservableCollection<Candle> SubscribedCandles { get; set; } = new();

    private ObservableCollection<BalanceCurrency_MV> BalanceCurrencies { get; set; } = new();

    private List<string> ValidPairs = new List<string> { "BTCUST", "XRPBTC", "XMRBTC", "DSHBTC", "XRPUST", "XMRUST", "DSHUSD" };

    public MainWindow(ITestConnector testConnectorService)
    {
        InitializeComponent();
        _testConnectorService = testConnectorService;

        _testConnectorService.NewBuyTrade += OnNewTradeReceived;
        _testConnectorService.NewSellTrade += OnNewTradeReceived;
        _testConnectorService.CandleSeriesProcessing += OnNewCandleProcessingReceived;

        trades_dataGrid.ItemsSource = SubscribedTrades;
        candles_dataGrid.ItemsSource = SubscribedCandles;
        balance_dataGrid.ItemsSource = BalanceCurrencies;
    }

    private void OnNewTradeReceived(Trade trade)
    {
        App.Current.Dispatcher.Invoke(() => SubscribedTrades.Insert(0, trade));
    }

    private void OnNewCandleProcessingReceived(Candle candle)
    {
        App.Current.Dispatcher.Invoke(() => SubscribedCandles.Insert(0, candle));
    }

    public async Task LoadTradesAsync()
    {
        string pair = pair_data_textBox.Text;
        int count = Convert.ToInt32(count_data_textBox.Text);
        var trades = await _testConnectorService.GetNewTradesAsync(pair, count);
        Trades.Clear();
        foreach (var trade in trades)
        {
            Trades.Add(trade);
        }
        tradeCandle_dataGrid.ItemsSource = Trades;

        foreach (var column in tradeCandle_dataGrid.Columns)
        {
            column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
        }
    }

    public async Task LoadCandlesAsync()
    {
        string pair = pair_data_textBox.Text;
        int count = Convert.ToInt32(count_data_textBox.Text);

        int hours = Convert.ToInt32(HH_textBox.Text);
        int minutes = Convert.ToInt32(mm_textBox.Text);
        int seconds = Convert.ToInt32(ss_textBox.Text);
        DateTime timeFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, hours, minutes, seconds, DateTimeKind.Local);

        int interval = Convert.ToInt32(candleInterval_data_textBox.Text);

        var candles = await _testConnectorService.GetCandleSeriesAsync(pair, interval, timeFrom, count: count);
        Candles.Clear();
        foreach (var candle in candles)
        {
            Candles.Add(candle);
        }
        tradeCandle_dataGrid.ItemsSource = Candles;

        foreach (var column in tradeCandle_dataGrid.Columns)
        {
            column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
        }
    }

    private void gedTrades_button_Click(object sender, RoutedEventArgs e)
    {
        LoadTradesAsync();
    }

    private void gedCandles_button_Click(object sender, RoutedEventArgs e)
    {
        LoadCandlesAsync();
    }

    private void subscribeTrades_button_Click(object sender, RoutedEventArgs e)
    {
        _testConnectorService.SubscribeTrades(pair_subscribe_textBox.Text);
    }

    private void subscribeCandles_button_Click(object sender, RoutedEventArgs e)
    {
        int interval = Convert.ToInt32(candleInterval_subscribe_textBox.Text);
        _testConnectorService.SubscribeCandles(pair_subscribe_textBox.Text, interval);
    }

    private void unsubscribeTrades_button_Click(object sender, RoutedEventArgs e)
    {
        _testConnectorService.UnsubscribeTrades(pair_subscribe_textBox.Text);
    }

    private void unsubscribeCandles_button_Click(object sender, RoutedEventArgs e)
    {
        _testConnectorService.UnsubscribeCandles(pair_subscribe_textBox.Text);
    }

    private void Currency_button_Click(object sender, RoutedEventArgs e)
    {
        BalanceCurrencies.Clear();

        Button currentButton = (Button)sender;

        string currentCurrency = currentButton.Content.ToString();
        ConvertUserBalanceAsync(currentCurrency);
    }

    public async Task ConvertUserBalanceAsync(string currency)
    {
        if(currency == "USDT") { currency = "UST"; }
        else if (currency == "DASH") { currency = "DSH"; }

        if (currency != "BTC")
        {
            if (ValidPairs.Contains($"BTC{currency}"))
            {
                Trade BTCCurrency = (await _testConnectorService.GetNewTradesAsync($"BTC{currency}", 1)).FirstOrDefault();
                BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = "BTC", Count = 1 * BTCCurrency.Price });
            }
            else
            {
                if (ValidPairs.Contains($"{currency}BTC"))
                {
                    Trade CurrencyBTC = (await _testConnectorService.GetNewTradesAsync($"{currency}BTC", 1)).FirstOrDefault();
                    BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = "BTC", Count = 1 / CurrencyBTC.Price });
                }
                else
                {
                    BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = "BTC", Count = 0 });
                }
            }
        }
        else
        {
            BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = "BTC", Count = 1 });
        }

        if (currency != "XRP")
        {
            if (ValidPairs.Contains($"XRP{currency}"))
            {
                Trade XRPCurrency = (await _testConnectorService.GetNewTradesAsync($"XRP{currency}", 1)).FirstOrDefault();
                BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = "XRP", Count = 15000 * XRPCurrency.Price });
            }
            else
            {
                BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = "XRP", Count = 0 });
            }
        }
        else
        {
            BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = "XRP", Count = 15000 });
        }

        if (currency != "XMR")
        {
            if (ValidPairs.Contains($"XMR{currency}"))
            {
                Trade XMRCurrency = (await _testConnectorService.GetNewTradesAsync($"XMR{currency}", 1)).FirstOrDefault();
                BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = "XMR", Count = 50 * XMRCurrency.Price });
            }
            else
            {
                BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = "XMR", Count = 0 });
            }
        }
        else
        {
            BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = "XMR", Count = 50 });
        }


        if (currency == "UST") { currency = "USD"; }
        
        if (currency != "DSH")
        {
            if (ValidPairs.Contains($"DSH{currency}"))
            {
                Trade DASHCurrency = (await _testConnectorService.GetNewTradesAsync($"DSH{currency}", 1)).FirstOrDefault();
                BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = "DSH", Count = 30 * DASHCurrency.Price });
            }
            else
            {
                BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = "DSH", Count = 0 });
            }
        }
        else
        {
            BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = "DSH", Count = 30 });
        }
    }
}