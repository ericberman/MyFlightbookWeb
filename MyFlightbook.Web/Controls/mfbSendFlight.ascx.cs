using MyFlightbook;
using System;
using System.Globalization;
using System.Net.Mail;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2019-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Controls
{
    public partial class mfbSendFlight : UserControl
    {
        #region Properties
        /// <summary>
        /// Location for the link to the flight in email
        /// </summary>
        public string SendPageTarget
        {
            get { return hdnFlightSendToTarget.Value; }
            set { hdnFlightSendToTarget.Value = value; }
        }
        #endregion

        protected void Page_Load(object sender, EventArgs e) { }
    }
}