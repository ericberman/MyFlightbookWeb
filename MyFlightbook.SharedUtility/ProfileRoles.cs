using System;

/******************************************************
 * 
 * Copyright (c) 2009-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Encapsulates the various roles a user can have on the site.
    /// </summary>
    public static class ProfileRoles
    {
        [FlagsAttribute]
        public enum UserRoles
        {
            /// <summary>
            /// Basic User Role
            /// </summary>
            None = 0x0000,
            /// <summary>
            /// Support role - can answer user questions, emulate accounts, and so forth
            /// </summary>
            Support = 0x0001,
            /// <summary>
            /// Data manager role - can manage user such as models, aircraft, airports, images, etc.
            /// </summary>
            DataManager = 0x0002,
            /// <summary>
            /// Reporter role - can view reports
            /// </summary>
            Reporter = 0x0004,
            /// <summary>
            /// Accountant role - can manage financial aspects of the site, such as viewing donations
            /// </summary>
            Accountant = 0x0008,
            /// <summary>
            /// Overall Site Administrator role - can do everything, particularly receive site status health (e.g., exception reports), manage other admin roles, and so forth.
            /// </summary>
            SiteAdmin = 0x0010
        };

        #region Bitmasks for roles
        public const uint maskCanManageData = ((uint)UserRoles.SiteAdmin | (uint)UserRoles.DataManager);
        public const uint maskCanReport = ((uint)UserRoles.SiteAdmin | (uint)UserRoles.Reporter);
        public const uint maskSiteAdminOnly = (uint)UserRoles.SiteAdmin;
        public const uint maskCanManageMoney = ((uint)UserRoles.SiteAdmin | (uint)UserRoles.Accountant);
        public const uint maskCanSupport = ((uint)UserRoles.SiteAdmin | (uint)UserRoles.Support | (uint)UserRoles.DataManager); // reporters cannot support
        public const uint maskCanContact = ((uint)UserRoles.SiteAdmin | (uint)UserRoles.Support);
        public const uint maskUnrestricted = 0xFFFFFFFF;
        #endregion

        #region Helper routines for roles
        static public bool CanSupport(UserRoles r)
        {
            return ((uint)r & maskCanSupport) != 0;
        }

        static public bool CanManageData(UserRoles r)
        {
            return ((uint)r & maskCanManageData) != 0;
        }

        static public bool CanReport(UserRoles r)
        {
            return ((uint)r & maskCanReport) != 0;
        }

        static public bool CanManageMoney(UserRoles r)
        {
            return ((uint)r & maskCanManageMoney) != 0;
        }

        static public bool CanDoSomeAdmin(UserRoles r)
        {
            return r != UserRoles.None;
        }

        static public bool CanDoAllAdmin(UserRoles r)
        {
            return r == UserRoles.SiteAdmin;
        }
        #endregion
    }

}