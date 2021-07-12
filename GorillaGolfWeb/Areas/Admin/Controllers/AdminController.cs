using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using GorillaGolfCommon.core;
using GorillaGolfCommon.golf;
using GorillaGolfWeb.Areas.Admin.Models;
using GorillaGolfWeb.Filters;

namespace GorillaGolfWeb.Areas.Admin.Controllers
{
    [AdminAuthentication]
    [NoCache]
    public class AdminController : Controller
    {
        public ActionResult PlayerList()
        {
            // Get list of players
            var model = Player.GetPlayers();
            return View(model);
        }

        public ActionResult ViewPlayer(int playerid)
        {
            // Get player
            Player player = Player.GetPlayer(playerid, true);
            if (player == null) return RedirectToAction("PlayerList");

            return View("PlayerView", player);
        }

        public ActionResult RemovePlayer(int playerid)
        {
            if (Player.CanBeRemoved(playerid))
            {
                Player.DeletePlayer(playerid);
            }
            return RedirectToAction("PlayerList");
        }

        [HttpGet]
        public ActionResult EditPlayer(int playerid)
        {
            Player player = Player.GetPlayer(playerid);
            return View("EditPlayer", player);
        }

        [HttpPost]
        public ActionResult EditPlayer(Player player)
        {
            try
            {
                if (player.PlayerID == 0) Player.AddPlayer(player);
                else Player.UpdatePlayer(player);
            }
            catch (DuplicatePlayerUserNameException)
            {
                // TODO: display error
            }
            return RedirectToAction("ViewPlayer", new { playerid = player.PlayerID });
        }

        [HttpGet]
        public ActionResult CreditPlayer(int playerid)
        {
            Player player = Player.GetPlayer(playerid);
            Transaction tx = new Transaction { PlayerID = playerid };
            ViewBag.PlayerName = player.FullName;
            return View("CreditPlayer", tx);
        }

        [HttpPost]
        public ActionResult CreditPlayer(Transaction tx)
        {
            if (ModelState.IsValid)
            {
                tx.TxType = TxTypes.DEPOSIT;
                Transaction.AddTransaction(tx);
                return RedirectToAction("ViewPlayer", new { playerid = tx.PlayerID });
            }

            return View(tx);
        }

        [HttpGet]
        public ActionResult DebitPlayer(int playerid)
        {
            Player player = Player.GetPlayer(playerid, true);
            DebitModel debit = new DebitModel
            {
                PlayerID = playerid,
                PlayerName = player.FullName,
                Amount = player.Balance??0,
                Balance = player.Balance??0,
                CreditingPlayerID = 0,
                WithdrawalType = DebitModel.WithdrawalTypes.POOL
            };

            // Get list of crediting players.  It needs to have a blank entry (PlayerID = 0) and
            // cannot have the player listed that is getting a withdrawal.
            List<SelectListItem> creditingPlayers = new List<SelectListItem>();
            foreach (Player p in Player.GetPlayers(true))
            {
                if (p.PlayerID == playerid) continue;
                creditingPlayers.Add(new SelectListItem{Text = p.DisplayName, Value = p.PlayerID.ToString()});
            }
            // Insert blank entry
            creditingPlayers.Insert(0, new SelectListItem{Text = "", Value = "0"});
            debit.CreditingPlayerList = creditingPlayers;

            return View("DebitPlayer", debit);
        }

