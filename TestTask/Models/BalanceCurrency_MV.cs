using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestTask.Models
{
    public class BalanceCurrency_MV //Класс для представления данных внутри dataGrid
    {
        public string CurrencyName { get; set; } //Имя валюты
        public decimal Count { get; set; } //Количество
    }
}
