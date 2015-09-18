using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.AliPay.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.AliPay.SellerEmail")]
        public string SellerEmail { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AliPay.Key")]
        public string Key { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AliPay.Partner")]
        public string Partner { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AliPay.ReturnUrl")]
        public string ReturnUrl { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AliPay.InvoiceSubject")]
        public string InvoiceSubject { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AliPay.InvoiceBody")]
        public string InvoiceBody { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AliPay.AdditionalFee")]
        public decimal AdditionalFee { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AliPay.EnableBankPay")]
        public bool EnableBankPay { get; set; }
    }
}