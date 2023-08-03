using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TestChangeBot.TelegramBotHandler;

namespace TestChangeBot
{
    public class OperationState
    {
        public string SelectedBaseCurrency { get; set; }
        public string SelectedTargetCurrency { get; set; }
        public string OperationId { get; set; }
        public OperationStep CurrentStep { get; set; }
        public string Amount { get; set; } // Добавляем новое свойство Amount
        public string Wallet { get; set; } // Добавляем новое свойство Wallet
        public string OrderAmount { get; set; }
    }
}
