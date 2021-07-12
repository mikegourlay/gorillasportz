using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GorillaGolfCommon.core;
using GorillaGolfCommon.golf;
using GorillaGolfWeb.Areas.Main.Models;
using GorillaGolfWeb.Filters;

namespace GorillaGolfWeb.Areas.Main.Controllers
{
    [NoCache]
    public class LoginController : Controller
    {
        private const string GCCookie = "GorillaSportzCredentials";

        [AllowAnonymous]
        [HttpGet]
        public ActionResult emrlink()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Ping()
        {
            return View();
        }

        // GET: Main/Login
        [AllowAnonymous]
        public ActionResult Login()
        {
            // Check for user credentials cookie
            HttpCookie httpCookie = Request.Cookies[GCCookie];
            string[] credentials = httpCookie?.Value.Split('|');
            if (credentials?.Length == 2 && credentials[0].Length > 0 && credentials[1].Length > 1)
            {
                return DoLogin(credentials[0], credentials[1], true);
            }
            return View("Login", new LoginModel{RememberMe = true});
        }

        public ActionResult Logout()
        {
            // Abandon session
            Session.Abandon();

            // Clear credential cookie
            ClearCredentialsCookie();

            return RedirectToAction("Login");
        }


        [HttpPost]
        public ActionResult Login(LoginModel loginModel)
        {
            return DoLogin(loginModel.UserName, loginModel.Password, loginModel.RememberMe);
        }

        protected ActionResult DoLogin(string username, string password, bool rememberMe)
        {
            // Validate login credentials
            Player loggedPlayer = Player.CheckLogin(username, password);

            if (loggedPlayer != null)
            {
                SessionVariables.PlayerID = loggedPlayer.PlayerID;
                SessionVariables.PlayerName = loggedPlayer.DisplayName;
                SessionVariables.IsAdmin = loggedPlayer.UserType == UserTypes.Admin;
                if (rememberMe) StoreCredentialCookie(username, password);
                return RedirectToAction("OutingSummary", "Player");
            }
            else
            {
                ViewBag.Message = "Incorrect Username or Password.";
                return View("Login");
            }
        }

        protected void ClearCredentialsCookie()
        {
            HttpCookie cookie = new HttpCookie(GCCookie, "") { Expires = DateTime.Now.AddYears(100) };
            Response.SetCookie(cookie);
        }

        protected void StoreCredentialCookie(string uname, string pword)
        {
            HttpCookie cookie = new HttpCookie(GCCookie, uname + "|" + pword) { Expires = DateTime.Now.AddYears(100) };
            Response.SetCookie(cookie);
        }
    }
}