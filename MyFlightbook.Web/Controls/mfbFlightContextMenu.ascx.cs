using MyFlightbook;
using System;
using System.Globalization;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbFlightContextMenu : UserControl
{
    #region events
    public event EventHandler<LogbookEventArgs> SendFlight;
    public event EventHandler<LogbookEventArgs> DeleteFlight;
    
    #endregion

    #region properties
    protected int FlightID
    {
        get { return String.IsNullOrEmpty(hdnID.Value) ? LogbookEntry.idFlightNew : Convert.ToInt32(hdnID.Value, CultureInfo.InvariantCulture); }
        set { hdnID.Value = value.ToString(CultureInfo.InvariantCulture); }
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

    private LogbookEntryDisplay m_le = null;
    public LogbookEntryDisplay Flight
    {
        get { return m_le; }
        set
        {
            m_le = value;
            if (value != null)
            {
                FlightID = value.FlightID;
                mfbMiniFacebook.FlightEntry = value;
                mfbTweetThis.FlightToTweet = value;
                lnkRequestSignature.Visible = value.CanRequestSig;

                // fix the ID of the delete button to prevent replay attacks
                string szDelID = String.Format(CultureInfo.InvariantCulture, "lnkDel{0}", value.FlightID);
                ConfirmButtonExtender1.TargetControlID = lnkDelete.ID = szDelID;
                ConfirmButtonExtender1.ConfirmText = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.LogbookConfirmDelete, "\n\n", m_le.ToShortString());

                string szEdit = String.Format(CultureInfo.InvariantCulture, EditTargetFormatString, value.FlightID);
                lnkEditThisFlight.NavigateUrl = szEdit;
                string szJoin = szEdit.Contains("?") ? "&" : "?";
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

    protected void lnkSendFlight_Click(object sender, EventArgs e)
    {
        if (SendFlight != null)
            SendFlight(this, new LogbookEventArgs(FlightID));
    }

    protected void lnkDelete_Click(object sender, EventArgs e)
    {
        if (DeleteFlight != null)
            DeleteFlight(this, new LogbookEventArgs(FlightID));
    }
}