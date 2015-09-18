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
using Nop.Plugin.Payments.AJPay.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Web.Framework;


namespace Nop.Plugin.Payments.AJPay
{
    /// <summary>
    /// AJPay payment processor
    /// </summary>
    public class AjPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly AjPayPaymentSettings _aliPayPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly IStoreContext _storeContext;

        #endregion

        #region Ctor

        public AjPaymentProcessor(AjPayPaymentSettings aliPayPaymentSettings,
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
        /// SHA256加密，不可逆转
        /// </summary>
        /// <param name="str">string str:被加密的字符串</param>
        /// <returns>返回加密后的字符串</returns>
        public string SHA256Encrypt(string str)
        {
            //System.Security.Cryptography.SHA256 s256 = new System.Security.Cryptography.SHA256Managed();
            //byte[] byte1;
            //byte1 = s256.ComputeHash(Encoding.UTF8.GetBytes(str));
            //s256.Clear();
            //return Convert.ToBase64String(byte1);

            byte[] bytes = Encoding.UTF8.GetBytes(str);
            HashAlgorithm algorithm = null;
            algorithm = new SHA256Managed();
            return BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLower();
        }

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
        /// <param name="Key">Key</param>
        /// <returns>Result</returns>
        public string CreatUrl(string[] Para, string Key)
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

            prestr.Append("&key="+Key);
            string sign = SHA256Encrypt(prestr.ToString());
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
            //支付网关支付请求示例
            //商户订单号，此处用系统时间加3位随机数作为订单号，商户应根据自己情况调整，确保该订单号在商户系统中的全局唯一
            string num = DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(999);
            string out_trade_no = postProcessPaymentRequest.Order.Id.ToString(); //这是采用随机数作为订单号样例
            //支付金额
            string total_fee = postProcessPaymentRequest.Order.OrderTotal.ToString("0.00", CultureInfo.InvariantCulture);
            //货币代码，人民币：RMB；港币：HKD；美元：USD （非人民币收单业务，需要与业务人员联系开通）
            string currency_type = "RMB";
            //创建订单的客户端IP（消费者电脑公网IP）
            //string order_create_ip = Request.UserHostAddress;  //创建订单的客户端IP（消费者电脑公网IP，用于防钓鱼支付）
            //string order_create_ip = "";
            //签名类型
            string sign_type = "SHA256";
            //交易完成后跳转的URL，用来接收网关的页面转跳即时通知结果
            string return_url = _webHelper.GetStoreLocation(false) + "Plugins/PaymentAjPay/Return";
            string notify_url = _webHelper.GetStoreLocation(false) + "Plugins/PaymentAjPay/Notify";
            ////直连银行参数（请参见文档）
            //string pay_id = "gonghang";
            //订单备注，该信息使用64位编码提交服务器，并将在支付完成后随支付结果原样返回
            var sbItemName = new StringBuilder();
            foreach (var orderItem in postProcessPaymentRequest.Order.OrderItems)
            {
                sbItemName.Append(orderItem.Product.Name + " ");
            }

            string memo = sbItemName.ToString();
            string store_oi_type = "0";
            byte[] bytes = System.Text.Encoding.Default.GetBytes(memo);
            string base64_memo = Convert.ToBase64String(bytes); 
            //=======================请求参数结束=========================== 

            string key = "h0iy0osxkkaplpxkpscc52x7o2dr0597";
            string partner = "TEST"; 

            IList<string> paraList = new List<string>();
            
            paraList.Add("out_trade_no=" + out_trade_no);
            paraList.Add("total_fee=" + total_fee);
            paraList.Add("return_url=" + return_url);
            paraList.Add("currency_type=" + currency_type);
            //paraList.Add("order_create_ip=" + order_create_ip);
            paraList.Add("sign_type=" + sign_type);
            
            //可选参数
            //paraList.Add("pay_id=" + pay_id);	        		//直连银行参数，例子是直接转跳到招商银行时的参数
            paraList.Add("base64_memo=" + base64_memo);		//订单备注的BASE64编码
            paraList.Add("store_oi_type=" + store_oi_type);		//订单备注的BASE64编码

            paraList.Add("partner=" + partner);
          
            string aandj_url = CreatUrl(
                paraList.ToArray(), 
                key
                );
            var post = new RemotePost();
            post.FormName = "aandjsubmit";
            post.Url = "http://testgateway.ajmcl.com/paygateway/payment";
            post.Method = "GET";

            post.Add("out_trade_no", out_trade_no);
            post.Add("total_fee", total_fee);
            post.Add("return_url", return_url);
            post.Add("currency_type", currency_type);
            //post.Add("order_create_ip", order_create_ip);
            post.Add("sign_type", sign_type);

            //可选参数
            //post.Add("pay_id", pay_id);	        		//直连银行参数，例子是直接转跳到招商银行时的参数
            post.Add("base64_memo", base64_memo);		//订单备注的BASE64编码
            post.Add("store_oi_type", store_oi_type);		//订单备注的BASE64编码

            post.Add("partner" , partner); 

            post.Add("sign", aandj_url);
           
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

            //AJPay is the redirection payment method
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
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.AJPay.Controllers" }, { "area", null } };
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
            controllerName = "PaymentAjPay";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.AJPay.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(PaymentAjPayController);
        }

        public override void Install()
        {
            //settings
            var settings = new AjPayPaymentSettings()
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
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AJPay.RedirectionTip", "Select pay method, you will be redirected to AJPay site to complete the order.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AJPay.SellerEmail", "Seller email");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AJPay.SellerEmail.Hint", "Enter seller email.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AJPay.Key", "Key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AJPay.Key.Hint", "Enter key.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AJPay.Partner", "Partner");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AJPay.Partner.Hint", "Enter partner.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AJPay.ReturnUrl", "Return Url (Please leave it empty, if you didn't have custom page)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AJPay.ReturnUrl.Hint", "Return Url.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AJPay.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AJPay.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AJPay.InvoiceSubject", "Subject");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AJPay.InvoiceSubject.Hint", "Enter name of product.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AJPay.InvoiceBody", "Body");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AJPay.InvoiceBody.Hint", "Enter description of product.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AJPay.EnableBankPay", "Enable Bank Pay");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AJPay.EnableBankPay.Hint", "Enable bank pay.");
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
            this.DeletePluginLocaleResource("Plugins.Payments.AJPay.SellerEmail.RedirectionTip");
            this.DeletePluginLocaleResource("Plugins.Payments.AJPay.SellerEmail");
            this.DeletePluginLocaleResource("Plugins.Payments.AJPay.SellerEmail.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.AJPay.Key");
            this.DeletePluginLocaleResource("Plugins.Payments.AJPay.Key.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.AJPay.Partner");
            this.DeletePluginLocaleResource("Plugins.Payments.AJPay.Partner.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.AJPay.ReturnUrl");
            this.DeletePluginLocaleResource("Plugins.Payments.AJPay.ReturnUrl.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.AJPay.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.AJPay.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.AJPay.InvoiceSubject");
            this.DeletePluginLocaleResource("Plugins.Payments.AJPay.InvoiceSubject.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.AJPay.InvoiceBody");
            this.DeletePluginLocaleResource("Plugins.Payments.AJPay.InvoiceBody.Hint");

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
