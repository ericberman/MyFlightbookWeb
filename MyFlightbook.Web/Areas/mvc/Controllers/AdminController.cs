using MyFlightbook.Achievements;
using MyFlightbook.Charting;
using MyFlightbook.Histogram;
using MyFlightbook.Image;
using MyFlightbook.Instruction;
using MyFlightbook.Subscriptions;
using MyFlightbook.Web.Admin;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;


/******************************************************
 * 
 * Copyright (c) 2023-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class AdminController : AdminControllerBase
    {
        #region Admin Web Services
        #region Misc - Props
        [HttpPost]
        [Authorize]
        public ActionResult InvalidProps()
        {
            return SafeOp(ProfileRoles.maskCanSupport, () =>
            {
                ViewBag.emptyProps = CustomFlightProperty.ADMINEmptyProps();
                return PartialView("_miscInvalidProps");
            });
        }

        [HttpPost]
        [Authorize]
        public string DeleteEmptyProp(int propid)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                CustomFlightProperty cfp = new CustomFlightProperty() { PropID = propid };
                cfp.DeleteProperty();
                return string.Empty;
            });
        }
        #endregion

        #region Misc - Signatures
        private const string szSessKeyInvalidSigProgress = "sessSignedflightsAutoFixed";

        [HttpPost]
        [Authorize]
        public ActionResult UpdateInvalidSigs()
        {
            return SafeOp(ProfileRoles.maskCanSupport, () => {
                if (Session[szSessKeyInvalidSigProgress] == null)
                    Session[szSessKeyInvalidSigProgress] = new { offset = 0, lstToFix = new List<LogbookEntryBase>(), lstAutoFix = new List<LogbookEntryBase>(), progress = string.Empty, additionalFlights = 0 };

                dynamic state = Session[szSessKeyInvalidSigProgress];

                int cFlights = LogbookEntryBase.AdminGetProblemSignedFlights(state.offset, state.lstToFix, state.lstAutoFix);
                dynamic newState = new
                {
                    additionalFlights = cFlights,
                    offset = state.offset + cFlights,
                    lstToFix = state.lstToFix,
                    lstAutoFix = state.lstAutoFix,
                    progress = String.Format(CultureInfo.CurrentCulture, "Found {0} signed flights, {1} appear to have problems, {2} were autofixed (capitalization or leading/trailing whitespace)", state.offset, state.lstToFix.Count, state.lstAutoFix.Count)
                };
                Session[szSessKeyInvalidSigProgress] = newState;
                return (ActionResult) Json(newState);
            });
        }

        [HttpPost]
        [Authorize]
        public ActionResult InvalidSigsResult()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () => {
                dynamic state = Session[szSessKeyInvalidSigProgress];
                Session[szSessKeyInvalidSigProgress] = null;
                ViewBag.lstToFix = state.lstToFix;
                ViewBag.lstAutoFix = state.lstAutoFix;
                ViewBag.progress = String.Format(CultureInfo.CurrentCulture, "Found {0} signed flights, {1} appear to have problems, {2} were autofixed (capitalization or leading/trailing whitespace)", state.offset, state.lstToFix.Count, state.lstAutoFix.Count);
                return PartialView("_invalidSigs");
            });
        }
        #endregion

        #region Misc - Nightly run
        private static void StartNightlyRun(int dbg = 0, string userRestriction = null, string tasksToRun = null)
        {
            // request came from this machine - make a request to ourselves and send it out in email
            EmailSubscriptionManager em = new EmailSubscriptionManager() { ActiveBrand = Branding.CurrentBrand };
            if (dbg != 0 && !String.IsNullOrEmpty(userRestriction))
                em.UserRestriction = userRestriction;
            if (!String.IsNullOrEmpty(tasksToRun))
                em.TasksToRun = Enum.TryParse(tasksToRun, out EmailSubscriptionManager.SelectedTasks tasks) ?tasks : EmailSubscriptionManager.SelectedTasks.All;
            new Thread(new ThreadStart(em.NightlyRun)).Start();
        }

        [HttpPost]
        [Authorize]
        public string KickOffNightlyRun()
        {
            return SafeOp(ProfileRoles.maskSiteAdminOnly, () =>
            {
                StartNightlyRun();
                return string.Empty;
            });
        }

        /// <summary>
        /// Kicks off a nightly email run MUST BE CALLED FROM LOCAL MACHINE
        /// </summary>
        /// <param name="dbg">If non-zero, then limits to the specified user</param>
        /// <param name="tasks">If provided, limits to the specified task (e.g., just cloud backup, for example)</param>
        /// <returns>Status</returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        [HttpGet]
        public ActionResult NightlyRun(int dbg = 0, string tasks = null)
        {
            if (!IsLocalCall())
                throw new UnauthorizedAccessException("NightlyRun can ONLY be called from the local machine.  Otherwise, use the admin tool");

            StartNightlyRun(dbg, User.Identity.IsAuthenticated ? User.Identity.Name : null, tasks);
            return Content("Success - nightly run has been started");
        }
        #endregion

        #region Misc - Cache
        [HttpPost]
        [Authorize]
        public string FlushCache()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                return String.Format(CultureInfo.CurrentCulture, "Cache flushed, {0:#,##0} items removed.", util.FlushCache());
            });
        }
        #endregion

        #region Property management
        [HttpPost]
        [Authorize]
        public ActionResult UpdateProperty(int idPropType, string title, string shortTitle, string sortKey, string formatString, string description, uint flags)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                CustomPropertyType cptOrig = CustomPropertyType.GetCustomPropertyType(idPropType);
                CustomPropertyType cpt = new CustomPropertyType();
                util.CopyObject(cptOrig, cpt);  // make our modifications on a copy to avoid mucking up live objects in case fcommit fails.
                cpt.Title = title;
                cpt.ShortTitle = shortTitle;
                cpt.SortKey = sortKey;
                cpt.FormatString = formatString;
                cpt.Description = description;
                cpt.Flags = flags;
                cpt.FCommit();
                return Json(cpt);
            });
        }
        #endregion

        #region Achievements
        [Authorize]
        [HttpPost]
        public string InvalidateBadgeCache()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                MyFlightbook.Profile.InvalidateAllAchievements();
                return "Achievements invalidated";
            });
        }
        #endregion

        #region Stats
        [Authorize]
        [HttpPost]
        public string TrimOldTokensAndAuths()
        {
            return SafeOp(ProfileRoles.maskCanReport, () =>
            {
                return AdminStats.TrimOldTokensAndAuths();
            });
        }

        [Authorize]
        [HttpPost]
        public string TrimOldOAuth()
        {
            return SafeOp(ProfileRoles.maskCanReport, () =>
            {
                AdminStats.TrimOldTokensAndAuths();
                return string.Empty;
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> FlightsPerUser(string dateRange)
        {
            return await SafeOp(ProfileRoles.maskCanReport, async () =>
            {
                IEnumerable<FlightsPerUserStats> lstFlightsPerUser = null;
                DateTime? creationDate = null;
                if (!String.IsNullOrEmpty(dateRange))
                    creationDate = DateTime.Now.AddMonths(-Convert.ToInt32(dateRange, CultureInfo.InvariantCulture));

                await Task.Run(() => { lstFlightsPerUser = FlightsPerUserStats.Refresh(creationDate); });

                NumericBucketmanager bmFlightsPerUser = new NumericBucketmanager() { BucketForZero = true, BucketWidth = 100, BucketSelectorName = "FlightCount" };
                HistogramManager hmFlightsPerUser = new HistogramManager()
                {
                    SourceData = lstFlightsPerUser,
                    SupportedBucketManagers = new BucketManager[] { bmFlightsPerUser },
                    Values = new HistogramableValue[] { new HistogramableValue("Range", "Flights", HistogramValueTypes.Integer) }
                };

                GoogleChartData flightsPerUserChart = new GoogleChartData
                {
                    Title = "Flights/user",
                    XDataType = GoogleColumnDataType.@string,
                    YDataType = GoogleColumnDataType.number,
                    XLabel = "Flights/User",
                    YLabel = "Users - All",
                    SlantAngle = 90,
                    Width = 1000,
                    Height = 500,
                    ChartType = GoogleChartType.ColumnChart,
                    ContainerID = "flightsPerUserDiv",
                    TickSpacing = (uint)((lstFlightsPerUser.Count() < 20) ? 1 : (lstFlightsPerUser.Count() < 100 ? 5 : 10))
                };

                bmFlightsPerUser.ScanData(hmFlightsPerUser);

                using (DataTable dt = bmFlightsPerUser.ToDataTable(hmFlightsPerUser))
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        flightsPerUserChart.XVals.Add((string)dr["DisplayName"]);
                        flightsPerUserChart.YVals.Add((int)Convert.ToDouble(dr["Flights"], CultureInfo.InvariantCulture));
                    }
                }
                ViewBag.flightsPerUserChart = flightsPerUserChart;
                return PartialView("_userActivity");
            });
        }
        #endregion

        #region Images
        private static void CacheProgress(string key, StringBuilder sb)
        {
            HttpRuntime.Cache.Add(key, sb, null, Cache.NoAbsoluteExpiration, new TimeSpan(0, 30, 0), CacheItemPriority.Normal, null);
        }

        /// <summary>
        /// For a slow operation, returns the current status, or else an empty string once completed.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public string SlowImageOpStatus(string key)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                StringBuilder sb = (StringBuilder)HttpRuntime.Cache[key];
                return sb?.ToString() ?? string.Empty;
            });
        }

        [Authorize]
        [HttpPost]
        public string DeleteOrphans()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                string key = Guid.NewGuid().ToString();

                StringBuilder sb = new StringBuilder();
                CacheProgress(key, sb);

                new Thread(new ThreadStart(() =>
                {
                    sb.Append("Deleting orphaned flight images:\r\n");
                    sb.AppendFormat(CultureInfo.CurrentCulture, "{0}\r\n", MFBImageInfo.ADMINDeleteOrphans(MFBImageInfoBase.ImageClass.Flight));
                    sb.Append("Deleting orphaned endorsement images:\r\n");
                    sb.AppendFormat(CultureInfo.CurrentCulture, "{0}\r\n", MFBImageInfo.ADMINDeleteOrphans(MFBImageInfoBase.ImageClass.Endorsement));
                    sb.Append("Deleting orphaned offline endorsements:\r\n");
                    sb.AppendFormat(CultureInfo.CurrentCulture, "{0}\r\n", MFBImageInfo.ADMINDeleteOrphans(MFBImageInfoBase.ImageClass.OfflineEndorsement));
                    sb.Append("Deleting orphaned Aircraft images:\r\n");
                    sb.AppendFormat(CultureInfo.CurrentCulture, "{0}\r\n", MFBImageInfo.ADMINDeleteOrphans(MFBImageInfoBase.ImageClass.Aircraft));
                    sb.Append("Deleting orphaned BasicMed images:\r\n");
                    sb.AppendFormat(CultureInfo.CurrentCulture, "{0}\r\n", MFBImageInfo.ADMINDeleteOrphans(MFBImageInfoBase.ImageClass.BasicMed));
                    HttpRuntime.Cache.Remove(key); // clear it out to indicate success
                })).Start();
                return key;
            });
        }

        [Authorize]
        [HttpPost]
        public string DeleteDebugS3Images()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                string key = Guid.NewGuid().ToString();

                StringBuilder sb = new StringBuilder();
                CacheProgress(key, sb);

                new Thread(new ThreadStart(() =>
                {
                    sb.AppendLine("Starting to clean S3 debug images...");
                    AWSImageManagerAdmin.ADMINCleanUpDebug();
                    HttpRuntime.Cache.Remove(key);
                })).Start();
                return key;
            });
        }

        [Authorize]
        [HttpPost]
        public string SyncImages(MFBImageInfoBase.ImageClass ic, bool fPreviewOnly)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                string key = Guid.NewGuid().ToString();

                MFBImageAdmin imageAdmin = new MFBImageAdmin(ic);

                CacheProgress(key, imageAdmin.Status);

                new Thread(new ThreadStart(() =>
                {
                    imageAdmin.Status.AppendFormat(CultureInfo.InvariantCulture, "Starting to sync image class {0} (preview = {1})...\r\n", ic.ToString(), fPreviewOnly.ToString().ToUpperInvariant());
                    IEnumerable<DirKey> lstDk = DirKey.DirKeyForClass(ic, out string linkTemplate);
                    imageAdmin.SyncImages(lstDk, fPreviewOnly);
                    HttpRuntime.Cache.Remove(key);
                })).Start();
                return key;
            });
        }

        [Authorize]
        [HttpPost]
        public string DeleteS3Orphans(MFBImageInfoBase.ImageClass ic, bool fPreviewOnly)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                return SafeOp(ProfileRoles.maskCanManageData, () =>
                {
                    string key = Guid.NewGuid().ToString();

                    MFBImageAdmin imageAdmin = new MFBImageAdmin(ic);
                    CacheProgress(key, imageAdmin.Status);

                    new Thread(new ThreadStart(() =>
                    {
                        imageAdmin.Status.AppendFormat(CultureInfo.InvariantCulture, "Starting to delete S3 orphans for image class {0} (preview = {1})...\r\n", ic.ToString(), fPreviewOnly.ToString().ToUpperInvariant());
                        imageAdmin.DeleteS3Orphans(fPreviewOnly, fIsLiveSite: Branding.CurrentBrand.MatchesHost(Request.Url.Host));
                        HttpRuntime.Cache.Remove(key);
                    })).Start();
                    return key;
                });
            });
        }

        [Authorize]
        [HttpPost]
        public string UpdateBrokenImage(MFBImageInfoBase.ImageClass ic, string key, string thumbName, int width, int height)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                if (width == 0)
                    throw new ArgumentOutOfRangeException(nameof(width), "Can't have zero width");
                if (height == 0)
                    throw new ArgumentOutOfRangeException(nameof(height), "Can't have Invalid height");
                MFBImageInfo mfbii = MFBImageInfo.LoadMFBImageInfo(ic, key, thumbName);
                mfbii.WidthThumbnail = width;
                mfbii.HeightThumbnail = height;
                mfbii.ToDB();
                return string.Empty;
            });
        }

        [Authorize]
        [HttpPost]
        public string ProcessPendingVideos()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                PendingVideo.ProcessPendingVideos(out string szSummary);
                return szSummary;
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult NextImageRowset(int offset, int limit, MFBImageInfoBase.ImageClass imageClass)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                Dictionary<string, MFBImageCollection> dictRows = MFBImageInfo.FromDB(imageClass, offset, limit, out List<string> lstKeys);
                ViewBag.imagesRows = dictRows;
                ViewBag.keys = lstKeys;
                ViewBag.imageClass = imageClass;
                ViewBag.linkTemplate = MFBImageInfoBase.TemplateForClass(imageClass);
                return PartialView("_imageRowSet");
            });
        }

        [Authorize]
        [HttpPost]
        public string MigrateImages(MFBImageInfoBase.ImageClass imageClass, int maxFiles, int maxMB)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                return AWSImageManagerAdmin.MigrateImages(maxMB, maxFiles, imageClass);
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult AircraftForFlightImage(int idFlight)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                LogbookEntry le = new LogbookEntry(idFlight, User.Identity.Name, LogbookEntryCore.LoadTelemetryOption.None, true);
                return Json(new { href = VirtualPathUtility.ToAbsolute(String.Format(CultureInfo.InvariantCulture, "~/Member/EditAircraft.aspx?id={0}&a=1", le.AircraftID)), text = le.TailNumDisplay });
            });
        }
        #endregion
        #endregion

        #region Full page endpoints
        [HttpGet]
        [Authorize]
        public ActionResult Images(string id = null, int skip = 0)
        {
            CheckAuth(ProfileRoles.maskCanManageData);
            if (String.IsNullOrEmpty(id) || !Enum.TryParse(id, true, out MFBImageInfoBase.ImageClass result))
            {
                ViewBag.brokenImages = MFBImageAdmin.BrokenImages();
                return View("adminImages");
            } else
            {
                ViewBag.imageClass = result;
                ViewBag.skip = skip;    // set a starting point for images...
                ViewBag.totalRows = String.Format(CultureInfo.CurrentCulture, Resources.Admin.ImageRowsHeader, new MFBImageAdmin(result).ImageRowCount());
                return View("adminReviewImages");
            }
        }

        /// <summary>
        /// Emails nightly stats.  MUST be called from the local machine OR by 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> NightlyStats()
        {
            if (!IsLocalCall())
                CheckAuth(ProfileRoles.maskCanReport);

            AdminStats astats = new AdminStats();
            if (await astats.Refresh(false))
            {
                ViewBag.stats = astats;
                ViewBag.emailOnly = true;
                util.NotifyAdminEvent(String.Format(CultureInfo.CurrentCulture,
                    "{0} site stats as of {1} {2}",
                    Request.Url.Host,
                    DateTime.Now.ToShortDateString(),
                    DateTime.Now.ToShortTimeString()),
                    RenderViewToString(ControllerContext, "adminStatsNightlyMail", null),
                    ProfileRoles.maskCanReport);
            }

            return Content("Success!");
        }

        [HttpGet]
        public async Task<ActionResult> Stats(bool fForEmail = false)
        {
            // Allow local requests, unless you are authenticated AND can report AND not requesting the full page
            // Request.islocal doesn't work when we access via dns name, so actually check IP addresses.
            if (!fForEmail || !IsLocalCall())
                CheckAuth(ProfileRoles.maskCanReport);

            AdminStats astats = new AdminStats();
            if (await astats.Refresh(!fForEmail))
            {
                ViewBag.stats = astats;
                ViewBag.emailOnly = fForEmail;


                return View("stats");
            }
            else
                return new EmptyResult();
        }

        [Authorize]
        [HttpPost]
        public ActionResult UpdateFAQ(int idFaq, string Category, string Question, string Answer)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                FAQItem fi = new FAQItem()
                {
                    Category = Category,
                    Question = Question,
                    Answer = Answer,
                    idFAQ = idFaq
                };
                    fi.Commit();
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpGet]
        public ActionResult FAQ()
        {
            CheckAuth(ProfileRoles.maskCanManageData);
            ViewBag.faqs = FAQItem.AllFAQItems;
            return View("adminFAQ");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Endorsements(int id, string FARRef, string BodyTemplate, string Title)
        {
            CheckAuth(ProfileRoles.maskCanManageData);
            EndorsementType et = new EndorsementType()
            {
                ID = id,
                FARReference = FARRef,
                BodyTemplate = BodyTemplate,
                Title = Title
            };
            et.FCommit();
            ViewBag.templates = EndorsementType.LoadTemplates();
            return View("adminEndorsements");
        }

        [Authorize]
        [HttpGet]
        public ActionResult Endorsements()
        {
            CheckAuth(ProfileRoles.maskCanManageData);
            ViewBag.templates = EndorsementType.LoadTemplates();
            return View("adminEndorsements");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Achievements(int id, string Name, string AirportsRaw, string overlay, bool? fBinary, int bronze = 0, int silver = 0, int gold = 0, int platinum = 0)
        {
            if (Name == null)
                throw new ArgumentNullException(nameof(Name));
            if (AirportsRaw == null)
                throw new ArgumentNullException(nameof(AirportsRaw));
            CheckAuth(ProfileRoles.maskCanManageData);

            AirportListBadgeData b = new AirportListBadgeData()
            {
                ID = (Badge.BadgeID)id,
                Name = Name,
                AirportsRaw = AirportsRaw,
                OverlayName = overlay,
                BinaryOnly = fBinary ?? false
            };
            b.Levels[0] = bronze;
            b.Levels[1] = silver;
            b.Levels[2] = gold;
            b.Levels[3] = platinum;
            b.Commit();
            return Redirect("Achievements");
        }

        [Authorize]
        [HttpGet]
        public ActionResult Achievements()
        {
            CheckAuth(ProfileRoles.maskCanManageData);
            ViewBag.airportBadges = AirportListBadge.BadgeData;
            return View("adminAchievements");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Properties(string txtCustomPropTitle, string txtCustomPropFormat, string txtCustomPropDesc, uint propType, uint propFlags)
        {
            CheckAuth(ProfileRoles.maskCanManageData);  // check for ability to do any admin
            CustomPropertyType cpt = new CustomPropertyType()
            {
                Title = txtCustomPropTitle,
                FormatString = txtCustomPropFormat,
                Description = txtCustomPropDesc,
                Type = (CFPPropertyType)propType,
                Flags = propFlags
            };
            cpt.FCommit();
            ViewBag.propList = CustomPropertyType.GetCustomPropertyTypes();
            return View("adminProps");
        }

        [Authorize]
        [HttpGet]
        public ActionResult Properties()
        {
            CheckAuth(ProfileRoles.maskCanManageData);  // check for ability to do any admin
            ViewBag.propList = CustomPropertyType.GetCustomPropertyTypes();
            return View("adminProps");
        }

        [Authorize]
        public ActionResult Misc()
        {
            CheckAuth(ProfileRoles.maskCanManageData);

            Dictionary<string, int> d = new Dictionary<string, int>();
            foreach (System.Collections.DictionaryEntry entry in HttpRuntime.Cache)
            {
                string szClass = entry.Value.GetType().ToString();
                d[szClass] = d.TryGetValue(szClass, out int value) ? ++value : 1;
            }

            ViewBag.cacheSummary = d;
            ViewBag.memStats = String.Format(CultureInfo.CurrentCulture, "Cache has {0:#,##0} items", HttpRuntime.Cache.Count);
            return View("adminMisc");
        }

        [Authorize]
        // GET: mvc/Admin
        public ActionResult Index()
        {
            CheckAuth(ProfileRoles.maskCanManageData);
            return View();
        }
        #endregion
    }
}