        [HttpPost]
        public ActionResult DebitPlayer(DebitModel debit)
        {
            if (ModelState.IsValid)
            {
                bool isError = false;
                // If the withdrawal type is from a player and no crediting player is selected,
                // return error.
                if (debit.WithdrawalType == DebitModel.WithdrawalTypes.PLAYER && debit.CreditingPlayerID == 0)
                {
                    ModelState.AddModelError("", "No Crediting Player Selected.");
                    isError = true;
                }

                // This should be caught at UI level but check here for the player not having enough funds
                // for specified withdrawal
                if (Math.Abs(debit.Amount) > debit.Balance)
                {
                    ModelState.AddModelError("", "Player does not have enough funds to withdrawal amount specified.");
                    isError = true;
                }

                if (isError)
                {
                    // Get list of crediting players.  It needs to have a blank entry (PlayerID = 0) and
                    // cannot have the player listed that is getting a withdrawal.
                    List<SelectListItem> creditingPlayers = new List<SelectListItem>();
                    foreach (Player p in Player.GetPlayers(true))
                    {
                        if (p.PlayerID == debit.PlayerID) continue;
                        creditingPlayers.Add(new SelectListItem { Text = p.DisplayName, Value = p.PlayerID.ToString() });
                    }
                    // Insert blank entry
                    creditingPlayers.Insert(0, new SelectListItem { Text = "", Value = "0" });
                    debit.CreditingPlayerList = creditingPlayers;

                    return View(debit);
                }

                Transaction tx = new Transaction
                {
                    PlayerID = debit.PlayerID,
                    TxType = TxTypes.WITHDRAWAL,
                    Amount = -(Math.Abs(debit.Amount)),
                    AssociatedPlayerID = debit.WithdrawalType == DebitModel.WithdrawalTypes.POOL
                        ? 0
                        : debit.CreditingPlayerID 
                };
                Transaction.AddTransaction(tx);

                // If this was a transfer (Crediting PlayerID is not zero), need to add a credit to
                // associated player
                if (debit.CreditingPlayerID != 0)
                {
                    tx = new Transaction
                    {
                        PlayerID = debit.CreditingPlayerID,
                        AssociatedPlayerID = debit.PlayerID,
                        TxType = TxTypes.DEPOSIT,
                        Amount = Math.Abs(debit.Amount)
                    };
                    Transaction.AddTransaction(tx);
                }

                return RedirectToAction("ViewPlayer", new { playerid = debit.PlayerID });
            }

            return View(debit);
        }

        public ActionResult AddPlayer()
        {
            // Create a new player
            Player player = new Player {Status = UserStatus.Active, UserType = UserTypes.Normal};
            return View("EditPlayer", player);
        }

        

        public ActionResult UpdatePlayersHIndex()
        {
            List<Player> playerList = Player.GetPlayers(true);
            Dictionary<int, string> msgs = new Dictionary<int, string>();
            Dictionary<int, string> errors = new Dictionary<int, string>();
            foreach (Player player in playerList)
            {
                if (Player.UpdateHIndex(player, out var msg)) msgs.Add(player.PlayerID, msg);
                else errors.Add(player.PlayerID, msg);
            }

            ViewBag.HIndexUpdateMsgs = msgs;
            ViewBag.HIndexUpdateErrors = errors;

            return View("PlayerList", playerList);
        }

        public ActionResult CourseList()
        {
            // Get List of Courses
            var courseList = Course.GetCourses(); 

            // Check for any custom error added by another action
            if (TempData["CustomError"] != null)
            {
                ModelState.AddModelError(String.Empty, TempData["CustomError"].ToString());
            }
            return View(courseList);
        }

       public ActionResult AddCourse()
        {
            // creating a new course so initialize the course with 18 holes
            // Set the Par initially to 0 to indicate hole not used.
            Course course = new Course { HoleList = new List<Hole>() };
            for (var i = 0; i < 18; i++) course.HoleList.Add(new Hole { HoleNumber = i + 1, Par = 0 });
            return View("EditCourse", course);
        }

        [HttpGet]
        public ActionResult EditCourse(int courseID)
        {
            Course course = Course.GetCourse(courseID);

            return View("EditCourse", course);
        }

        [HttpPost]
        public ActionResult EditCourse(Course tmpCourse)
        {
            // Make sure no two handicaps are the same
            HashSet<int> hcSet = new HashSet<int>();
            foreach (Hole h in tmpCourse.HoleList)
            {
                if (!h.Handicap.HasValue) continue;
                if (hcSet.Contains((int) h.Handicap))
                {
                    ModelState.AddModelError(String.Empty, "Duplicate hole handicaps detected.");
                    return View("EditCourse", tmpCourse);
                }
                else hcSet.Add((int)h.Handicap);
            }

            // Make sure that if any handicaps are entered that all holes
            // with a Par value entered have a handicap entered.
            if (tmpCourse.HoleList.Exists(x => x.Handicap.HasValue))
            {
                foreach (Hole h in tmpCourse.HoleList.Where(x => x.Par != 0))
                {
                    if (!h.Handicap.HasValue)
                    {
                        ModelState.AddModelError(String.Empty, "Not all holes have a handicap specified.");
                        return View("EditCourse", tmpCourse);
                    }
                }
            }
            
            try
            {
                if (tmpCourse.CourseID == 0) Course.AddCourse(tmpCourse);
                else Course.UpdateCourse(tmpCourse);
            }
            catch (Exception ex)
            {
                TempData["CustomError"] = $"Error {(tmpCourse.CourseID == 0 ? "adding" : "updating")} Course: {ex.Message}";
            }
            return RedirectToAction("ViewCourse", new {courseID = tmpCourse.CourseID});
        }

