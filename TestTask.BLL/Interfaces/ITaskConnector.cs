using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestTask.BLL.Models;

namespace TestTask.BLL.Interfaces
{
    public interface ITestConnector
    {
        #region Rest

        Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount);

        //Изменил имя входного параметра для указания длительности свечи с periodInSec на periodOnMin, так как минимальное принимаемое значение API bitfinex - 1m
        Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInMin, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 100);

        #endregion

        #region Socket


        event Action<Trade> NewBuyTrade;
        event Action<Trade> NewSellTrade;
        void SubscribeTrades(string pair, int maxCount = 100);
        void UnsubscribeTrades(string pair);

        event Action<Candle> CandleSeriesProcessing;

        //Удалил следующие входные параметры:
        //from - API bitfinex не принимает начала временного интервала, так как возвращает новое, только что появившееся, значение свечи 
        //to - API bitfinex не принимает конец временного интервала и возвращает только новое значение свечи
        //Изменил имя входного параметра для указания длительности свечи с periodInSec на periodOnMin, так как минимальное принимаемое значение API bitfinex - 1m
        void SubscribeCandles(string pair, int periodInMin, long? count = 0);
        void UnsubscribeCandles(string pair);

        #endregion

    }
}
