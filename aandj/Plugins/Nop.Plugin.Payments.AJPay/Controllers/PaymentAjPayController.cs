using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.AJPay.Models;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.AJPay.Controllers
{
    public class PaymentAjPayController : BasePaymentController
    {
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly AjPayPaymentSettings _ajPayPaymentSettings;
        private readonly PaymentSettings _paymentSettings;

        public PaymentAjPayController(ISettingService settingService, 
            IPaymentService paymentService, IOrderService orderService, 
            IOrderProcessingService orderProcessingService, 
            ILogger logger, IWebHelper webHelper,
            AjPayPaymentSettings ajPayPaymentSettings,
            PaymentSettings paymentSettings)
        {
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._logger = logger;
            this._webHelper = webHelper;
            this._ajPayPaymentSettings = ajPayPaymentSettings;
            this._paymentSettings = paymentSettings;
        }
        
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new ConfigurationModel();
            model.SellerEmail = _ajPayPaymentSettings.SellerEmail;
            model.Key = _ajPayPaymentSettings.Key;
            model.Partner = _ajPayPaymentSettings.Partner;
            model.AdditionalFee = _ajPayPaymentSettings.AdditionalFee;
            model.ReturnUrl = _ajPayPaymentSettings.ReturnUrl;
            model.InvoiceSubject = _ajPayPaymentSettings.InvoiceSubject;
            model.InvoiceBody = _ajPayPaymentSettings.InvoiceBody;
            model.EnableBankPay = _ajPayPaymentSettings.EnableBankPay;

            return View("~/Plugins/Payments.AJPay/Views/PaymentAjPay/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //save settings
            _ajPayPaymentSettings.SellerEmail = model.SellerEmail;
            _ajPayPaymentSettings.Key = model.Key;
            _ajPayPaymentSettings.Partner = model.Partner;
            _ajPayPaymentSettings.AdditionalFee = model.AdditionalFee;
            _ajPayPaymentSettings.ReturnUrl = model.ReturnUrl;
            _ajPayPaymentSettings.InvoiceBody = model.InvoiceBody;
            _ajPayPaymentSettings.InvoiceSubject = model.InvoiceSubject;
            _ajPayPaymentSettings.EnableBankPay = model.EnableBankPay;

            _settingService.SaveSetting(_ajPayPaymentSettings);
            
            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();

            model.EnableBankPay = _ajPayPaymentSettings.EnableBankPay;
            model.PayMethod = AjPayPaymentSystemNames.DirectPay;

            #region 支付宝及时到账支付和银联支付
            model.DirectPayMethod = new BankPaymethod()
            {
                Name = "A & J Payment",
                BankCode = AjPayPaymentSystemNames.DirectPay,
                BankLogo = "alipay"
            };

            
            #endregion

            return View("~/Plugins/Payments.AJPay/Views/PaymentAjPay/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            if (_ajPayPaymentSettings.EnableBankPay)
            {
                paymentInfo.CustomValues.Add(AjPayPaymentSystemNames.SelectedAlipayPayMethod, form["PayMethod"]);
            }
            return paymentInfo;
        }

        [ValidateInput(false)]
        public ActionResult Notify(FormCollection form)
        {
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.AJPay") as AjPaymentProcessor;
            if (processor == null ||
                !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("AJPay module cannot be loaded");


            string alipayNotifyUrl = "https://mapi.alipay.com/gateway.do?service=notify_verify";

            string partner = _ajPayPaymentSettings.Partner;
            if (string.IsNullOrEmpty(partner))
                throw new Exception("Partner is not set");
            string key = _ajPayPaymentSettings.Key;
            if (string.IsNullOrEmpty(key))
                throw new Exception("Partner is not set");
            string _input_charset = "utf-8";

            alipayNotifyUrl = alipayNotifyUrl + "&partner=" + partner + "&notify_id=" + Request.Form["notify_id"];
            string responseTxt = processor.Get_Http(alipayNotifyUrl, 120000);

            int i;
            NameValueCollection coll;
            coll = Request.Form;
            String[] requestarr = coll.AllKeys;
            string[] Sortedstr = processor.BubbleSort(requestarr);

            var prestr = new StringBuilder();
            for (i = 0; i < Sortedstr.Length; i++)
            {
                if (Request.Form[Sortedstr[i]] != "" && Sortedstr[i] != "sign" && Sortedstr[i] != "sign_type")
                {
                    if (i == Sortedstr.Length - 1)
                    {
                        prestr.Append(Sortedstr[i] + "=" + Request.Form[Sortedstr[i]]);
                    }
                    else
                    {
                        prestr.Append(Sortedstr[i] + "=" + Request.Form[Sortedstr[i]] + "&");

                    }
                }
            }

            prestr.Append(key);

            string mysign = processor.GetMD5(prestr.ToString(), _input_charset);

            string sign = Request.Form["sign"];

            if (mysign == sign && responseTxt == "true")
            {
                if (Request.Form["trade_status"] == "WAIT_BUYER_PAY")
                {
                    string strOrderNo = Request.Form["out_trade_no"];
                    string strPrice = Request.Form["total_fee"];
                }
                else if (Request.Form["trade_status"] == "TRADE_FINISHED" || Request.Form["trade_status"] == "TRADE_SUCCESS")
                {
                    string strOrderNo = Request.Form["out_trade_no"];
                    string strPrice = Request.Form["total_fee"];

                    Guid orderNumberGuid = Guid.Empty;
                    try
                    {
                        orderNumberGuid = new Guid(strOrderNo);
                        var order = _orderService.GetOrderByGuid(orderNumberGuid);
                        if (order != null && _orderProcessingService.CanMarkOrderAsPaid(order))
                        {
                            _orderProcessingService.MarkOrderAsPaid(order);
                        }
                    }
                    catch { }
                }
                else
                {
                }

                Response.Write("success");
            }
            else
            {
                Response.Write("fail");
                string logStr = "MD5:mysign=" + mysign + ",sign=" + sign + ",responseTxt=" + responseTxt;
                _logger.Error(logStr);
            }

            return Content("");
        }

        /// <summary>
        /// 获取网关服务器GET过来通知消息，并以“参数名=参数值”的形式组成数组
        /// </summary>
        /// <returns>request回来的信息组成的数组</returns>
        public SortedDictionary<string, string> GetRequestGet()
        {
            int i = 0;
            SortedDictionary<string, string> sArray = new SortedDictionary<string, string>(); 
            // Get names of all forms into a string array.
            String[] requestItem = Request.Form.AllKeys; ;

            for (i = 0; i < requestItem.Length; i++)
            {
                sArray.Add(requestItem[i], Request.QueryString[requestItem[i]]);
            } 
            return sArray;
        }

        [ValidateInput(false)]
        public ActionResult Return()
        {
            SortedDictionary<string, string> sPara = GetRequestGet();

            string key = "h0iy0osxkkaplpxkpscc52x7o2dr0597";
            string[] keys = Request.Form.AllKeys;
            var amount = "";
            var base64_memo = "";
            var partner = "";
            var pay_no = ""; 
            var out_trade_no = ""; 
            var pay_result = ""; 
            var pay_time = "";
            var sett_date = "";
            var sett_time = "";
            var sign_type = "";
            var trade_status = "";
            var sign = ""; 
           
            for (int il = 0; il < keys.Length; il++)
            {
                if (keys[il] == "amount")
                {
                    amount = Request.Form[keys[il]];
                }
                if (keys[il] == "base64_memo")
                {
                    base64_memo = Request.Form[keys[il]];
                }
                if (keys[il] == "out_trade_no")
                {
                    out_trade_no = Request.Form[keys[il]];
                }
                if (keys[il] == "partner")
                {
                    partner = Request.Form[keys[il]];
                }
                if (keys[il] == "pay_no")
                {
                    pay_no = Request.Form[keys[il]];
                }
                if (keys[il] == "pay_result")
                {
                    pay_result = Request.Form[keys[il]];
                }
                if (keys[il] == "pay_time")
                {
                    pay_time = Request.Form[keys[il]];
                }
                if (keys[il] == "sett_date")
                {
                    sett_date = Request.Form[keys[il]];
                }
                if (keys[il] == "sett_time")
                {
                    sett_time = Request.Form[keys[il]];
                } 
                if (keys[il] == "sign_type")
                {
                    sign_type = Request.Form[keys[il]];
                } 

                if (keys[il] == "sign")
                {
                    sign = Request.Form[keys[il]];
                }
            }

            IList<string> paraList = new List<string>();
            paraList.Add("amount=" + amount);
            paraList.Add("base64_memo=" + base64_memo);
            paraList.Add("out_trade_no=" + out_trade_no);
            paraList.Add("partner=" + partner);
            paraList.Add("pay_no=" + pay_no);
            paraList.Add("pay_result=" + pay_result);
            paraList.Add("pay_time=" + pay_time);
            paraList.Add("sett_date=" + sett_date);
            paraList.Add("sett_time=" + sett_time);
            paraList.Add("sign_type=" + sign_type);

            string signed = CreatUrl(
                paraList.ToArray(),
                key
                );

            if (pay_result == "1" && signed == sign)
            {
                string strOrderNo = out_trade_no; 
                try
                {
                    var order = _orderService.GetOrderById(int.Parse(strOrderNo));
                    if (order != null && _orderProcessingService.CanMarkOrderAsPaid(order))
                    {
                        _orderProcessingService.MarkOrderAsPaid(order);
                    }
                }
                catch { }
            }

            //if (!string.IsNullOrWhiteSpace(_ajPayPaymentSettings.ReturnUrl))
            //    return Redirect(_ajPayPaymentSettings.ReturnUrl);
            return RedirectToRoute("CheckoutCompleted",  new { orderId = out_trade_no});
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

            prestr.Append("&key=" + Key);
            string sign = SHA256Encrypt(prestr.ToString());
            return sign;
        }

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

    }


}