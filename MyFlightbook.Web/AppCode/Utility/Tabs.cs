using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

/******************************************************
 * 
 * Copyright (c) 2008-2020 MyFlightbook LLC
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
        tabMaps,
        tabTraining,
        tabProfile,
        tabAdmin,

        // logbook tabs
        lbtAddNew = 100,
        lbtFindFlights,
        lbtTotals,
        lbtCurrency,
        lbtTrends,
        lbtDownload,
        lbtImport,
        lbtStartingTotals,
        lbtCheckFlights,
        lbtPrintView,
        lbtPending,

        // profile tabs
        pftAccount = 200,
        pftPrefs,
        pftPilotInfo,
        pftDonate,
        pftName,
        pftPass,
        pftQA,

        // Instruction tabs
        instEndorsements = 250,
        instSignFlights,
        instStudents,
        instInstructors,
        inst8710,
        instModelRollup,
        instTimeRollup,
        instProgressTowardsMilestones,
        instAchievements,

        // Map tabs
        mptRoute = 300,
        mptFindAirport,
        mptAddAirports,
        mptVisited,
        mptQuiz,

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

    internal static class tabIDHelper
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static string Description(this tabID id)
        {
            switch (id)
            {
                default:
                    throw new ArgumentOutOfRangeException(nameof(id));
                case tabID.tabUnknown:
                    return string.Empty;
                case tabID.tabHome:
                    return Resources.Tabs.TabHome;
                case tabID.tabLogbook:
                    return Resources.Tabs.TabLogbook;
                case tabID.tabAircraft:
                    return Resources.Tabs.TabAircraft;
                case tabID.tabMaps:
                    return Resources.Tabs.TabAirports;
                case tabID.tabTraining:
                    return Resources.Tabs.TabInstruction;
                case tabID.tabProfile:
                    return Resources.Tabs.TabProfile;
                case tabID.tabAdmin:
                    return Resources.Tabs.TabAdmin;

                //Logbook
                case tabID.lbtAddNew:
                    return Resources.Tabs.LogbookAdd;
                case tabID.lbtFindFlights:
                    return Resources.Tabs.LogbookFind;
                case tabID.lbtTotals:
                    return Resources.Tabs.LogbookTotals;
                case tabID.lbtCurrency:
                    return Resources.Tabs.LogbookCurrency;
                case tabID.lbtTrends:
                    return Resources.Tabs.LogbookAnalysis;
                case tabID.lbtDownload:
                    return Resources.Tabs.LogbookDownload;
                case tabID.lbtImport:
                    return Resources.Tabs.LogbookImport;
                case tabID.lbtStartingTotals:
                    return Resources.Tabs.LogbookStartingTotals;
                case tabID.lbtPrintView:
                    return Resources.Tabs.LogbookPrintView;
                case tabID.lbtPending:
                    return Resources.Tabs.LogbookPending;
                case tabID.lbtCheckFlights:
                    return Resources.Tabs.LogbookCheckFlights;

                // Profile
                case tabID.pftAccount:
                    return Resources.Tabs.ProfileAccount;
                case tabID.pftPrefs:
                    return Resources.Tabs.ProfilePreferences;
                case tabID.pftPilotInfo:
                    return Resources.Tabs.ProfilePilotInformation;
                case tabID.pftDonate:
                    return Resources.Tabs.ProfileDonate;
                case tabID.pftName:
                    return Resources.Tabs.ProfileName;
                case tabID.pftPass:
                    return Resources.Tabs.ProfilePassword;
                case tabID.pftQA:
                    return Resources.Tabs.ProfileQA;

                // Training
                case tabID.instEndorsements:
                    return Resources.Tabs.InstructionEndorsements;
                case tabID.instSignFlights:
                    return Resources.Tabs.InstructionSignedFlights;
                case tabID.instStudents:
                    return Resources.Tabs.InstructionStudents;
                case tabID.instInstructors:
                    return Resources.Tabs.InstructionInstructors;
                case tabID.instProgressTowardsMilestones:
                    return Resources.Tabs.InstructionProgress;
                case tabID.instAchievements:
                    return Resources.Tabs.InstructionAchievements;
                case tabID.inst8710:
                    return Resources.Tabs.Instruction8710;
                case tabID.instModelRollup:
                    return Resources.Tabs.InstructionRollupModel;
                case tabID.instTimeRollup:
                    return Resources.Tabs.InstructionRollupTime;

                // Map
                case tabID.mptRoute:
                    return Resources.Tabs.MapRoutes;
                case tabID.mptFindAirport:
                    return Resources.Tabs.MapFind;
                case tabID.mptAddAirports:
                    return Resources.Tabs.MapAdd;
                case tabID.mptVisited:
                    return Resources.Tabs.MapVisited;
                case tabID.mptQuiz:
                    return Resources.Tabs.MapQuiz;

                // Aircraft
                case tabID.actMyAircraft:
                    return Resources.Tabs.MyAircraft;
                case tabID.actImportAircraft:
                    return Resources.Tabs.ImportAircraft;
                case tabID.actMakes:
                    return Resources.Tabs.TabMakes;
                case tabID.actMyClubs:
                    return Resources.Tabs.Clubs;

                // Admin Tabs
                case tabID.admUsers:
                    return Resources.Tabs.AdminUsers;
                case tabID.admModels:
                    return Resources.Tabs.AdminModels;
                case tabID.admProperties:
                    return Resources.Tabs.AdminProperties;
                case tabID.admStats:
                    return Resources.Tabs.AdminStats;
                case tabID.admManufacturers:
                    return Resources.Tabs.AdminManufacturers;
                case tabID.admImages:
                    return Resources.Tabs.AdminImages;
                case tabID.admAirports:
                    return Resources.Tabs.AdminAirports;
                case tabID.admEndorsements:
                    return Resources.Tabs.AdminEndorsements;
                case tabID.admFAQ:
                    return Resources.Tabs.AdminFAQ;
                case tabID.admAircraft:
                    return Resources.Tabs.AdminAircraft;
                case tabID.admDonations:
                    return Resources.Tabs.AdminDonations;
                case tabID.admAchievements:
                    return Resources.Tabs.AdminAchievements;
                case tabID.admMisc:
                    return Resources.Tabs.AdminMisc;
                case tabID.admTelemetry:
                    return Resources.Tabs.AdminTelemetry;
            }
        }
    }

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
        public string Text
        {
            get { return ID.Description(); }
        }

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
        public List<ProfileRoles.UserRoles> Roles { get; }

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
            Link = string.Empty;
            ID = tabID.tabUnknown;
            Roles = new List<ProfileRoles.UserRoles>();
        }

        /// <summary>
        /// Creates a new TabItem with specific parameters
        /// </summary>
        /// <param name="szLink">The link</param>
        /// <param name="tabID">The ID</param>
        /// <param name="szRoles">The roles, if any</param>
        public TabItem(string szLink, tabID id, string szRoles) : this()
        {
            Link = szLink;
            ID = id;
            if (!String.IsNullOrEmpty(szRoles))
            {
                string[] rgRoles = szRoles.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string sz in rgRoles)
                    Roles.Add((ProfileRoles.UserRoles)Enum.Parse(typeof(ProfileRoles.UserRoles), sz));
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
            List<TabItem> lst = new List<TabItem>();

            foreach (XElement item in items)
            {
                TabItem ti = new TabItem(item.Element("Link").Value, (tabID)Enum.Parse(typeof(tabID), item.Element("ID").Value), item.Element("Roles").Value);
                XElement subItems = item.Element("Items");
                ti.Children = (subItems == null) ? new List<TabItem>() : ReadTabs(subItems.Elements("Item"));
                lst.Add(ti);
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
                throw new ArgumentNullException(nameof(lst));
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
