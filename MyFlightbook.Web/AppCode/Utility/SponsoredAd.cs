using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2016-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.SponsoredAds
{
    /// <summary>
    /// Summary description for SponsoredAd
    /// </summary>
    public class SponsoredAd
    {
        #region Properties
        /// <summary>
        /// ID for this campaign
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// Name for this campaign
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// URL for clicks
        /// </summary>
        public string TargetLink { get; set; }

        /// <summary>
        /// Name of the image file
        /// </summary>
        public string ImageName { get; set; }

        /// <summary>
        /// # of impressions on our end
        /// </summary>
        public int ImpressionCount { get; set; }

        /// <summary>
        /// Click-through counts
        /// </summary>
        public int ClickCount { get; set; }

        /// <summary>
        /// URL for the image (relative)
        /// </summary>
        public string ImagePath
        {
            get { return "~/images/SponsoredAds/" + ImageName; }
        }
        #endregion

        #region object creation
        public SponsoredAd()
        {
            Name = TargetLink = ImageName = string.Empty;
        }

        public SponsoredAd(IDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
            InitFromDataReader(dr);
        }

        private void InitFromDataReader(IDataReader dr)
        {
            ID = Convert.ToInt32(dr["idSponsoredAds"], CultureInfo.InvariantCulture);
            Name = (string)dr["Name"];
            TargetLink = (string)dr["TargetURL"];
            ImageName = (string)dr["ImageName"];
            ImpressionCount = Convert.ToInt32(dr["ImpressionCount"], CultureInfo.InvariantCulture);
            ClickCount = Convert.ToInt32(dr["ClickCount"], CultureInfo.InvariantCulture);
        }
        #endregion

        #region Ad retrieval
        private const string cacheKeySponsoredAds = "sponsoredAdsCacheKey";

        /// <summary>
        /// Returns all ad campaigns, optionally matching a specific ID.  Results are cached.
        /// </summary>
        /// <param name="id">The ID of the desired campaign</param>
        /// <returns></returns>
        public static IEnumerable<SponsoredAd> Campaigns()
        {
            List<SponsoredAd> lst = (List<SponsoredAd>)util.GlobalCache.Get(cacheKeySponsoredAds);
            if (lst == null)
            {
                lst = new List<SponsoredAd>();
                DBHelper dbh = new DBHelper("SELECT * FROM sponsoredads ");
                dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new SponsoredAd(dr)); });
                util.GlobalCache.Set(cacheKeySponsoredAds, lst, DateTimeOffset.UtcNow.AddHours(2));
            }
            return lst;
        }

        /// <summary>
        /// Retrieves a specific ad campaign.  Should hit the cache
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static SponsoredAd GetAd(int id)
        {
            return Campaigns().FirstOrDefault(sa => sa.ID == id);
        }
        #endregion

        #region Click and impression management
        /// <summary>
        /// Increments the # of impressions, updating the database.  Note: could be out of sync with the database; database is normative.
        /// </summary>
        public void AddImpression()
        {
            ImpressionCount++;
            DBHelper dbh = new DBHelper("UPDATE sponsoredads SET ImpressionCount = ImpressionCount + 1 WHERE idSponsoredAds=?id");
            dbh.DoNonQuery((comm) => {comm.Parameters.AddWithValue("id", ID);});
        }

        /// <summary>
        /// Increments the # of clicks, updating the database.  Note: could be out of sync with the database; database is normative.
        /// </summary>
        public void AddClick()
        {
            ClickCount++;
            DBHelper dbh = new DBHelper("UPDATE sponsoredads SET ClickCount = ClickCount + 1 WHERE idSponsoredAds=?id");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("id", ID); });
        }
        #endregion
    }
}