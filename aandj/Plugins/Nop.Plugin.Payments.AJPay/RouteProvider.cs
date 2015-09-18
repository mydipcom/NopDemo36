using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.AJPay
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            //Notify
            routes.MapRoute("Plugin.Payments.AJPay.Notify",
                 "Plugins/PaymentAjPay/Notify",
                 new { controller = "PaymentAjPay", action = "Notify" },
                 new[] { "Nop.Plugin.Payments.AjPay.Controllers" }
            );

            //Notify
            routes.MapRoute("Plugin.Payments.AJPay.Return",
                 "Plugins/PaymentAjPay/Return",
                 new { controller = "PaymentAjPay", action = "Return" },
                 new[] { "Nop.Plugin.Payments.AJPay.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
