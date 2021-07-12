using System.Web.Mvc;

namespace GorillaGolfWeb.Areas.Main
{
    public class MainAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Main";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Player_default",
                "Player/{action}/{id}",
                new { controller="Player", action = "OutingSummary", id = UrlParameter.Optional }
            );
        }
    }
}