using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TestTask.BLL.Interfaces;
using TestTask.BLL.Models;
using Websocket.Client;

namespace TestTask.BLL.Services
{
    public class TaskConnector_Service : ITestConnector
    {
        private WebsocketClient _client;
        public event Action<Trade> NewBuyTrade;
        public event Action<Trade> NewSellTrade;
        public event Action<Candle> CandleSeriesProcessing;

        private List<PairChanID> CandleActiveChannels = new List<PairChanID>();
        private List<PairChanID> TradeActiveChannels = new List<PairChanID>();

        public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInMin, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 100)
        {
            using(HttpClient client = new HttpClient())
            {
                long? periodStartMs = from?.ToUnixTimeMilliseconds();
                long? periodEndMs = to?.ToUnixTimeMilliseconds();

                string bitfinexCandleAPIurl = $"https://api-pub.bitfinex.com/v2/candles/trade:{GetTimeFrameByMin(periodInMin)}:t{pair}/hist?limit={count}&start={periodStartMs}&end={periodEndMs}";

                //Отправка запроса
                HttpResponseMessage response = await client.GetAsync(bitfinexCandleAPIurl);
                response.EnsureSuccessStatusCode();

                //Чтение данных ответа
                string content = await response.Content.ReadAsStringAsync();
                Debug.WriteLine(content);

                List<List<object>>? candlesData = JsonSerializer.Deserialize<List<List<object>>>(content);


                var candles = candlesData.Select(candle => new Candle
                {
                    Pair = pair,
                    OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(((JsonElement)candle[0]).GetInt64()),
                    OpenPrice = ((JsonElement)candle[1]).GetDecimal(),
                    LowPrice = ((JsonElement)candle[2]).GetDecimal(),
                    HighPrice = ((JsonElement)candle[3]).GetDecimal(),
                    ClosePrice = ((JsonElement)candle[4]).GetDecimal(),
                    TotalVolume = ((JsonElement)candle[5]).GetDecimal(),
                    TotalPrice = ((JsonElement)candle[4]).GetDecimal() * ((JsonElement)candle[5]).GetDecimal()
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

                ////Временный вариант отображения данных, присылаемых в ответе
                //foreach (var trade in trades)
                //{
                //    Debug.WriteLine($"{trade.Id} {trade.Pair} {trade.Time} {trade.Amount} {trade.Price} {trade.Side}");
                //}

                return trades;
            }
        }


        public void SubscribeCandles(string pair, int periodInMin, long? count = 0)
        {
            InitializeWebSocket();

            if (!CandleActiveChannels.Any(pc => pc.Pair == pair))
            {
                string subscribeMessage = $"{{\"event\": \"subscribe\", \"channel\": \"candles\", \"key\": \"trade:{GetTimeFrameByMin(periodInMin)}:t{pair}\"}}";
                _client.Send(subscribeMessage);
            }
        }

        public void SubscribeTrades(string pair, int maxCount = 100)
        {
            InitializeWebSocket();

            if (!TradeActiveChannels.Any(pc => pc.Pair == pair))
            {
                string subscribeMessage = $"{{\"event\": \"subscribe\", \"channel\": \"trades\", \"symbol\": \"t{pair}\"}}";
                _client.Send(subscribeMessage);
            }
        }

        public void UnsubscribeCandles(string pair)
        {
            PairChanID selectedPairChanId = CandleActiveChannels.FirstOrDefault(pc => pc.Pair == pair);
            if (selectedPairChanId != null)
            {
                string unsubscribeMessage = $"{{\"event\": \"unsubscribe\", \"chanId\": \"{selectedPairChanId.ChanId}\"}}";
                _client.Send(unsubscribeMessage);

                CandleActiveChannels.Remove(selectedPairChanId);
            }
        }

        public void UnsubscribeTrades(string pair)
        {
            PairChanID selectedPairChanId = TradeActiveChannels.FirstOrDefault(pc => pc.Pair == pair);
            if (selectedPairChanId != null)
            {
                string unsubscribeMessage = $"{{\"event\": \"unsubscribe\", \"chanId\": \"{selectedPairChanId.ChanId}\"}}";
                _client.Send(unsubscribeMessage);

                TradeActiveChannels.Remove(selectedPairChanId);
            }
        }





        //Метод для инициализации сокета, если он не был создан до этого. 
        //Он указывает то, как необходимо обрабатывать сообщение от API Bitfinex
        //в случаях свечей или трейдов
        private void InitializeWebSocket()
        {
            if (_client == null)
            {
                string wsUrl = "wss://api-pub.bitfinex.com/ws/2";
                _client = new WebsocketClient(new Uri(wsUrl));

                _client.MessageReceived.Subscribe(msg =>
                {
                    Debug.WriteLine(msg.Text);
                    var jsonDoc = JsonDocument.Parse(msg.Text);
                    var root = jsonDoc.RootElement;

                    if (root.ValueKind == JsonValueKind.Array) //Проверка на то, что прислан массив с данными трейдов, а не ответ от сервера о подписке
                    {
                        decimal chanId = root[0].GetDecimal();

                        //Проверка к какому типу канала относится ответ(Трейд или Свеча) Если найдено - относится
                        PairChanID tradeChannel = TradeActiveChannels.FirstOrDefault(pc => pc.ChanId == chanId);
                        PairChanID candleChannel = CandleActiveChannels.FirstOrDefault(pc => pc.ChanId == chanId);

                        if (tradeChannel != null)
                        {
                            if (root.GetArrayLength() == 3) //Проверка на то что это именно массив с данными трейда а не hb, потому что если hb, то в списке только id канала и пометка hb
                            {
                                var tradeInfo = root[2];
                                Trade newTrade = new Trade
                                {
                                    Id = tradeInfo[0].ToString(),
                                    Pair = tradeChannel.Pair,
                                    Time = DateTimeOffset.FromUnixTimeMilliseconds(tradeInfo[1].GetInt64()),
                                    Amount = tradeInfo[2].GetDecimal(),
                                    Price = tradeInfo[3].GetDecimal(),
                                    Side = tradeInfo[2].GetDecimal() > 0 ? "buy" : "sell",
                                };

                                if (newTrade.Side == "buy")
                                    NewBuyTrade?.Invoke(newTrade);
                                else
                                    NewSellTrade?.Invoke(newTrade);
                            }
                        }
                        else if (candleChannel != null)
                        {
                            if (root[1].ValueKind == JsonValueKind.Array) //Проверка на наличие данных о новой свече, а не hb
                            {
                                var candleData = root[1];

                                //Проверка на то что это именно массив с новой свечой, а не с историей прошлых свечей за недавнее время
                                if (candleData.GetArrayLength() == 6)
                                {
                                    Candle newCandle = new Candle
                                    {
                                        Pair = candleChannel.Pair,
                                        OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(candleData[0].GetInt64()),
                                        OpenPrice = candleData[1].GetDecimal(),
                                        LowPrice = candleData[2].GetDecimal(),
                                        HighPrice = candleData[3].GetDecimal(),
                                        ClosePrice = candleData[4].GetDecimal(),
                                        TotalVolume = candleData[5].GetDecimal(),
                                    };

                                    newCandle.TotalPrice = newCandle.ClosePrice * newCandle.TotalVolume;
                                    CandleSeriesProcessing?.Invoke(newCandle);
                                }
                            }
                        }
                    }
                    else if (root.TryGetProperty("event", out JsonElement eventElement) && eventElement.GetString() == "subscribed") //Обработка ответа о подписке
                    {
                        //Добавление нового активного канала трейда или свечи
                        string channel = root.GetProperty("channel").GetString();
                        decimal chanId = root.GetProperty("chanId").GetDecimal();

                        if (channel == "trades") //Трейд
                        {
                            string pair = root.GetProperty("symbol").GetString().TrimStart('t'); //Извлечение из ответа только валютной пары
                            TradeActiveChannels.Add(new PairChanID { Pair = pair, ChanId = chanId });
                        }
                        else if (channel == "candles") //Свеча
                        {
                            string pair = root.GetProperty("key").GetString().Split(':')[2].TrimStart('t'); //Извлечение из ответа только валютной пары
                            CandleActiveChannels.Add(new PairChanID { Pair = pair, ChanId = chanId });
                        }
                    }
                });

                _client.Start();
            }
        }

        //Метод для конвертрования длительности свечи с минут на значения, которые валидны для API
        private string GetTimeFrameByMin(int periodInMin)
        {
            return periodInMin switch
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
            };
        }

    }
}
