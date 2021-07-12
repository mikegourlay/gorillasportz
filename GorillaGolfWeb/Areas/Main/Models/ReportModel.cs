using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GorillaGolfWeb.Areas.Main.Models
{
    public class ReportModel
    {
        public enum ReportTypes
        {
            NetScore,
            GrossScore,
            TotalSkins,
            AvgSkinsPerOuting,
            TotalHics,
            AvgHicsPerOuting,
            TotalKPs,
            AvgKPsPerOuting,
            TotalWinnings,
            TotalWinningsPerOuting,
            UnsettledWinnings,
            AveragePar3,
            AveragePar4,
            AveragePar5
        }

        public ReportTypes ReportType { get; set; }
        public List<KeyValuePair<int, string>> Data { get; set; }
        public List<string> Legend { get; set; }
        public int Season { get; set; }

        public IEnumerable<SelectListItem> ReportTypeList => new List<SelectListItem>
        {
            new SelectListItem {Text = "Average Net Score", Value = ReportTypes.NetScore.ToString(), Selected = ReportType == ReportTypes.NetScore},
            new SelectListItem {Text = "Average Gross Score", Value = ReportTypes.GrossScore.ToString(), Selected = ReportType == ReportTypes.GrossScore},
            new SelectListItem {Text = "Total Skins", Value = ReportTypes.TotalSkins.ToString(), Selected = ReportType == ReportTypes.TotalSkins},
            new SelectListItem {Text = "Average Skins/Outing", Value = ReportTypes.AvgSkinsPerOuting.ToString(), Selected = ReportType == ReportTypes.AvgSkinsPerOuting},
            new SelectListItem {Text = "Total HICS", Value = ReportTypes.TotalHics.ToString(), Selected = ReportType == ReportTypes.TotalHics},
            new SelectListItem {Text = "Average HICS/Outing", Value = ReportTypes.AvgHicsPerOuting.ToString(), Selected = ReportType == ReportTypes.AvgHicsPerOuting},
            new SelectListItem {Text = "Total KPs", Value = ReportTypes.TotalKPs.ToString(), Selected = ReportType == ReportTypes.TotalKPs},
            new SelectListItem {Text = "Average KPs/Outing", Value = ReportTypes.AvgKPsPerOuting.ToString(), Selected = ReportType == ReportTypes.AvgKPsPerOuting},
            new SelectListItem {Text = "Total Winning$", Value = ReportTypes.TotalWinnings.ToString(), Selected = ReportType == ReportTypes.TotalWinnings},
            new SelectListItem {Text = "Total Winning$/Outing", Value = ReportTypes.TotalWinningsPerOuting.ToString(), Selected = ReportType == ReportTypes.TotalWinningsPerOuting},
            new SelectListItem {Text = "Unsettled Winning$", Value = ReportTypes.UnsettledWinnings.ToString(), Selected = ReportType == ReportTypes.UnsettledWinnings},
            new SelectListItem {Text = "Average on Par 3s", Value = ReportTypes.AveragePar3.ToString(), Selected = ReportType == ReportTypes.AveragePar3},
            new SelectListItem {Text = "Average on Par 4s", Value = ReportTypes.AveragePar4.ToString(), Selected = ReportType == ReportTypes.AveragePar4},
            new SelectListItem {Text = "Average on Par 5s", Value = ReportTypes.AveragePar5.ToString(), Selected = ReportType == ReportTypes.AveragePar5}
        };
    }
}