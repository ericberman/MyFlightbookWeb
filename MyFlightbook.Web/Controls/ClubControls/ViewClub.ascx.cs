using MyFlightbook.Airports;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2014-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Clubs.ClubControls
{
    public partial class ViewClub : UserControl
    {
        const string szVSKeyActiveClub = "ActiveClubVSKey";
        const string szVSCanDelete = "vsCanDelete";

        public Club ActiveClub
        {
            get { return (Club)ViewState[szVSKeyActiveClub]; }
            set
            {
                ViewState[szVSKeyActiveClub] = value;
                Refresh();
            }
        }

        public bool LinkToDetails { get; set; }

        public event EventHandler<ClubChangedEventArgs> ClubChanged;
        public event EventHandler<ClubChangedEventArgs> ClubChangeCanceled;
        public event EventHandler<ClubChangedEventArgs> ClubDeleted;

        public bool ShowCancel { get; set; }
        public bool ShowDelete
        {
            get { return ViewState[szVSCanDelete] != null && (bool)ViewState[szVSCanDelete]; }
            set { ViewState[szVSCanDelete] = value; }
        }

        public FormViewMode DefaultMode
        {
            get { return fvClub.DefaultMode; }
            set { fvClub.DefaultMode = value; }
        }

        public void Update()
        {
            fvClub.UpdateItem(true);
        }

        public void Insert()
        {
            fvClub.InsertItem(true);
        }

        public void Refresh()
        {
            Club c = ActiveClub;
            if (c == null)
                return;

            fvClub.DataSource = new List<Club> { c };
            fvClub.DataBind();

            // Show the link or don't
            if (fvClub.CurrentMode == FormViewMode.ReadOnly)
            {
                MultiView mv = (MultiView)fvClub.FindControl("mvClubHeader");
                if (mv != null)
                    mv.ActiveViewIndex = (LinkToDetails ? 0 : 1);
            }
            else
            {
                Button b = (Button)fvClub.FindControl("btnCancel");
                b.Visible = ShowCancel;
                b = (Button)fvClub.FindControl("btnDelete");
                b.Visible = ShowDelete;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void fvClub_ItemUpdating(object sender, FormViewUpdateEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            Page.Validate("valEditClub");
            if (Page.IsValid)
            {
                Club c = ActiveClub ?? new Club();
                c.City = (string)e.NewValues["City"];
                c.ContactPhone = (string)e.NewValues["ContactPhone"];
                c.Country = (string)e.NewValues["Country"];
                c.Description = (string)e.NewValues["Description"];
                c.HomeAirportCode = (string)e.NewValues["HomeAirportCode"];
                if (!String.IsNullOrEmpty(c.HomeAirportCode))
                {
                    AirportList al = new AirportList(c.HomeAirportCode);
                    List<airport> lst = new List<airport>(al.GetAirportList());
                    airport ap = lst.FirstOrDefault(a => a.IsPort);
                    c.HomeAirportCode = ap == null ? c.HomeAirportCode : ap.Code;
                }
                c.Name = (string)e.NewValues["Name"];
                c.StateProvince = (string)e.NewValues["StateProvince"];
                c.ProvidedLink = (string)e.NewValues["ProvidedLink"];
                c.ID = Convert.ToInt32(e.NewValues["ID"], CultureInfo.InvariantCulture);
                c.EditingPolicy = (Club.EditPolicy) Enum.Parse(typeof(Club.EditPolicy), e.NewValues["EditingPolicy"].ToString(), true);
                c.IsPrivate = Convert.ToBoolean(e.NewValues["IsPrivate"], CultureInfo.InvariantCulture);
                c.ShowHeadshots = Convert.ToBoolean(e.NewValues["ShowHeadshots"], CultureInfo.InvariantCulture);
                c.ShowMobileNumbers = Convert.ToBoolean(e.NewValues["ShowMobileNumbers"], CultureInfo.InvariantCulture);
                c.PrependsScheduleWithOwnerName = Convert.ToBoolean(e.NewValues["PrependsScheduleWithOwnerName"], CultureInfo.InvariantCulture);
                c.DeleteNotifications = (Club.DeleteNoficiationPolicy)Enum.Parse(typeof(Club.DeleteNoficiationPolicy), (string)e.NewValues["DeleteNotifications"]);
                c.DoubleBookRoleRestriction = (Club.DoubleBookPolicy)Enum.Parse(typeof(Club.DoubleBookPolicy), (string)e.NewValues["DoubleBookRoleRestriction"]);
                c.AddModifyNotifications = (Club.AddModifyNotificationPolicy)Enum.Parse(typeof(Club.AddModifyNotificationPolicy), (string)e.NewValues["AddModifyNotifications"]);
                c.TimeZone = TimeZoneInfo.FindSystemTimeZoneById((string)e.NewValues["TimeZone.Id"]);
                if (c.IsNew)
                    c.Creator = Page.User.Identity.Name;
                if (c.FCommit())
                {
                    ClubChanged?.Invoke(this, new ClubChangedEventArgs(ActiveClub));
                    this.ActiveClub = c;
                }
                else
                    lblErr.Text = c.LastError;
            }
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            fvClub.ChangeMode(FormViewMode.ReadOnly);
            Refresh();
            ClubChangeCanceled?.Invoke(sender, new ClubChangedEventArgs(ActiveClub));
        }

        protected void btnDelete_Click(object sender, EventArgs e)
        {
            fvClub.ChangeMode(FormViewMode.ReadOnly);
            Refresh();
            ClubDeleted?.Invoke(sender, new ClubChangedEventArgs(ActiveClub));
        }

        protected void fvClub_ModeChanging(object sender, FormViewModeEventArgs e)
        {

        }
        protected void fvClub_ModeChanged(object sender, EventArgs e)
        {

        }

        protected void fvClub_DataBound(object sender, EventArgs e)
        {
        }
    }
}