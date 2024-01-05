using System;
using System.Globalization;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2019-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Controls
{
    public partial class mfbFlightContextMenu : UserControl
    {
        #region events
        public event EventHandler<LogbookEventArgs> DeleteFlight;
        #endregion

        #region properties
        protected int FlightID
        {
            get { return String.IsNullOrEmpty(hdnID.Value) ? LogbookEntryBase.idFlightNew : Convert.ToInt32(hdnID.Value, CultureInfo.InvariantCulture); }
            set 
            { 
                hdnID.Value = value.ToString(CultureInfo.InvariantCulture);
                lnkSendFlight.OnClientClick = String.Format(CultureInfo.InvariantCulture, "javascript:sendFlight({0}); return false;", value);
            }
        }

        public bool CanDelete
        {
            get { return lnkDelete.Visible; }
            set { lnkDelete.Visible = value; }
        }

        public bool CanSend
        {
            get { return lnkSendFlight.Visible; }
            set { lnkSendFlight.Visible = value; }
        }

        public bool CanEdit
        {
            get { return lnkEditThisFlight.Visible; }
            set { lnkEditThisFlight.Visible = value; }
        }

        public string EditTargetFormatString { get; set; }

        public string SignTargetFormatString { get; set; }

        private LogbookEntryDisplay m_le;
        public LogbookEntryDisplay Flight
        {
            get { return m_le; }
            set
            {
                m_le = value;
                if (value != null)
                {
                    FlightID = value.FlightID;
                    lnkRequestSignature.Visible = value.CanRequestSig;

                    // Issue #1161 - enable copy from the context menu
                    Page.ClientScript.RegisterClientScriptInclude("copytoClip", ResolveClientUrl("~/public/Scripts/CopyClipboard.js"));
                    hdnCopyLink.Value = m_le.SocialMediaItemUri().ToString();
                    lnkCopyFlightLink.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript: copyClipboard('', '{0}', true, '{1}');", hdnCopyLink.ClientID, lblFlightCopied.ClientID);

                    // fix the ID of the delete button to prevent replay attacks
                    string szDelID = String.Format(CultureInfo.InvariantCulture, "lnkDel{0}", value.FlightID);
                    ConfirmButtonExtender1.TargetControlID = lnkDelete.ID = szDelID;
                    ConfirmButtonExtender1.ConfirmText = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.LogbookConfirmDelete, "\n\n", m_le.ToShortString());

                    string szEdit = String.Format(CultureInfo.InvariantCulture, EditTargetFormatString, value.FlightID);
                    lnkEditThisFlight.NavigateUrl = szEdit;
                    string szJoin = szEdit.Contains("?") ? "&" : "?";
                    lnkCheck.NavigateUrl = szEdit + szJoin + "Chk=1";
                    lnkClone.NavigateUrl = szEdit + szJoin + "Clone=1";
                    lnkReverse.NavigateUrl = szEdit + szJoin + "Clone=1&Reverse=1";
                    lnkRequestSignature.NavigateUrl = String.Format(CultureInfo.InvariantCulture, SignTargetFormatString, value.FlightID);
                }
            }
        }
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            // Always do full postback on deletion
            ScriptManager.GetCurrent(Page).RegisterPostBackControl(lnkDelete);
        }

        protected void lnkDelete_Click(object sender, EventArgs e)
        {
            DeleteFlight?.Invoke(this, new LogbookEventArgs(FlightID));
        }
    }
}