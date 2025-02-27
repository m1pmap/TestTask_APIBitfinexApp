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

namespace TestTask;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow() { InitializeComponent(); }
    private readonly ITestConnector _testConnectorService;
    public ObservableCollection<Trade> Trades { get; set; } = new ObservableCollection<Trade>();
    public ObservableCollection<Candle> Candles { get; set; } = new ObservableCollection<Candle>();
    public ObservableCollection<Trade> SubscribedTrades { get; set; } = new ObservableCollection<Trade>();
    public ObservableCollection<Candle> SubscribedCandles { get; set; } = new ObservableCollection<Candle>();
    public MainWindow(ITestConnector testConnectorService)
    {
        InitializeComponent();
        _testConnectorService = testConnectorService;

        //_testConnectorService.GetNewTradesAsync("BTCUSD", 12);

        //_testConnectorService.GetNewTradesAsync("BTCUSD", 12);
        //_testConnectorService.GetCandleSeriesAsync("ETHUSD", 15, DateTimeOffset.UtcNow.AddHours(-1).AddHours(-1), count:  5);


        //_testConnectorService.SubscribeCandles("BTCUSD", 1);

        _testConnectorService.NewBuyTrade += OnNewTradeReceived;
        _testConnectorService.NewSellTrade += OnNewTradeReceived;
        _testConnectorService.CandleSeriesProcessing += OnNewCandleProcessingReceived;

        trades_dataGrid.ItemsSource = SubscribedTrades;
        candles_dataGrid.ItemsSource = SubscribedCandles;
        //Thread.Sleep(2000);
        //_testConnectorService.UnsubscribeCandles("BTCUSD");
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
}