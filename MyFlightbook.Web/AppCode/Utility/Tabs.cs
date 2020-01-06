using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

/******************************************************
 * 
 * Copyright (c) 2008-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// All of the tabs and side-navigation items.
    /// </summary>
    public enum tabID
    {
        tabUnknown = -1,

        // top-level tabs
        tabHome = 0,
        tabLogbook,
        tabAircraft,
        tabMakes,
        tabMaps,
        tabTraining,
        tabProfile,
        tabAdmin,
        tabFlightAnalysis,

        // logbook tabs
        lbtAddNew = 100,
        lbtFindFlights,
        lbtTotals,
        lbtCurrency,
        lbtTrends,
        lbt8710,
        lbtDownload,
        lbtImport,
        lbtStartingTotals,
        lbtPrintView,
        lbtPending,

        // profile tabs
        pftName = 200,
        pftPass,
        pftQA,
        pftAccount,
        pftPrefs,
        pftPilotInfo,
        pftDonate,

        // Instruction tabs
        instEndorsements = 250,
        instSignFlights,
        instStudents,
        instInstructors,
        instProgressTowardsMilestones,
        instAchievements,

        // Map tabs
        mptRoute = 300,
        mptFindAirport,
        mptAddAirports,
        mptVisited,
        mptQuiz,
        mptAllAirports,

        // Analysis tabs - obsolete
        flaChart = 400,
        flaData,
        flaDownload,

        // Aircraft tabs
        actMyAircraft = 500,
        actImportAircraft,
        actMakes,
        actMyClubs,

        // Admin Tabs
        admUsers = 600,
        admModels,
        admProperties,
        admStats,
        admManufacturers,
        admImages,
        admAirports,
        admEndorsements,
        admFAQ,
        admAircraft,
        admDonations,
        admAchievements,
        admMisc,
        admTelemetry
    };

    public class TabClickedEventArgs : EventArgs
    {
        public tabID TabID { get; set; }

        public TabClickedEventArgs(tabID tid = tabID.tabHome)
            : base()
        {
            TabID = tid;
        }
    }

    [Serializable]
    public class TabItem
    {
        #region properties
        /// <summary>
        /// The display text for the tab item
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The link for the tab
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// The ID for the tab
        /// </summary>
        public tabID ID { get; set; }

        /// <summary>
        /// Roles to which the tab is restricted; empty for everybody
        /// </summary>
        public List<ProfileRoles.UserRole> Roles { get; set; }

        /// <summary>
        /// Any child tabs
        /// </summary>
        public IEnumerable<TabItem> Children { get; set; }
        #endregion

        #region constructors
        /// <summary>
        /// Creates a new tabitem.
        /// </summary>
        public TabItem()
        {
            Children = new List<TabItem>();
            Text = Link = string.Empty;
            ID = tabID.tabUnknown;
            Roles = new List<ProfileRoles.UserRole>();
        }

        /// <summary>
        /// Creates a new TabItem with specific parameters
        /// </summary>
        /// <param name="szText">The display text</param>
        /// <param name="szLink">The link</param>
        /// <param name="tabID">The ID</param>
        /// <param name="szRoles">The roles, if any</param>
        public TabItem(string szText, string szLink, tabID id, string szRoles) : this()
        {
            Text = szText;
            Link = szLink;
            ID = id;
            if (!String.IsNullOrEmpty(szRoles))
            {
                string[] rgRoles = szRoles.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string sz in rgRoles)
                    Roles.Add((ProfileRoles.UserRole)Enum.Parse(typeof(ProfileRoles.UserRole), sz));
            }
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} ({1})", Text, Link);
        }
    }

    /// <summary>
    /// Loads a tab list, provides caching and utility functions as well.
    /// </summary>
    public class TabList
    {
        public IEnumerable<TabItem> Tabs { get; set; }

        public TabList()
        {
            Tabs = new List<TabItem>();
        }

        public TabList(string szFile) : this()
        {
            if (!String.IsNullOrEmpty(szFile))
            {
                XDocument doc = XDocument.Load(System.Web.Hosting.HostingEnvironment.MapPath(szFile));
                Tabs = ReadTabs(doc.Elements("Items").Elements("Item"));
            }
        }

        private List<TabItem> ReadTabs(IEnumerable<XElement> items)
        {
            Type t = typeof(Resources.Tabs);
            List<TabItem> lst = new List<TabItem>();

            try
            {
                foreach (XElement item in items)
                {
                    string szNameKey = item.Element("Text").Value.Trim();
                    string szLocName = String.IsNullOrEmpty(szNameKey) ? String.Empty : (string)t.GetProperty(szNameKey).GetValue(t, null);
                    TabItem ti = new TabItem(szLocName, item.Element("Link").Value, (tabID)Enum.Parse(typeof(tabID), item.Element("ID").Value), item.Element("Roles").Value);
                    XElement subItems = item.Element("Items");
                    ti.Children = (subItems == null) ? new List<TabItem>() : ReadTabs(subItems.Elements("Item"));
                    lst.Add(ti);
                }
            } catch(Exception ex)
            {

            }

            return lst;
        }

        public TabList ChildTabList(tabID id)
        {
            TabList tl = new TabList();
            foreach (TabItem ti in Tabs)
            {
                if (ti.ID == id)
                {
                    tl.Tabs = ti.Children;
                    break;
                }
            }
            return tl;
        }

        /// <summary>
        /// Determines if the specified list contains the specified tab somewhere in its children
        /// </summary>
        /// <param name="lst">The list of tabitems</param>
        /// <param name="id">The ID to find</param>
        /// <returns>True if it does</returns>
        static public bool ContainsTab(IEnumerable<TabItem> lst, tabID id)
        {
            if (lst == null)
                throw new ArgumentNullException("lst");
            foreach (TabItem ti in lst)
            {
                if (ti.ID == id)
                    return true;
                if (ContainsTab(ti.Children, id))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the top-level tab associated with this tabID
        /// </summary>
        /// <param name="id">The ID to find</param>
        /// <returns>The top-level tab, or tabUnknown if not found.</returns>
        public tabID TopLevelTab(tabID id)
        {
            tabID tiResult = tabID.tabUnknown;

            foreach (TabItem ti in Tabs)
            {
                if (ti.ID == id || ContainsTab(ti.Children, id))
                    return ti.ID;
            }

            return tiResult;
        }
    }

    /// <summary>
    /// When a tab is going to be bound, this is called to give an opportunity to modify the tab or cancel the binding (e.g., for staged rollout in an A|B test or to allow use of alternative UI's side-by-side
    /// </summary>
    public class TabBoundEventArgs : EventArgs
    {
        public TabItem Item { get; set; }

        public bool Cancel { get; set; }

        public bool SuppressChildren { get; set; }

        public TabBoundEventArgs() : base()
        {
            Item = null;
            SuppressChildren = Cancel = false;
        }

        public TabBoundEventArgs(TabItem ti) : this()
        {
            Item = ti;
        }
    }
}
