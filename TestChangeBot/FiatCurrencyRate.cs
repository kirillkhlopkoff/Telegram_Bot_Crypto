using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestChangeBot
{
    public class FiatCurrencyRate
    {
        public string Ccy { get; set; }
        public string Base_Ccy { get; set; }
        public decimal Buy { get; set; }
        public decimal Sale { get; set; }
    }
}
