using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        public Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            throw new NotImplementedException();
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
