using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.AJPay.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.AJPay.SellerEmail")]
        public string SellerEmail { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AJPay.Key")]
        public string Key { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AJPay.Partner")]
        public string Partner { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AJPay.ReturnUrl")]
        public string ReturnUrl { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AJPay.InvoiceSubject")]
        public string InvoiceSubject { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AJPay.InvoiceBody")]
        public string InvoiceBody { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AJPay.AdditionalFee")]
        public decimal AdditionalFee { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AJPay.EnableBankPay")]
        public bool EnableBankPay { get; set; }
    }
}