        public ActionResult ViewCourse(int courseID)
        {
            Course course = Course.GetCourse(courseID);
            if (course != null) return View("CourseView", course);
                
            return RedirectToAction("CourseList");
        }

        public ActionResult RemoveCourse(int courseid)
        {
            // First check to see if this course can be removed.
            // If there are outings referencing it then it cannot be removed.
            if (Course.CanCourseBeRemoved(courseid))
            {
                Course.DeleteCourse(courseid);
            }
            else
            {
                TempData["CustomError"] = "This course is referenced by an Outing and cannot be removed.";
            }
            return RedirectToAction("CourseList");
        }

        public ActionResult OutingList(OutingListModel olm)
        {
            // Get season
            olm.Season = olm.Season != 0
                ? olm.Season
                : SessionVariables.SeasonID.HasValue
                    ? (int) SessionVariables.SeasonID
                    : Season.CurrentSeason;

            SessionVariables.SeasonID = olm.Season;

            // Load seasons list for Season selector
            ViewBag.SeasonList = Season.Seasons;

            // Get List of Outings for season
            olm.OutingList = Outing.GetOutings(olm.Season);

            // Check for any custom error added by another action
            if (TempData["CustomError"] != null)
            {
                ModelState.AddModelError(String.Empty, TempData["CustomError"].ToString());
            }
            return View(olm);
        }

        [HttpGet]
        public ActionResult EditOuting(int outingid)
        {
            Outing outing;
            if (outingid == 0)
            {
                // This is an add
                outing = new Outing
                {
                    OutingDate = DateTime.Now,
                    HicValue = (Decimal).25,
                    SkinValue = (Decimal).25,
                    LowNetValue = (Decimal).25,
                    KPValue = 1,
                    Season = Season.CurrentSeason
                };
            }
            else
            {
                // This is an edit
                outing = Outing.GetOuting(outingid);
            }

           // Make sure there are at least 5 player entries per group for editing
            foreach (var grp in outing.GroupList)
            {
                for (var i = grp.PlayerList.Count; i < 5; i++)
                {
                    grp.PlayerList.Add(new PlayerOuting { Player = new Player() });
                }
            }
           

            // Get list of courses for the Course Selector
            ViewBag.CourseList = Course.GetCourses();

            // Put list of players in viewbag for use in dropdowns.
            InitPlayerSelections();

            return View("EditOuting", outing);
        }

