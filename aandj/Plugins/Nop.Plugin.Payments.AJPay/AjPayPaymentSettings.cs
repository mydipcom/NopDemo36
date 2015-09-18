using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.AJPay
{
    public class AjPayPaymentSettings : ISettings
    {
        public string SellerEmail { get; set; }
        public string Key { get; set; }
        public string Partner { get; set; }
        public decimal AdditionalFee { get; set; }
        public string ReturnUrl { get; set; }
        public string InvoiceSubject { get; set; }
        public string InvoiceBody { get; set; }
        public bool EnableBankPay { get; set; }
    }

    public static class AjPayPaymentSystemNames
    {
        public static string SelectedAlipayPayMethod { get { return "SelectedAjPayMethod"; } }
        public static string DirectPay { get { return "directPay"; } }
        public static string BankPay { get { return "bankPay"; } }
    }

    public class BankPaymethod
    {
        public string Name { get; set; }
        public string BankCode { get; set; }
        public string BankLogo { get; set; }
    }
}
