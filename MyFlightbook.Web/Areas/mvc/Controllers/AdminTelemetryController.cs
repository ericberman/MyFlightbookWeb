using MyFlightbook.Geography;
using MyFlightbook.Telemetry;
using System;
using System.Linq;
using System.Web;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web.Mvc;
using System.Web.Script.Services;

/******************************************************
 * 
 * Copyright (c) 2023-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class AdminTelemetryController : AdminControllerBase
    {
        #region Admin - Telemetry
        [Authorize]
        [HttpPost]
        public string MigrateDBToFiles(int cLimit, string szLimitUser)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                return TelemetryReference.MigrateFROMDBToFiles(szLimitUser, cLimit);
            });
        }

        [Authorize]
        [HttpPost]
        public string MigrateFilesToDB(int cLimit, string szLimitUser)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                return TelemetryReference.MigrateFROMFilesToDB(szLimitUser, cLimit);
            });
        }

        [Authorize]
        [HttpPost]
        private static string EnumerableToHtmlResult<T>(IEnumerable<T> lst)
        {
            if (lst.Any())
            {
                StringBuilder sb = new StringBuilder();
                foreach (T item in lst)
                    sb.AppendFormat(CultureInfo.CurrentCulture, "<div>{0}</div>", item.ToString());
                return sb.ToString();
            }
            else
                return "<p>No issues found!</p>";
        }

        [Authorize]
        [HttpPost]
        public string FindDupeTelemetry()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                return EnumerableToHtmlResult(TelemetryReference.FindDuplicateTelemetry());
            });
        }

        [Authorize]
        [HttpPost]
        public string FindOrphanReferences()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                return EnumerableToHtmlResult(TelemetryReference.FindOrphanedRefs());
            });
        }

        [Authorize]
        [HttpPost]
        public string FindOrphanedFiles()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                return EnumerableToHtmlResult(TelemetryReference.FindOrphanedFiles());
            });
        }

        [Authorize]
        [HttpPost]
        public string MigrateFlightToDisk(int idFlight)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                LogbookEntry le = new LogbookEntry();
                le.FLoadFromDB(idFlight, null, LogbookEntryCore.LoadTelemetryOption.LoadAll, true);
                le.Telemetry.Compressed = 0;    // no longer know the compressed 
                le.MoveTelemetryFromFlightEntry();

                return "Flight moved from DB to disk";
            });
        }

        [Authorize]
        [HttpPost]
        public string MigrateFlightFromDisk(int idFlight)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                LogbookEntry le = new LogbookEntry();
                le.FLoadFromDB(idFlight, null, LogbookEntryCore.LoadTelemetryOption.LoadAll, true);
                le.MoveTelemetryToFlightEntry();
                return "Flight moved from disk back to DB";
            });
        }

        private static string AntipodesSessionKey(string filename)
        {
            return "ANTIPODES-FILE*:" + filename;
        }

        [Authorize]
        [HttpPost]
        public ActionResult UploadAntipodes()
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                if (Request.Files.Count == 0)
                    throw new InvalidOperationException("No file uploaded");
                HttpPostedFileBase pf = Request.Files[0];
                byte[] rgbytes = new byte[pf.ContentLength];
                _ = pf.InputStream.Read(rgbytes, 0, pf.ContentLength);
                pf.InputStream.Close();
                Session[AntipodesSessionKey(Request.Files[0].FileName)] = rgbytes;
                return new EmptyResult();
            });
        }

        [Authorize]
        [HttpPost]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public ActionResult Antipodes(string fileName)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                string sessionKey = AntipodesSessionKey(fileName);
                byte[] rgbytes = (byte[])Session[sessionKey] ?? throw new InvalidOperationException("Uploaded file not found");
                Session[sessionKey] = null; // clear it out.

                LatLong[] rgStraight = null;
                LatLong[] rgAntipodes = null;

                using (Telemetry.FlightData fdOriginal = new Telemetry.FlightData())
                {
                    if (fdOriginal.ParseFlightData(Encoding.UTF8.GetString(rgbytes)))
                    {
                        rgStraight = fdOriginal.GetPath();
                        rgAntipodes = new LatLong[rgStraight.Length];
                        for (int i = 0; i < rgStraight.Length; i++)
                            rgAntipodes[i] = rgStraight[i].Antipode;
                    }
                }

                if (rgStraight == null || rgAntipodes == null)
                    throw new InvalidOperationException("No path found");

                LatLongBox boundingBoxOriginal = new LatLongBox(rgStraight[0]);
                LatLongBox boundingBoxAntipodes = new LatLongBox(rgAntipodes[0]);
                List<double[]> original = new List<double[]>();
                List<double[]> antipodes = new List<double[]>();

                foreach (LatLong ll in rgStraight)
                {
                    original.Add(new double[] { ll.Latitude, ll.Longitude });
                    boundingBoxOriginal.ExpandToInclude(ll);
                }

                foreach (LatLong ll in rgAntipodes)
                {
                    antipodes.Add(new double[] { ll.Latitude, ll.Longitude });
                    boundingBoxAntipodes.ExpandToInclude(ll);
                }

                return Json(new { original, antipodes, boundingBoxOriginal, boundingBoxAntipodes });
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult PathsForFlight(int idFlight)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                TelemetryReference ted = TelemetryReference.LoadForFlight(idFlight);
                if (ted == null)
                    return null;
                LogbookEntry le = new LogbookEntry();
                le.FLoadFromDB(idFlight, User.Identity.Name, LogbookEntryCore.LoadTelemetryOption.LoadAll, true);

                IEnumerable<LatLong> pathReconstituded = ted.GoogleData.DecodedPath();
                IEnumerable<LatLong> pathStraight = Array.Empty<LatLong>();
                using (Telemetry.FlightData fd = new Telemetry.FlightData())
                {
                    if (fd.ParseFlightData(le.FlightData))
                        pathStraight = fd.GetPath();
                }

                LatLongBox llb = new LatLongBox(pathReconstituded.First());
                List<double[]> lstReconstituded = new List<double[]>();
                List<double[]> lstStraight = new List<double[]>();

                foreach (LatLong ll in pathReconstituded)
                {
                    lstReconstituded.Add(new double[] { ll.Latitude, ll.Longitude });
                    llb.ExpandToInclude(ll);
                }

                foreach (LatLong ll in pathStraight)
                {
                    lstStraight.Add(new double[] { ll.Latitude, ll.Longitude });
                    llb.ExpandToInclude(ll);
                }

                return Json(new { reconstituded = lstReconstituded, straight = lstStraight, boundingBox = llb });
            });
        }
        #endregion

        [HttpPost]
        [Authorize]
        public ActionResult TelemetryForUser(string szUser)
        {
            return SafeOp(ProfileRoles.maskCanManageData, () =>
            {
                ViewBag.Telemetries = String.IsNullOrEmpty(szUser) ? Array.Empty<TelemetryReference>() : TelemetryReference.ADMINTelemetryForUser(szUser);
                return PartialView("_userTelemetry");
            });
        }

        // GET: mvc/AdminTelemetry
        [Authorize]
        public ActionResult Index()
        {
            if (!MyFlightbook.Profile.GetUser(User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException();

            return View("adminTelemetry");
        }
    }
}