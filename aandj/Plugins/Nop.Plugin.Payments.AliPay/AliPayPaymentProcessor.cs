using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Customers;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.AliPay.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Web.Framework;


namespace Nop.Plugin.Payments.AliPay
{
    /// <summary>
    /// AliPay payment processor
    /// </summary>
    public class AliPayPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly AliPayPaymentSettings _aliPayPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly IStoreContext _storeContext;

        #endregion

        #region Ctor

        public AliPayPaymentProcessor(AliPayPaymentSettings aliPayPaymentSettings,
            ISettingService settingService, IWebHelper webHelper,
            IStoreContext storeContext)
        {
            this._aliPayPaymentSettings = aliPayPaymentSettings;
            this._settingService = settingService;
            this._webHelper = webHelper;
            this._storeContext = storeContext;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets MD5 hash
        /// </summary>
        /// <param name="Input">Input</param>
        /// <param name="Input_charset">Input charset</param>
        /// <returns>Result</returns>
        public string GetMD5(string Input, string Input_charset)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] t = md5.ComputeHash(Encoding.GetEncoding(Input_charset).GetBytes(Input));
            StringBuilder sb = new StringBuilder(32);
            for (int i = 0; i < t.Length; i++)
            {
                sb.Append(t[i].ToString("x").PadLeft(2, '0'));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Bubble sort
        /// </summary>
        /// <param name="Input">Input</param>
        /// <returns>Result</returns>
        public string[] BubbleSort(string[] Input)
        {
            int i, j;
            string temp;

            bool exchange;

            for (i = 0; i < Input.Length; i++)
            {
                exchange = false;

                for (j = Input.Length - 2; j >= i; j--)
                {
                    if (System.String.CompareOrdinal(Input[j + 1], Input[j]) < 0)
                    {
                        temp = Input[j + 1];
                        Input[j + 1] = Input[j];
                        Input[j] = temp;

                        exchange = true;
                    }
                }

                if (!exchange)
                {
                    break;
                }
            }
            return Input;
        }

        /// <summary>
        /// Create URL
        /// </summary>
        /// <param name="Para">Para</param>
        /// <param name="InputCharset">Input charset</param>
        /// <param name="Key">Key</param>
        /// <returns>Result</returns>
        public string CreatUrl(string[] Para, string InputCharset, string Key)
        {
            int i;
            string[] Sortedstr = BubbleSort(Para);
            StringBuilder prestr = new StringBuilder();

            for (i = 0; i < Sortedstr.Length; i++)
            {
                if (i == Sortedstr.Length - 1)
                {
                    prestr.Append(Sortedstr[i]);

                }
                else
                {
                    prestr.Append(Sortedstr[i] + "&");
                }

            }

            prestr.Append(Key);
            string sign = GetMD5(prestr.ToString(), InputCharset);
            return sign;
        }

        /// <summary>
        /// Gets HTTP
        /// </summary>
        /// <param name="StrUrl">Url</param>
        /// <param name="Timeout">Timeout</param>
        /// <returns>Result</returns>
        public string Get_Http(string StrUrl, int Timeout)
        {
            string strResult = string.Empty;
            try
            {
                HttpWebRequest myReq = (HttpWebRequest)HttpWebRequest.Create(StrUrl);
                myReq.Timeout = Timeout;
                HttpWebResponse HttpWResp = (HttpWebResponse)myReq.GetResponse();
                Stream myStream = HttpWResp.GetResponseStream();
                StreamReader sr = new StreamReader(myStream, Encoding.Default);
                StringBuilder strBuilder = new StringBuilder();
                while (-1 != sr.Peek())
                {
                    strBuilder.Append(sr.ReadLine());
                }

                strResult = strBuilder.ToString();
            }
            catch (Exception exc)
            {
                strResult = "Error: " + exc.Message;
            }
            return strResult;
        }
        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;
            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            string service = "create_direct_pay_by_user";

            string seller_email = _aliPayPaymentSettings.SellerEmail;
            string sign_type = "MD5";
            string key = _aliPayPaymentSettings.Key;
            string partner = _aliPayPaymentSettings.Partner;
            string input_charset = "utf-8";

            string show_url = _webHelper.GetStoreLocation(false);

            string out_trade_no = postProcessPaymentRequest.Order.OrderGuid.ToString();
            string subject = string.IsNullOrEmpty(_aliPayPaymentSettings.InvoiceSubject) ? _storeContext.CurrentStore.Name : _aliPayPaymentSettings.InvoiceSubject;
            //string body = _aliPayPaymentSettings.InvoiceBody;
            string body = "";
            foreach (var item in postProcessPaymentRequest.Order.OrderItems)
            {
                if (item.Product.Name.Length > 100)
                    body += item.Product.Name.Substring(0, 100) + "...,";
                else
                    body += item.Product.Name + ",";

            }
            body = body.Substring(0, body.Length - 1);
            if (body.Length > 999)
            { 
                body = body.Substring(0, 996) + "...";
            }

            string total_fee = postProcessPaymentRequest.Order.OrderTotal.ToString("0.00", CultureInfo.InvariantCulture);

            string notify_url = _webHelper.GetStoreLocation(false) + "Plugins/PaymentAliPay/Notify";
            string return_url = _webHelper.GetStoreLocation(false) + "Plugins/PaymentAliPay/Return";

            var paymethod = "directPay"; //bankPay:银联支付； directPay:支付宝即时到账
            var defaultBank = "";

            var customInfo = postProcessPaymentRequest.Order.DeserializeCustomValues();
            if (customInfo != null && customInfo.Count > 0)
            {
                if (customInfo.ContainsKey(AliPayPaymentSystemNames.SelectedAlipayPayMethod))
                {
                    paymethod = customInfo[AliPayPaymentSystemNames.SelectedAlipayPayMethod].ToString();
                    if (!string.IsNullOrEmpty(paymethod) && paymethod != AliPayPaymentSystemNames.DirectPay)
                    {
                        //默认网银
                        defaultBank = paymethod;//如果传递的paymethod不等于directPay,则表示使用银联支付，paymethod表示银行编码

                        //默认支付方式
                        paymethod = AliPayPaymentSystemNames.BankPay;
                    }
                }
            }

            IList<string> paraList = new List<string>();
            paraList.Add("service=" + service);
            paraList.Add("partner=" + partner);
            paraList.Add("seller_email=" + seller_email);
            paraList.Add("out_trade_no=" + out_trade_no);
            paraList.Add("subject=" + subject);
            paraList.Add("body=" + body);
            paraList.Add("total_fee=" + total_fee);
            paraList.Add("show_url=" + show_url);
            paraList.Add("return_url=" + return_url);
            paraList.Add("notify_url=" + notify_url);
            paraList.Add("payment_type=1");
            paraList.Add("paymethod=" + paymethod);
            if (!string.IsNullOrEmpty(defaultBank))
            {
                paraList.Add("defaultbank=" + defaultBank);
            }
            paraList.Add("_input_charset=" + input_charset);

            string aliay_url = CreatUrl(
                paraList.ToArray(),
                input_charset,
                key
                );
            var post = new RemotePost();
            post.FormName = "alipaysubmit";
            post.Url = "https://mapi.alipay.com/gateway.do?_input_charset=utf-8";
            post.Method = "POST";

            post.Add("service", service);
            post.Add("partner", partner);
            post.Add("seller_email", seller_email);
            post.Add("out_trade_no", out_trade_no);
            post.Add("subject", subject);
            post.Add("body", body);
            post.Add("total_fee", total_fee);
            post.Add("show_url", show_url);
            post.Add("return_url", return_url);
            post.Add("notify_url", notify_url);
            post.Add("payment_type", "1");
            post.Add("paymethod", paymethod);
            if (!string.IsNullOrEmpty(defaultBank))
            {
                post.Add("defaultbank", defaultBank);
            }
            post.Add("sign", aliay_url);
            post.Add("sign_type", sign_type);

            post.Post();
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return _aliPayPaymentSettings.AdditionalFee;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //AliPay is the redirection payment method
            //It also validates whether order is also paid (after redirection) so customers will not be able to pay twice
            
            //payment status should be Pending
            if (order.PaymentStatus != PaymentStatus.Pending)
                return false;

            //let's ensure that at least 1 minute passed after order is placed
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalMinutes < 1)
                return false;

