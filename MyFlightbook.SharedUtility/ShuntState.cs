/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Manages shunting of the site.
    /// </summary>
    public static class ShuntState
    {
        public const string keyShuntMsg = "ShuntMessage";
        public const string keyShuntState = "ShuntState";

        /// <summary>
        /// True if site is shunted
        /// </summary>
        public static bool IsShunted { get; private set; }

        private static string _rawShuntMessage;

        /// <summary>
        /// The branded message to display when shunted (the underlying shunt message is NOT branded)
        /// </summary>
        public static string ShuntMessage { get { return Branding.ReBrand(_rawShuntMessage); } }

        /// <summary>
        /// Caches the current shunt state; should only be called on application start.
        /// <paramref name="fShunted">true if the site is shunted</paramref>
        /// <paramref name="rawShuntMessage">The message to display when shunted.  It will be rebranded, so you can use "%APP_NAME%" and such.</paramref>
        /// </summary>
        public static void Init(bool fShunted, string rawShuntMessage)
        {
            IsShunted = fShunted;
            _rawShuntMessage = rawShuntMessage;
        }
    }
}
