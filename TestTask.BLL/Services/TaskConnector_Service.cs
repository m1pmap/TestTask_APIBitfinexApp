using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TestTask.BLL.Interfaces;
using TestTask.BLL.Models;

namespace TestTask.BLL.Services
{
    public class TaskConnector_Service : ITestConnector
    {
        public event Action<Trade> NewBuyTrade;
        public event Action<Trade> NewSellTrade;
        public event Action<Candle> CandleSeriesProcessing;

        public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInMin, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 100)
        {
            using(HttpClient client = new HttpClient())
            {
                string timeframe = periodInMin switch
                {
                    1 => "1m",
                    5 => "5m",
                    15 => "15m",
                    30 => "30m",
                    60 => "1h",
                    240 => "4h",
                    1440 => "1D",
                    10080 => "7D",
                    20160 => "14D",
                    43200 => "1M",
                    _ => throw new ArgumentException("Invalid period")
                };

                long? periodStartMs = from?.ToUnixTimeMilliseconds();
                long? periodEndMs = to?.ToUnixTimeMilliseconds();

                string bitfinexCandleAPIurl = $"https://api-pub.bitfinex.com/v2/candles/trade:{timeframe}:t{pair}/hist?limit={count}&start={periodStartMs}&end={periodEndMs}";

                //Отправка запроса
                HttpResponseMessage response = await client.GetAsync(bitfinexCandleAPIurl);
                response.EnsureSuccessStatusCode();

                //Чтение данных ответа
                string content = await response.Content.ReadAsStringAsync();
                Debug.WriteLine(content);

                List<List<object>>? candlesData = JsonSerializer.Deserialize<List<List<object>>>(content);

                decimal totalPrice = candlesData.Sum(candle => ((JsonElement)candle[5]).GetDecimal());

                var candles = candlesData.Select(candle => new Candle
                {
                    Pair = pair,
                    OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(((JsonElement)candle[0]).GetInt64()),
                    OpenPrice = ((JsonElement)candle[1]).GetDecimal(),
                    LowPrice = ((JsonElement)candle[2]).GetDecimal(),
                    HighPrice = ((JsonElement)candle[3]).GetDecimal(),
                    ClosePrice = ((JsonElement)candle[4]).GetDecimal(),
                    TotalVolume = ((JsonElement)candle[5]).GetDecimal(),
                    TotalPrice = totalPrice
                }).ToList();

                //Временный вариант отображения данных, присылаемых в ответе
                foreach (var candle in candles)
                {
                    Debug.WriteLine($"{candle.Pair} {candle.OpenTime} {candle.OpenPrice} {candle.LowPrice} {candle.HighPrice} {candle.ClosePrice} {candle.TotalVolume} {candle.TotalPrice}");
                } 

                return candles;
            }
        }

        public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
        {
            using (HttpClient client = new HttpClient())
            {
                //Формирование запроса
                string bitfinexAPIurl = $"https://api-pub.bitfinex.com/v2/trades/t{pair}/hist?limit={maxCount}&sort=-1";

                //Отправка
                HttpResponseMessage response = await client.GetAsync(bitfinexAPIurl);
                response.EnsureSuccessStatusCode();

                //Чтение и формирование выходных данных
                string content = await response.Content.ReadAsStringAsync();
                List<List<object>>? tradeData = JsonSerializer.Deserialize<List<List<object>>>(content);

                var trades = tradeData.Select(t => new Trade
                {
                    Id = t[0].ToString(),
                    Pair = pair,
                    Time = DateTimeOffset.FromUnixTimeMilliseconds(((JsonElement)t[1]).GetInt64()),
                    Amount = ((JsonElement)t[2]).GetDecimal(),
                    Price = ((JsonElement)t[3]).GetDecimal(),
                    Side = ((JsonElement)t[2]).GetDecimal() > 0 ? "buy" : "sell",
                }).ToList();

                //Временный вариант отображения данных, присылаемых в ответе
                Debug.WriteLine($"Start {trades.Count()}");
                foreach (var trade in trades)
                {
                    Debug.WriteLine($"{trade.Id} {trade.Pair} {trade.Time} {trade.Amount} {trade.Price} {trade.Side}");
                }

                return trades;
            }
        }


        public void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
        {
            throw new NotImplementedException();
        }

        public void SubscribeTrades(string pair, int maxCount = 100)
        {
            throw new NotImplementedException();
        }

        public void UnsubscribeCandles(string pair)
        {
            throw new NotImplementedException();
        }

        public void UnsubscribeTrades(string pair)
        {
            throw new NotImplementedException();
        }
    }
}
