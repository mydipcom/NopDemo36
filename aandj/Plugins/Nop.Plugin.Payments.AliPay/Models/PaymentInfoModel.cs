using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Nop.Plugin.Payments.AliPay.Models
{
    public class PaymentInfoModel : BaseNopModel
    {
        public PaymentInfoModel()
        {
            BankPayMethods = new List<BankPaymethod>();
        }

        [NopResourceDisplayName("Payment.PayMethod")]
        [AllowHtml]
        public string PayMethod { get; set; }

        [NopResourceDisplayName("Payment.DirectPayMethod")]
        public BankPaymethod DirectPayMethod { get; set; }

        [NopResourceDisplayName("Payment.BankPayMethod")]
        public IList<BankPaymethod> BankPayMethods { get; set; }

        public bool EnableBankPay { get; set; }
    }
}