using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.AliPay.Models;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.AliPay.Controllers
{
    public class PaymentAliPayController : BasePaymentController
    {
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly AliPayPaymentSettings _aliPayPaymentSettings;
        private readonly PaymentSettings _paymentSettings;

        public PaymentAliPayController(ISettingService settingService, 
            IPaymentService paymentService, IOrderService orderService, 
            IOrderProcessingService orderProcessingService, 
            ILogger logger, IWebHelper webHelper,
            AliPayPaymentSettings aliPayPaymentSettings,
            PaymentSettings paymentSettings)
        {
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._logger = logger;
            this._webHelper = webHelper;
            this._aliPayPaymentSettings = aliPayPaymentSettings;
            this._paymentSettings = paymentSettings;
        }
        
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new ConfigurationModel();
            model.SellerEmail = _aliPayPaymentSettings.SellerEmail;
            model.Key = _aliPayPaymentSettings.Key;
            model.Partner = _aliPayPaymentSettings.Partner;
            model.AdditionalFee = _aliPayPaymentSettings.AdditionalFee;
            model.ReturnUrl = _aliPayPaymentSettings.ReturnUrl;
            model.InvoiceSubject = _aliPayPaymentSettings.InvoiceSubject;
            model.InvoiceBody = _aliPayPaymentSettings.InvoiceBody;
            model.EnableBankPay = _aliPayPaymentSettings.EnableBankPay;

            return View("~/Plugins/Payments.AliPay/Views/PaymentAliPay/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //save settings
            _aliPayPaymentSettings.SellerEmail = model.SellerEmail;
            _aliPayPaymentSettings.Key = model.Key;
            _aliPayPaymentSettings.Partner = model.Partner;
            _aliPayPaymentSettings.AdditionalFee = model.AdditionalFee;
            _aliPayPaymentSettings.ReturnUrl = model.ReturnUrl;
            _aliPayPaymentSettings.InvoiceBody = model.InvoiceBody;
            _aliPayPaymentSettings.InvoiceSubject = model.InvoiceSubject;
            _aliPayPaymentSettings.EnableBankPay = model.EnableBankPay;

            _settingService.SaveSetting(_aliPayPaymentSettings);
            
            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();

            model.EnableBankPay = _aliPayPaymentSettings.EnableBankPay;
            model.PayMethod = AliPayPaymentSystemNames.DirectPay;

            #region 支付宝及时到账支付和银联支付
            model.DirectPayMethod = new BankPaymethod()
            {
                Name = "支付宝",
                BankCode = AliPayPaymentSystemNames.DirectPay,
                BankLogo = "alipay"
            };

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "工商银行",
                BankCode = "ICBCB2C",
                BankLogo = "ICBC"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "招商银行",
                BankCode = "CMB",
                BankLogo = "CMB"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "建设银行",
                BankCode = "CCB",
                BankLogo = "CCB"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "中国银行",
                BankCode = "BOCB2C",
                BankLogo = "BOC"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "农业银行",
                BankCode = "ABC",
                BankLogo = "ABC"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "交通银行",
                BankCode = "COMM",
                BankLogo = "COMM"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "邮政储蓄银行",
                BankCode = "PSBC-DEBIT",
                BankLogo = "PSBC"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "光大银行",
                BankCode = "CEBBANK",
                BankLogo = "CEB"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "浦发银行",
                BankCode = "SPDB",
                BankLogo = "SPDB"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "广东发展银行",
                BankCode = "GDB",
                BankLogo = "GDB"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "中信银行",
                BankCode = "CITIC",
                BankLogo = "CITIC"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "兴业银行",
                BankCode = "CIB",
                BankLogo = "CIB"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "深圳发展银行",
                BankCode = "SDB",
                BankLogo = "SDB"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "民生银行",
                BankCode = "CMBC",
                BankLogo = "CMBC"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "北京银行",
                BankCode = "BJBANK",
                BankLogo = "BJBANK"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "杭州银行",
                BankCode = "HZCBB2C",
                BankLogo = "HZCB"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "上海银行",
                BankCode = "SHBANK",
                BankLogo = "SHBANK"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "北京农村商业银行",
                BankCode = "BJRCB",
                BankLogo = "BJRCB"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "平安银行",
                BankCode = "SPABANK",
                BankLogo = "SPABANK"
            });

            model.BankPayMethods.Add(new BankPaymethod
            {
                Name = "宁波银行",
                BankCode = "NBBANK",
                BankLogo = "NBBANK"
            });

            #endregion

            return View("~/Plugins/Payments.AliPay/Views/PaymentAliPay/PaymentInfo.cshtml", model);
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
            if (_aliPayPaymentSettings.EnableBankPay)
            {
                paymentInfo.CustomValues.Add(AliPayPaymentSystemNames.SelectedAlipayPayMethod, form["PayMethod"]);
            }
            return paymentInfo;
        }

        [ValidateInput(false)]
        public ActionResult Notify(FormCollection form)
        {
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.AliPay") as AliPayPaymentProcessor;
            if (processor == null ||
                !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("AliPay module cannot be loaded");


            string alipayNotifyUrl = "https://mapi.alipay.com/gateway.do?service=notify_verify";

            string partner = _aliPayPaymentSettings.Partner;
            if (string.IsNullOrEmpty(partner))
                throw new Exception("Partner is not set");
            string key = _aliPayPaymentSettings.Key;
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

        [ValidateInput(false)]
        public ActionResult Return()
        {
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.AliPay") as AliPayPaymentProcessor;
            if (processor == null ||
                !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("AliPay module cannot be loaded");

            string partner = _aliPayPaymentSettings.Partner;
            if (string.IsNullOrEmpty(partner))
                throw new Exception("Partner is not set");
            string key = _aliPayPaymentSettings.Key;
            if (string.IsNullOrEmpty(key))
                throw new Exception("Partner is not set");
            string _input_charset = "utf-8";
            string alipayNotifyUrl = "https://mapi.alipay.com/gateway.do?service=notify_verify";

            NameValueCollection coll;
            int i;
            //Load Form variables into NameValueCollection variable.
            coll = Request.QueryString;

            if (coll.Count > 0)
            {
                alipayNotifyUrl = alipayNotifyUrl + "&partner=" + partner + "&notify_id=" + Request.QueryString["notify_id"];
                string responseTxt = processor.Get_Http(alipayNotifyUrl, 120000);

                // Get names of all forms into a string array.
                String[] requestarr = coll.AllKeys;
                string[] Sortedstr = processor.BubbleSort(requestarr);

                var prestr = new StringBuilder();
                for (i = 0; i < Sortedstr.Length; i++)
                {
                    if (Request.QueryString[Sortedstr[i]] != "" && Sortedstr[i] != "sign" && Sortedstr[i] != "sign_type")
                    {
                        if (i == Sortedstr.Length - 1)
                        {
                            prestr.Append(Sortedstr[i] + "=" + Request.QueryString[Sortedstr[i]]);
                        }
                        else
                        {
                            prestr.Append(Sortedstr[i] + "=" + Request.QueryString[Sortedstr[i]] + "&");

                        }
                    }
                }

                prestr.Append(key);

                string mysign = processor.GetMD5(prestr.ToString(), _input_charset);

                string sign = Request.QueryString["sign"];

                if (mysign == sign && responseTxt == "true")
                {
                    if (Request.QueryString["trade_status"] == "WAIT_BUYER_PAY")
                    {
                        string strOrderNo = Request.QueryString["out_trade_no"];
                        string strPrice = Request.QueryString["total_fee"];
                    }
                    else if (Request.QueryString["trade_status"] == "TRADE_FINISHED" || Request.QueryString["trade_status"] == "TRADE_SUCCESS")
                    {
                        string strOrderNo = Request.QueryString["out_trade_no"];
                        string strPrice = Request.QueryString["total_fee"];

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
                }
                else
                {
                    Response.Write("fail");
                    string logStr = "MD5:mysign=" + mysign + ",sign=" + sign + ",responseTxt=" + responseTxt;
                    _logger.Error(logStr);
                }
            }

            if (!string.IsNullOrWhiteSpace(_aliPayPaymentSettings.ReturnUrl))
                return Redirect(_aliPayPaymentSettings.ReturnUrl);
            return RedirectToRoute("CheckoutCompleted", new { area = "" });
        }
    }
}