            return true;
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentAliPay";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.AliPay.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for payment info
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentAliPay";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.AliPay.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(PaymentAliPayController);
        }

        public override void Install()
        {
            //settings
            var settings = new AliPayPaymentSettings()
            {
                SellerEmail = "",
                Key = "",
                Partner= "",
                AdditionalFee = 0,
                InvoiceSubject = "Order Subject",
                //InvoiceBody = "对一笔交易的具体描述信息",
                EnableBankPay = true
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.RedirectionTip", "Select pay method, you will be redirected to AliPay site to complete the order.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.SellerEmail", "Seller email");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.SellerEmail.Hint", "Enter seller email.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.Key", "Key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.Key.Hint", "Enter key.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.Partner", "Partner");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.Partner.Hint", "Enter partner.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.ReturnUrl", "Return Url (Please leave it empty, if you didn't have custom page)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.ReturnUrl.Hint", "Return Url.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.InvoiceSubject", "Subject");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.InvoiceSubject.Hint", "Enter name of product.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.InvoiceBody", "Body");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.InvoiceBody.Hint", "Enter description of product.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.EnableBankPay", "Enable Bank Pay");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.EnableBankPay.Hint", "Enable bank pay.");
            this.AddOrUpdatePluginLocaleResource("Payment.PayMethod", "Pay Method");
            this.AddOrUpdatePluginLocaleResource("Payment.PayMethod.Hint", "Select pay method.");
            this.AddOrUpdatePluginLocaleResource("Payment.DirectPayMethod", "Alipay direct pay");
            this.AddOrUpdatePluginLocaleResource("Payment.DirectPayMethod.Hint", "Alipay direct pay.");
            this.AddOrUpdatePluginLocaleResource("Payment.BankPayMethod", "Alipay bank pay");
            this.AddOrUpdatePluginLocaleResource("Payment.BankPayMethod.Hint", "Alipay bank pay.");
            base.Install();
        }


        public override void Uninstall()
        {
            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.SellerEmail.RedirectionTip");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.SellerEmail");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.SellerEmail.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.Key");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.Key.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.Partner");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.Partner.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.ReturnUrl");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.ReturnUrl.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.InvoiceSubject");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.InvoiceSubject.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.InvoiceBody");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.InvoiceBody.Hint");

            base.Uninstall();
        }
        #endregion

        #region Properies

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.NotSupported;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Redirection;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        #endregion
    }
}
