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
    [PlayerAuthentication]
    [NoCache]
    public class PlayerController : Controller
    {

       public ActionResult OutingSummary(PlayerSummaryModel psm)
       {
           int playerID = psm.PlayerID != 0
               ? psm.PlayerID
               : SessionVariables.PlayerSummaryID.HasValue
                   ? (int) SessionVariables.PlayerSummaryID
                   : SessionVariables.PlayerID.HasValue
                       ? (int) SessionVariables.PlayerID
                       : 0;

           int seasonID = psm.Season != 0
               ? psm.Season
               : SessionVariables.SeasonID.HasValue
                   ? (int) SessionVariables.SeasonID
                   : Season.CurrentSeason;

            PlayerSummaryModel model = new PlayerSummaryModel
            {
                PlayerID = playerID,
                Outings = Outing.GetPlayerOutings(seasonID), // Published outings only
                TransactionDictionary = Transaction.GetPlayerTransactionDictionary(playerID),
                CurrentBalance = Player.GetPlayer(playerID, true).Balance??0,
                Season = seasonID
            };

           // Store selected player on session
           SessionVariables.PlayerSummaryID = playerID;

           // Store selected season on session
           SessionVariables.SeasonID = seasonID;

           // Load list of players for drop down selector
            List<Player> plist = Player.GetPlayers(true);
            ViewBag.PlayerSelectList = plist;

           // Load seasons list for Season selector
           ViewBag.SeasonList = Season.Seasons;

            return View(model);
        }

        public ActionResult SeasonTotals(ReportModel reportModel)
        {
            // Get current season and load seasons list for Season selector
            int seasonID = reportModel.Season != 0
                ? reportModel.Season
                : SessionVariables.SeasonID.HasValue
                    ? (int) SessionVariables.SeasonID
                    : Season.CurrentSeason;

            // Store selected season on session and in model
            SessionVariables.SeasonID = reportModel.Season = seasonID;

            ViewBag.SeasonList = Season.Seasons;

            switch (reportModel.ReportType)
            {
                case ReportModel.ReportTypes.GrossScore:
                    reportModel.Data = Reports.AverageGrossPerOuting(reportModel.Season);
                    reportModel.Legend = new List<string>{"Does not include Par 3 courses."};
                    break;
                case ReportModel.ReportTypes.NetScore:
                    reportModel.Data = Reports.AverageNetPerOuting(reportModel.Season);
                    reportModel.Legend = new List<string>{"Does not include Par 3 courses."};
                    break;
                case ReportModel.ReportTypes.TotalSkins:
                    reportModel.Data = Reports.TotalSkins(reportModel.Season);
                    reportModel.Legend = new List<string>{"Reported as <b><i>total</i></b> <font size='+1'>[</font><b><i>won by flip</i></b>]"};
                    break;
                case ReportModel.ReportTypes.AvgSkinsPerOuting:
                    reportModel.Data = Reports.AverageSkinsPerOuting(reportModel.Season);
                    break;
                case ReportModel.ReportTypes.TotalHics:
                    reportModel.Data = Reports.TotalHICS(reportModel.Season);
                    break;
                case ReportModel.ReportTypes.AvgHicsPerOuting:
                    reportModel.Data = Reports.AverageHICSPerOuting(reportModel.Season);
                    break;
                case ReportModel.ReportTypes.TotalKPs:
                    reportModel.Data = Reports.TotalKPs(reportModel.Season);
                    break;
                case ReportModel.ReportTypes.AvgKPsPerOuting:
                    reportModel.Data = Reports.AverageKPsPerOuting(reportModel.Season);
                    break;
                case ReportModel.ReportTypes.TotalWinnings:
                    reportModel.Data = Reports.TotalTransactions(false, reportModel.Season);
                    break;
                case ReportModel.ReportTypes.TotalWinningsPerOuting:
                    reportModel.Data = Reports.TotalTransactionsPerOuting(reportModel.Season);
                    break;
                case ReportModel.ReportTypes.UnsettledWinnings:
                    reportModel.Data = Reports.TotalTransactions(true, reportModel.Season);
                    break;
                case ReportModel.ReportTypes.AveragePar3:
                    reportModel.Data = Reports.AverageParScore(3, reportModel.Season);
                    reportModel.Legend = new List<string>{"Does not include Par 3 courses."};
                    break;
                case ReportModel.ReportTypes.AveragePar4:
                    reportModel.Data = Reports.AverageParScore(4, reportModel.Season);
                    break;
                case ReportModel.ReportTypes.AveragePar5:
                    reportModel.Data = Reports.AverageParScore(5, reportModel.Season);
                    break;
            }

            // Put player list in ViewBag for use in display
            Dictionary< int, Player> pList = Player.GetPlayersDictionary(true);
            ViewBag.Players = pList;

            // Put player outing counts for season in ViewBag for use in display
            Dictionary< int, int> countList = Player.GetPlayerOutingCounts(reportModel.Season);
            ViewBag.PlayerOutingCounts = countList;

           return View(reportModel);
        }

        public ActionResult ViewOuting(int outingid)
        {
            Outing outing = Outing.GetOuting(outingid);
            if (outing == null) return RedirectToAction("OutingSummary");

            // Get transaction summaries for this outing
            Dictionary<int, Transaction.PlayerTransactSummary> summary = Transaction.GetTransactionSummary(outingid);
            if (summary.Count == 0)
            {
                ViewBag.MoneyWinners = "";
                ViewBag.MoneyLosers = "";
            }
            else
            {
                decimal maxamount = summary.Values.Max(x => x.TotalAmount);
                List<Transaction.PlayerTransactSummary> maxmoneyList =
                    summary.Values.Where(x => x.TotalAmount == maxamount).ToList();
                decimal minamount = summary.Values.Min(x => x.TotalAmount);
                List<Transaction.PlayerTransactSummary> minmoneyList =
                    summary.Values.Where(x => x.TotalAmount == minamount).ToList();
                ViewBag.MoneyWinners = string.Join(", ",
                    maxmoneyList.Select(x => $"{x.Player.ShortName} ({x.TotalAmount.Currency()})").ToArray());
                ViewBag.MoneyLosers = string.Join(", ",
                    minmoneyList.Select(x => $"{x.Player.ShortName} ({x.TotalAmount.Currency()})").ToArray());
            }
            ViewBag.PlayerTransactionSummary = summary;

            return View("ViewOuting", outing);

            
        }
    }
}