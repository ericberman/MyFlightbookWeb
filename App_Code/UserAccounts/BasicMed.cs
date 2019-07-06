using MyFlightbook.FlightCurrency;
using MyFlightbook.Image;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

/******************************************************
 * 
 * Copyright (c) 2009-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Basicmed
{
    /// <summary>
    /// Provides support for compliance with BasicMed, which goes into effect on May 1 2017.
    /// 
    /// Key requirements:
    ///  - Must have held a medical certificate since 7/15/2016
    ///  - Every 24 months have to take an aeromedical course and keep a record in the logbook
    ///  - Every 4 years, you need to visit a state licensed physician and keep a record (checklist) in the logbook.
    /// </summary>
    public class BasicMed
    {
        #region Properties
        public IEnumerable<BasicMedEvent> Events { get; protected set; }

        public string Username { get; set; }

        /// <summary>
        /// Date by which you had to have a medical in order to use BasicMed
        /// </summary>
        public static DateTime LastMedicalCutoff
        {
            get { return new DateTime(2006, 7, 15); }
        }

        /// <summary>
        /// Earliest allowed date for course or physician visit
        /// </summary>
        public static DateTime EarliestEventDate
        {
            get { return new DateTime(2017, 1, 1); }
        }

        /// <summary>
        /// The date when BasicMed goes live.
        /// </summary>
        public static DateTime BasicMedGoLiveDate
        {
            get { return new DateTime(2017, 5, 1); }
        }
        /// <summary>
        /// 
        /// </summary>
        public CurrencyStatusItem Status
        {
            get
            {
                List<BasicMedEvent> lst = new List<BasicMedEvent>(Events);

                // List should already be sorted descending date, but just in case...
                lst.Sort((bme1, bme2) => { return bme2.EventDate.CompareTo(bme1.EventDate); });

                BasicMedEvent medicalCourse = lst.Find(bme => bme.EventType == BasicMedEvent.BasicMedEventType.AeromedicalCourse);
                BasicMedEvent physicianVisit = lst.Find(bme => bme.EventType == BasicMedEvent.BasicMedEventType.PhysicianVisit);

                // Handle cases where we can't find all three
                if (Profile.GetUser(Username).NextMedical.CompareTo(LastMedicalCutoff) < 0 || medicalCourse == null || physicianVisit == null)
                    return null;

                DateTime expiration = medicalCourse.ExpirationDate.EarlierDate(physicianVisit.ExpirationDate);
                if (expiration.CompareTo(DateTime.Now) < 0) // expired!
                {
                    if (medicalCourse.ExpirationDate.CompareTo(physicianVisit.ExpirationDate) < 0)
                        return new CurrencyStatusItem(Resources.Profile.BasicMedCurrencyTitle, expiration.ToShortDateString(), CurrencyState.NotCurrent, Resources.Profile.BasicMedErrNoMedicalCourseWithin2Years) { CurrencyGroup = CurrencyStatusItem.CurrencyGroups.Medical };
                    else
                        return new CurrencyStatusItem(Resources.Profile.BasicMedCurrencyTitle, expiration.ToShortDateString(), CurrencyState.NotCurrent, Resources.Profile.BasicMedErrNoPhysicalWithin4Years) { CurrencyGroup = CurrencyStatusItem.CurrencyGroups.Medical };
                }
                else
                    return new CurrencyStatusItem(Resources.Profile.BasicMedCurrencyTitle, expiration.ToShortDateString(), (expiration.CompareTo(DateTime.Now.AddCalendarMonths(1)) < 0) ? CurrencyState.GettingClose : CurrencyState.OK, string.Empty) { CurrencyGroup = CurrencyStatusItem.CurrencyGroups.Medical };
            }
        }
        #endregion

        #region Instantiation
        public BasicMed()
        {
            Username = string.Empty;
            Events = new List<BasicMedEvent>();
        }

        public BasicMed(string szUser) : this()
        {
            Username = szUser;
            Events = BasicMedEvent.EventsForUser(szUser);
        }
        #endregion
    }

    /// <summary>
    /// Represents an event that contributes to BasicMed currency; is the base class for aeromedical course events and physician visit events
    /// </summary>
    public class BasicMedEvent
    {
        public enum BasicMedEventType { Unknown, AeromedicalCourse, PhysicianVisit }

        #region Properties
        /// <summary>
        /// UniqueID
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The date of the event
        /// </summary>
        public DateTime EventDate { get; set; }

        /// <summary>
        /// Type of event
        /// </summary>
        public BasicMedEventType EventType { get; set; }

        public string EventTypeDescription
        {
            get
            {
                switch (EventType)
                {
                    default:
                    case BasicMedEventType.Unknown:
                        return string.Empty;
                    case BasicMedEventType.AeromedicalCourse:
                        return Resources.Profile.BasicMedMedicalCourse;
                    case BasicMedEventType.PhysicianVisit:
                        return Resources.Profile.BasicMedPhysicianVisit;
                }
            }
        }

        /// <summary>
        /// Expiration date
        /// </summary>
        public DateTime ExpirationDate
        {
            get
            {
                switch (EventType)
                {
                    case BasicMedEventType.AeromedicalCourse:
                        return EventDate.AddCalendarMonths(24);
                    case BasicMedEventType.PhysicianVisit:
                        // Expiration date here is very explicitly MONTHS, NOT CALENDAR MONTHS.
                        // See the final rule (https://www.faa.gov/news/updates/media/final_rule_faa_2016_9157.pdf, page 18
                        return EventDate.AddYears(4);
                    case BasicMedEventType.Unknown:
                    default:
                        throw new InvalidOperationException("Can't compute an expiration date for an unknown basicmedeventtype");
                }
            }
        }

        /// <summary>
        /// Description of the event, any comments
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Username for which this event is associated
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Any images (scans or PDFs) that document the event.
        /// </summary>
        public IEnumerable<MFBImageInfo> Images { get; set; }

        /// <summary>
        /// Last error, if validation fails
        /// </summary>
        public string LastError { get; private set; }

        /// <summary>
        /// Key to use for images.
        /// </summary>
        public string ImageKey
        {
            get { return ID.ToString(CultureInfo.InvariantCulture); }
        }
        #endregion

        #region Instantiation
        private BasicMedEvent()
        {
            EventDate = DateTime.Now;
            EventType = BasicMedEventType.Unknown;
            User = Description = LastError = string.Empty;
            ID = -1;
            Images = new MFBImageInfo[0];
        }

        public BasicMedEvent(BasicMedEventType eventType, string szUser) : this()
        {
            User = szUser;
            EventType = eventType;
        }

        private BasicMedEvent(MySqlDataReader dr) : this((BasicMedEventType) dr["eventtype"], (string) dr["username"])
        {
            ID = (int)dr["idbasicmedevent"];
            EventDate = (DateTime)dr["eventdate"];
            Description = (string)dr["description"];

            // Populate the images
            ImageList il = new ImageList(MFBImageInfo.ImageClass.BasicMed, ImageKey);
            il.Refresh(true, null, false);
            Images = il.ImageArray;
        }
        #endregion

        #region caching
        private const string szCacheKey = "basicMedEvents";

        private static void ClearCache(string szUser)
        {
            if (!String.IsNullOrEmpty(szUser))
                Profile.GetUser(szUser).AssociatedData.Remove(szCacheKey);
        }

        private static void AddToCache(string szUser, IEnumerable<BasicMedEvent> lst)
        {
            if (String.IsNullOrEmpty(szUser) || lst == null)
                return;

            Profile.GetUser(szUser).AssociatedData[szCacheKey] = lst;
        }

        private static IEnumerable<BasicMedEvent> CachedEvents(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                return null;
            return (IEnumerable<BasicMedEvent>) Profile.GetUser(szUser).CachedObject(szCacheKey);
        }
        #endregion

        #region database
        public bool IsValid()
        {
            LastError = string.Empty;

            try
            {
                if (String.IsNullOrEmpty(User))
                    throw new MyFlightbookValidationException("No username specified");

                if (EventType == BasicMedEventType.Unknown)
                    throw new MyFlightbookValidationException("Must specify a valid eventtype");

                if (!EventDate.HasValue())
                    throw new MyFlightbookValidationException(Resources.Profile.BasicMedErrNoDate);

                if (EventDate.Subtract(DateTime.Now).TotalDays > 3) // allow up to 3 days in the future
                    throw new MyFlightbookValidationException(Resources.Profile.BasicMedErrEventInFuture);

                if (EventDate.CompareTo(BasicMed.EarliestEventDate) < 0)
                    throw new MyFlightbookValidationException(Resources.Profile.BasicMedErrEventTooFarBack);
            }
            catch (MyFlightbookValidationException ex)
            {
                LastError = ex.Message;
                return false;
            }

            return true;
        }

        public void Commit()
        {
            if (!IsValid())
                throw new MyFlightbookException(LastError);

            string szQ = String.Format(CultureInfo.InvariantCulture, "{0} basicmedevent SET eventtype=?et, eventdate=?ed, username=?us, description=?desc {1}",
                ID > 0 ? "UPDATE" : "INSERT INTO",
                ID > 0 ? "WHERE idbasicmedevent=?id" : string.Empty);

            DBHelper dbh = new DBHelper(szQ);
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("et", (int)EventType);
                comm.Parameters.AddWithValue("ed", EventDate);
                comm.Parameters.AddWithValue("us", User);
                comm.Parameters.AddWithValue("desc", Description.LimitTo(255));
                comm.Parameters.AddWithValue("id", ID);
            });
            if (ID < 0)
                ID = dbh.LastInsertedRowId;
            ClearCache(User);
        }

        public void Delete()
        {
            // Delete any images
            // Now delete any associated images.
            try
            {
                ImageList il = new ImageList(MFBImageInfo.ImageClass.BasicMed, ImageKey);
                il.Refresh();
                foreach (MFBImageInfo mfbii in il.ImageArray)
                    mfbii.DeleteImage();

                DirectoryInfo di = new DirectoryInfo(System.Web.Hosting.HostingEnvironment.MapPath(il.VirtPath));
                di.Delete(true);
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
            catch (IOException) { }
            catch (Exception) { throw; }

            new DBHelper("DELETE FROM basicmedevent WHERE idbasicmedevent=?id").DoNonQuery((comm) => { comm.Parameters.AddWithValue("id", ID); });
            ClearCache(User);
        }

        /// <summary>
        /// Returns the basicmed events for the specified user
        /// </summary>
        /// <param name="szUser"></param>
        public static IEnumerable<BasicMedEvent> EventsForUser(string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException("szUser");

            IEnumerable<BasicMedEvent> cached = CachedEvents(szUser);
            if (cached != null)
                return cached;

            List<BasicMedEvent> lst = new List<BasicMedEvent>();
            DBHelper dbh = new DBHelper("SELECT * FROM basicmedevent WHERE username=?us ORDER BY eventdate DESC");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("us", szUser); },
                (dr) => { lst.Add(new BasicMedEvent(dr)); });

            AddToCache(szUser, lst);
            return lst;
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0:d}: {1} {2} (Expiration: {3:d}", EventDate, EventTypeDescription, Description, ExpirationDate);
        }
    }
}