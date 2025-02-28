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

    private List<string> ValidPairs = new List<string> { "BTCUST", "BTCUSD", "XRPBTC", "XRPUSD", "USTUSD", "XMRBTC", "XMRUSD", "DSHUSD", "DSHBTC", "XRPUST", "XMRUST", "DSHUSD" };

    public MainWindow(ITestConnector testConnectorService)
    {
        InitializeComponent();
        _testConnectorService = testConnectorService;

        //Указание какие методы будут выполняться при активации ивента
        _testConnectorService.NewBuyTrade += OnNewTradeReceived;
        _testConnectorService.NewSellTrade += OnNewTradeReceived;
        _testConnectorService.CandleSeriesProcessing += OnNewCandleProcessingReceived;

        //Привязка данных к DataGrid
        trades_dataGrid.ItemsSource = SubscribedTrades;
        candles_dataGrid.ItemsSource = SubscribedCandles;
        balance_dataGrid.ItemsSource = BalanceCurrencies;
    }

    //Методы, срабатываемые при получении нового трейда или свечи
    private void OnNewTradeReceived(Trade trade)
    {
        App.Current.Dispatcher.Invoke(() => SubscribedTrades.Insert(0, trade));
    }

    private void OnNewCandleProcessingReceived(Candle candle)
    {
        App.Current.Dispatcher.Invoke(() => SubscribedCandles.Insert(0, candle));
    }

    //Получение и загрузка трейдов
    public async Task LoadTradesAsync()
    {
        if(!string.IsNullOrWhiteSpace(pair_data_textBox.Text) ||
            !string.IsNullOrWhiteSpace(count_data_textBox.Text))
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
        else
        {
            MessageBox.Show("Указаны не все параметры", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    //Получение и загрузка свечей
    public async Task LoadCandlesAsync()
    {
        if (!string.IsNullOrWhiteSpace(pair_data_textBox.Text) ||
            !string.IsNullOrWhiteSpace(count_data_textBox.Text) ||
            !string.IsNullOrWhiteSpace(HH_textBox.Text) ||
            !string.IsNullOrWhiteSpace(mm_textBox.Text) ||
            !string.IsNullOrWhiteSpace(ss_textBox.Text) ||
            !string.IsNullOrWhiteSpace(candleInterval_data_textBox.Text))
        {
            string pair = pair_data_textBox.Text;
            int count = Convert.ToInt32(count_data_textBox.Text);

            int hours = Convert.ToInt32(HH_textBox.Text);
            int minutes = Convert.ToInt32(mm_textBox.Text);
            int seconds = Convert.ToInt32(ss_textBox.Text);
            DateTimeOffset timeFrom = DateTimeOffset.UtcNow.AddDays(-1).AddHours(hours).AddMinutes(minutes).AddSeconds(seconds);

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
        else
        {
            MessageBox.Show("Указаны не все параметры", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

    //Подписка на свечи и трейды
    private void subscribeTrades_button_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(pair_subscribe_textBox.Text))
        {
            _testConnectorService.SubscribeTrades(pair_subscribe_textBox.Text);
        }
        else
        {
            MessageBox.Show("Указаны не все параметры", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void subscribeCandles_button_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(pair_subscribe_textBox.Text) || !string.IsNullOrWhiteSpace(candleInterval_subscribe_textBox.Text))
        {
            int interval = Convert.ToInt32(candleInterval_subscribe_textBox.Text);
            _testConnectorService.SubscribeCandles(pair_subscribe_textBox.Text, interval);
        }
        else
        {
            MessageBox.Show("Указаны не все параметры", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    //Отписка
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
        Button currentButton = (Button)sender;
        string currentCurrency = currentButton.Content.ToString();
        ConvertUserBalanceAsync(currentCurrency);
    }

    //Конвертация всего баланса кошелька
    public async void ConvertUserBalanceAsync(string currency)
    {
        try
        {
            BalanceCurrencies.Clear();
            balance_dataGrid.Columns[1].Header = $"Количество в {currency}";
            //Преобразование тикеров под те, что принимаются API
            if (currency == "USDT") { currency = "UST"; }
            else if (currency == "DASH") { currency = "DSH"; }

            await ConvertCurrency("BTC", currency, 1);
            await ConvertCurrency("XRP", currency, 15000);
            await ConvertCurrency("XMR", currency, 50);
            await ConvertCurrency("DSH", currency, 30);

            //Высчитывания полной стоимости кошелька
            decimal sumCurrencies = BalanceCurrencies.Sum(bc => bc.Count);
            BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = "ИТОГО:", Count = sumCurrencies });
        }
        catch (Exception ex)
        {
            BalanceCurrencies.Clear();
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    //Конвертация валюты
    private async Task ConvertCurrency(string from, string to, int count)
    {
        try
        {
            if (from != to) //Если конвертируется не сама в себя
            {
                if (ValidPairs.Contains(from + to)) //Проверка среди валидных пар
                {
                    Trade fromToPair = (await _testConnectorService.GetNewTradesAsync(from + to, 1)).FirstOrDefault();
                    BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = $"{count} {from}", Count = Math.Round(count * fromToPair.Price, 4) });
                }
                else //Если пара не валидна, то валюты меняются местами
                {
                    if (ValidPairs.Contains(to + from))
                    {
                        Trade toFromPair = (await _testConnectorService.GetNewTradesAsync(to + from, 1)).FirstOrDefault();
                        BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = $"{count} {from}", Count = Math.Round(count / toFromPair.Price, 4) });
                    }
                    else //Если и перестановка не помогла, то конвертируется в USD и с USD в необходимую валюту
                    {
                        if (ValidPairs.Contains(from + "USD"))
                        {
                            Trade fromUSDPair = (await _testConnectorService.GetNewTradesAsync(from + "USD", 1)).FirstOrDefault();
                            decimal countInUSD = count * fromUSDPair.Price;

                            if (ValidPairs.Contains(to + "USD"))
                            {
                                Trade toUSDPair = (await _testConnectorService.GetNewTradesAsync(to + "USD", 1)).FirstOrDefault();
                                BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = $"{count} {from}", Count = Math.Round(countInUSD / toUSDPair.Price, 4) });
                            }
                            else //Если и это не помогло, то значит такой пары нет на Bitfinex
                            {
                                BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = $"{count} {from}", Count = 0 });
                            }
                        }
                    }
                }
            }
            else //Если происходит конвертация сама в себя, то возвращается количество
            {
                BalanceCurrencies.Add(new BalanceCurrency_MV { CurrencyName = $"{count} {from}", Count = count });
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"{ex.Message}. Повторите попытку позже.");
        }
    }
}