        [HttpPost]
        public ActionResult EditOuting(Outing outing)
        {
            // Remove any groups that don't have any players selected.
            outing.GroupList = outing.GroupList.Where(x => x.PlayerList.Exists(y => y.Player.PlayerID != 0)).ToList();
            try
            {
                if (outing.OutingID == 0)
                {
                    Outing.AddOuting(outing);
                }
                else
                {
                    Outing.UpdateOuting(outing);

                    // If this was a committed outing, need to mark it as uncommitted
                    // and remove all transactions.
                    if (outing.Committed)
                    {
                        Outing.Uncommit(outing.OutingID);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["CustomError"] = $"Error {(outing.OutingID == 0 ? "adding" : "updating")} Outing: {ex.Message}";
            }
            return RedirectToAction("ViewOuting", new { outingid = outing.OutingID });
        }

        public ActionResult AddOuting()
        {
            var outing = new Outing
            {
                OutingDate = DateTime.Now,
                HicValue = (Decimal) .25,
                SkinValue = (Decimal) .25,
                LowNetValue = (Decimal) .25,
                KPValue = 1,
                Season = Season.CurrentSeason
            };

            // Add one group to start with
            Group grp = new Group {OutingID = outing.OutingID};

            // Add five empty players to the group to start with
            grp.PlayerList.Add(new PlayerOuting { Player = new Player() });
            grp.PlayerList.Add(new PlayerOuting { Player = new Player() });
            grp.PlayerList.Add(new PlayerOuting { Player = new Player() });
            grp.PlayerList.Add(new PlayerOuting { Player = new Player() });
            grp.PlayerList.Add(new PlayerOuting { Player = new Player() });
            outing.GroupList.Add(grp);

            // Get list of courses for the Course Selector
            ViewBag.CourseList = Course.GetCourses();

            // Put list of players in viewbag for use in dropdowns.
            InitPlayerSelections();

            return View("EditOuting", outing);
        }

       

        public ActionResult RemoveOuting(int outingid)
        {
            Outing.DeleteOuting(outingid);

            return RedirectToAction("OutingList");
        }

        public ActionResult ViewOuting(int outingid)
        {
            Outing outing = Outing.GetOuting(outingid);
            if (outing != null) return View("OutingView", outing);

            return RedirectToAction("OutingList");
        }

        [HttpGet]
        public ActionResult ScoreOuting(int outingid)
        {
            Outing outing = Outing.GetOuting(outingid);
            if (outing != null) return View("ScoreOuting", outing);

            return RedirectToAction("OutingList");
        }

        [HttpPost]
        public ActionResult ScoreOuting(Outing outing)
        {
            Outing.UpdateOutingScore(outing);

            // If this was a committed outing, need to mark it as uncommitted
            // and remove all transactions.
            if (outing.Committed)
            {
                Outing.Uncommit(outing.OutingID);
            }

            return RedirectToAction("ViewOuting", new { outingid = outing.OutingID });
        }

        public ActionResult CommitOuting(int outingid)
        {
            Outing outing = Outing.GetOuting(outingid);
            if (outing == null || outing.Committed) return RedirectToAction("OutingList");
            
            // Check outing for errors before allowing commit.
            List<string> errors = Outing.CheckOuting(outing);

            if (errors.Count == 0)
            {
                Outing.Commit(outingid);
                outing.Committed = true;
            }
            else
            {
                foreach (string err in errors) ModelState.AddModelError(String.Empty, err);
            }

            return View("OutingView", outing);
        }

        public ActionResult SettleOuting(int outingid)
        {
            Outing outing = Outing.GetOuting(outingid);
            if (outing == null || outing.Settled) return RedirectToAction("OutingList");

            Outing.Settle(outingid);
            outing.Settled = true;

            return View("OutingView", outing);
        }

        public ActionResult UnsettleOuting(int outingid)
        {
            Outing outing = Outing.GetOuting(outingid);
            if (outing == null || !outing.Settled) return RedirectToAction("OutingList");

            Outing.Unsettle(outingid);
            outing.Settled = false;

            return View("OutingView", outing);
        }

        public ActionResult AddGroup(Outing outing)
        {
            Group grp = new Group()
            {
                OutingID = outing.OutingID
            };
            outing.GroupList.Add(grp);

            // Add five empty players to the group to start with
            grp.PlayerList.Add(new PlayerOuting {Player = new Player() });
            grp.PlayerList.Add(new PlayerOuting { Player = new Player() });
            grp.PlayerList.Add(new PlayerOuting { Player = new Player() });
            grp.PlayerList.Add(new PlayerOuting { Player = new Player() });
            grp.PlayerList.Add(new PlayerOuting { Player = new Player() });

            // Get list of courses for the Course Selector
            ViewBag.CourseList = Course.GetCourses();

            // Put list of players in viewbag for use in dropdowns.
            InitPlayerSelections();

            return View("EditOuting", outing);
        }

        protected void InitPlayerSelections()
        {
            // Put list of players in viewbag for use in dropdowns.
            // Insert a blank entry for clearing out and initializing entries
            List<Player> plist = Player.GetPlayers(true);
            plist.Insert(0, new Player() { PlayerID = 0 });
            ViewBag.PlayerSelectList = plist;
        }

        public ActionResult TransactionList()
        {
            // Get list of all deposit and credit transactions
            List<Transaction> tranList = Transaction.GetCreditDepositTransactions();

            return View(tranList);
        }
        
    }
}