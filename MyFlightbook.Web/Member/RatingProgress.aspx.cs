using MyFlightbook.Instruction;
using MyFlightbook.Histogram;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2013-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.RatingsProgress
{
    public partial class RatingProgressPage : Page
    {
        private const string szVSMilestones = "MilestonesViewstateKey";
        private const string szVSTargetUser = "VSTargetUser";
        private const string szCookieLastGroup = "cookMilestoneLastGroup";
        private const string szCookieLastMilestone = "cookMilestoneLastMilestone";
        private const string szCommaCookieSub = "#COMMA#";

        protected IEnumerable<MilestoneGroup> Milestones
        {
            get
            {
                IEnumerable<MilestoneGroup> lstMilestones = (IEnumerable<MilestoneGroup>)ViewState[szVSMilestones];
                if (lstMilestones == null)
                    ViewState[szVSMilestones] = lstMilestones = MilestoneProgress.AvailableProgressItems(TargetUser);
                return lstMilestones;
            }
            set
            {
                ViewState[szVSMilestones] = value;
            }
        }

        protected IEnumerable<HistogramableValue> HistogramableValues
        {
            get
            {
                // build a map of all histogrammable values for naming
                List<HistogramableValue> lst = new List<HistogramableValue>(LogbookEntryDisplay.HistogramableValues);

                foreach (CustomPropertyType cpt in CustomPropertyType.GetCustomPropertyTypes(Page.User.Identity.Name))
                {
                    HistogramableValue hv = LogbookEntryDisplay.HistogramableValueForPropertyType(cpt);
                    if (hv != null)
                        lst.Add(hv);
                }
                return lst;
            }
        }

        protected string TargetUser
        {
            get
            {
                string szUser = (string)ViewState[szVSTargetUser];

                if (String.IsNullOrEmpty(szUser))
                {
                    // Assume we're doing it for the target user; we can override below
                    szUser = Page.User.Identity.Name;

                    string szTargetUser = util.GetStringParam(Request, "user");
                    if (!String.IsNullOrEmpty(szTargetUser))
                    {
                        // Two scenarios:
                        // a) Support person (a=1 and current user is support)
                        // b) Instructor
                        // Check for admin
                        if (util.GetStringParam(Request, "a").Length > 0 && Profile.GetUser(Page.User.Identity.Name).CanSupport)
                            szUser = szTargetUser;
                        else
                        {
                            CFIStudentMap csm = new CFIStudentMap(Page.User.Identity.Name);
                            foreach (InstructorStudent student in csm.Students)
                                if (String.Compare(student.UserName, szTargetUser, StringComparison.Ordinal) == 0 && student.CanViewLogbook)
                                    szUser = szTargetUser;
                        }
                    }
                    ViewState[szVSTargetUser] = szUser;
                }
                return szUser;
            }
        }

        protected void ClearCookie()
        {
            Response.Cookies[szCookieLastGroup].Value = Response.Cookies[szCookieLastMilestone].Value = string.Empty;
        }

        protected void SetCookie()
        {
            Response.Cookies[szCookieLastGroup].Value = cmbMilestoneGroup.SelectedValue.Replace(",", szCommaCookieSub);
            Response.Cookies[szCookieLastMilestone].Value = cmbMilestones.SelectedValue.Replace(",", szCommaCookieSub);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Master.SelectedTab = tabID.instProgressTowardsMilestones;

            if (!IsPostBack)
            {
                Master.ShowSponsoredAd = false;
                lblTitle.Text = String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.PageTitle, HttpUtility.HtmlEncode(Profile.GetUser(TargetUser).UserFullName));
                lblOverallProgressDisclaimer.Text = Branding.ReBrand(Resources.MilestoneProgress.OverallProgressDisclaimer);
                cmbMilestoneGroup.DataSource = Milestones;
                cmbMilestoneGroup.DataBind();

                HttpCookie cookieLastGroup = Request.Cookies[szCookieLastGroup];
                HttpCookie cookieLastMilestone = Request.Cookies[szCookieLastMilestone];
                if (cookieLastGroup != null && cookieLastMilestone != null && !String.IsNullOrEmpty(cookieLastGroup.Value) && !String.IsNullOrEmpty(cookieLastMilestone.Value))
                {
                    try
                    {
                        cmbMilestoneGroup.SelectedValue = cookieLastGroup.Value.Replace(szCommaCookieSub, ",");
                        cmbMilestoneGroup_SelectedIndexChanged(cmbMilestoneGroup, e);
                        cmbMilestones.SelectedValue = cookieLastMilestone.Value.Replace(szCommaCookieSub, ",");
                        Refresh();
                    }
                    catch (Exception ex) when (ex is ArgumentOutOfRangeException)
                    {
                        ClearCookie();
                    }
                }

                RefreshCustomRatings();
            }

        }

        protected void Refresh()
        {
            // Empty the page defensively
            gvMilestoneProgress.DataSource = null;
            gvMilestoneProgress.DataBind();

            pnlOverallProgress.Visible = false;
            ClearCookie();

            if (cmbMilestones.SelectedIndex == 0 || String.IsNullOrEmpty(cmbMilestoneGroup.SelectedValue) || Milestones.FirstOrDefault(mg => mg.GroupName.CompareCurrentCulture(cmbMilestoneGroup.SelectedValue) == 0) == null)
                return;

            MilestoneGroup mgSel = Milestones.FirstOrDefault(mg => mg.GroupName.CompareCurrentCulture(cmbMilestoneGroup.SelectedValue) == 0);

            if (mgSel == null || cmbMilestones.SelectedIndex > mgSel.Milestones.Count || cmbMilestones.SelectedIndex == 0)
                return;

            MilestoneProgress mp = mgSel.Milestones[cmbMilestones.SelectedIndex - 1];
            SetCookie();

            if (!mp.HasData)
                mp.Username = TargetUser;

            lblRatingOverallDisclaimer.Text = mp.GeneralDisclaimer.Linkify();
            pnlRatingDisclaimer.Visible = !String.IsNullOrEmpty(lblRatingOverallDisclaimer.Text);

            gvMilestoneProgress.DataSource = mp.ComputedMilestones;
            gvMilestoneProgress.DataBind();

            pnlOverallProgress.Visible = true;
            int cMilestones = 0;
            int cMetMilestones = 0;
            foreach (MilestoneItem mi in mp.ComputedMilestones)
            {
                cMilestones++;
                if (mi.IsSatisfied)
                    cMetMilestones++;
            }
            lblOverallProgress.Text = String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.OverallProgressTemplate, cMetMilestones, cMilestones);
            lblPrintHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.MilestoneProgress.RatingProgressPrintHeaderTemplate, mp.Title, Profile.GetUser(TargetUser).UserFullName, DateTime.Now.Date.ToShortDateString());
        }

        protected void cmbMilestones_SelectedIndexChanged(object sender, EventArgs e)
        {
            Refresh();
        }

        protected void gvMilestoneProgress_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                MilestoneItem mi = (MilestoneItem)e.Row.DataItem;
                MultiView mv = (MultiView)e.Row.FindControl("mvProgress");
                if (mi.Type == MilestoneItem.MilestoneType.AchieveOnce)
                {
                    mv.ActiveViewIndex = 1;
                    HyperLink l = (HyperLink)e.Row.FindControl("lnkFlight");
                    l.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Public/ViewPublicFlight.aspx/{0}", mi.MatchingEventID);

                    MultiView mvAchievedStatus = (MultiView)e.Row.FindControl("mvAchievement");
                    mvAchievedStatus.ActiveViewIndex = (mi.IsSatisfied) ? 0 : 1;
                }
                else
                {
                    mv.ActiveViewIndex = 0;
                    HtmlControl c = (HtmlControl)mv.FindControl("divPercent");
                    int cappedPercentage = (int)Math.Min(mi.Percentage, 100);
                    c.Style["width"] = String.Format(CultureInfo.InvariantCulture, "{0}%", cappedPercentage);
                    c.Style["background-color"] = (cappedPercentage < 100) ? "#CCCCCC" : "limegreen";
                }

                Panel pnlNote = (Panel)e.Row.FindControl("pnlNote");
                pnlNote.Visible = !String.IsNullOrEmpty(mi.Note);
            }
        }

        protected void cmbMilestoneGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbMilestones.Items.Clear();
            ListItem li = new ListItem(Resources.MilestoneProgress.RatingUnknown, String.Empty)
            {
                Selected = true
            };
            cmbMilestones.Items.Add(li);

            MilestoneGroup mgSel = Milestones.FirstOrDefault(mg => mg.GroupName.CompareCurrentCulture(cmbMilestoneGroup.SelectedValue) == 0);
            if (mgSel != null)
            {
                cmbMilestones.DataSource = mgSel.Milestones;
                cmbMilestones.DataBind();
            }

            // Only show the "Add/Edit" ratings box if custom ratings is selected AND viewing your own milestones
            pnlAddEdit.Visible = mgSel is CustomRatingsGroup && Page.User.Identity.Name.CompareCurrentCultureIgnoreCase(TargetUser) == 0;

            Refresh();
        }

        #region Custom Ratings
        protected void RefreshCustomRatings()
        {
            gvCustomRatings.DataSource = CustomRatingProgress.CustomRatingsForUser(TargetUser);
            gvCustomRatings.DataBind();
        }

        protected void UpdateMilestones()
        {
            MilestoneGroup mgeCustom = null;
            foreach (MilestoneGroup mg in Milestones)
            {
                if (mg is CustomRatingsGroup)
                {
                    mgeCustom = mg as CustomRatingsGroup;
                    break;
                }
            }

            if (mgeCustom != null)
            {
                Milestones = MilestoneProgress.AvailableProgressItems(TargetUser);
                // Refresh the milestones to reflect the current milestone or group of milestones.
                cmbMilestoneGroup_SelectedIndexChanged(cmbMilestoneGroup, new EventArgs());
            }
        }

        protected void btnAddNew_Click(object sender, EventArgs e)
        {
            Validate("vgAddRating");
            if (!Page.IsValid)
                return; // let the validation do its thing?

            CustomRatingProgress crp = new CustomRatingProgress() { Title = txtNewTitle.Text, FARLink = string.Empty, GeneralDisclaimer = txtNewDisclaimer.Text, Username=Page.User.Identity.Name };

            List<CustomRatingProgress> lst = new List<CustomRatingProgress>(CustomRatingProgress.CustomRatingsForUser(Page.User.Identity.Name)) { crp };
            CustomRatingProgress.CommitRatingsForUser(Page.User.Identity.Name, lst);

            txtNewDisclaimer.Text = txtNewDisclaimer.Text = string.Empty;

            RefreshCustomRatings();

            UpdateMilestones();
        }

        protected void gvCustomRatings_RowEditing(object sender, GridViewEditEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            gvCustomRatings.EditIndex = e.NewEditIndex;
            RefreshCustomRatings();
        }

        protected void gvCustomRatings_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            gvCustomRatings.EditIndex = -1;
            RefreshCustomRatings();
        }

        protected void gvCustomRatings_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (sender == null)
                throw new ArgumentNullException(nameof(e));
            GridView gv = sender as GridView;
            List<CustomRatingProgress> lst = new List<CustomRatingProgress>(CustomRatingProgress.CustomRatingsForUser(Page.User.Identity.Name));
            if (e.CommandName.CompareCurrentCultureIgnoreCase("_DeleteRating") == 0)
            {
                lst.RemoveAll(crp => crp.Title.CompareCurrentCulture((string) e.CommandArgument) == 0);
                gvCustomRatings.EditIndex = -1;
                CustomRatingProgress.CommitRatingsForUser(Page.User.Identity.Name, lst);
                RefreshCustomRatings();

                UpdateMilestones();
            }
            else if (e.CommandName.CompareCurrentCultureIgnoreCase("_DeleteMilestone") == 0)
            {
                CustomRatingProgress crp = lst[Convert.ToInt32(hdnCpeIndex.Value, CultureInfo.InvariantCulture)];
                crp.RemoveMilestoneAt(Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture));
                mpeEditMilestones.Show();   // keep the dialog open
                CustomRatingProgress.CommitRatingsForUser(Page.User.Identity.Name, lst);
                gv.DataSource = crp.ProgressItems;
                gv.DataBind();
                UpdateMilestones();
            }
            else if (e.CommandName.CompareCurrentCultureIgnoreCase("_EditMilestones") == 0)
            {
                int row = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
                CustomRatingProgress crp = lst[row];
                hdnCpeIndex.Value = row.ToString(CultureInfo.InvariantCulture);

                if (cmbFields.Items.Count == 0)
                {
                    // initialize the editing drop-downs, if needed
                    cmbFields.DataSource = HistogramableValues;
                    cmbFields.DataBind();
                    cmbQueries.DataSource = CannedQuery.QueriesForUser(User.Identity.Name);
                    cmbQueries.DataBind();
                }

                lblEditMilestonesForProgress.Text = String.Format(CultureInfo.InvariantCulture, Resources.MilestoneProgress.CustomProgressMilestonesForCustomRating, HttpUtility.HtmlEncode(crp.Title));
                gvCustomRatingItems.DataSource = crp.ProgressItems;
                gvCustomRatingItems.DataBind();
                mpeEditMilestones.Show();
            }
            else if (e.CommandName.CompareCurrentCultureIgnoreCase("Update") == 0)
            {
                // Find the existing item
                CustomRatingProgress crp = lst[gv.EditIndex];
                if (crp != null)
                {
                    string szNewTitle = ((TextBox)gv.Rows[0].Cells[1].Controls[0]).Text;
                    if (String.IsNullOrWhiteSpace(szNewTitle))
                        return;
                    crp.Title = szNewTitle;
                    crp.GeneralDisclaimer = ((TextBox)gv.Rows[0].Cells[2].Controls[0]).Text;
                }
                CustomRatingProgress.CommitRatingsForUser(Page.User.Identity.Name, lst);
                gvCustomRatings.EditIndex = -1;

                RefreshCustomRatings();
                UpdateMilestones();
            }
        }

        protected void btnAddMilestone_Click(object sender, EventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (sender == null)
                throw new ArgumentNullException(nameof(e));

            mpeEditMilestones.Show();   // keep this visible

            int rowIndex = Convert.ToInt32(hdnCpeIndex.Value, CultureInfo.InvariantCulture);

            Validate("vgMilestone");
            if (!Page.IsValid)
                return;

            // Additional validations
            if (decThreshold.Value <= 0)
            {
                lblErrThreshold.Visible = true;
                return;
            }

            List<CustomRatingProgress> lst = new List<CustomRatingProgress>(CustomRatingProgress.CustomRatingsForUser(Page.User.Identity.Name));
            CustomRatingProgress crp = lst[rowIndex];

            crp.AddMilestoneItem(new CustomRatingProgressItem()
            {
                Title = txtMiTitle.Text,
                FARRef = txtMiFARRef.Text,
                Note = txtMiNote.Text,
                Threshold = decThreshold.Value,
                QueryName = cmbQueries.SelectedValue,
                FieldName = cmbFields.SelectedValue,
                FieldFriendlyName = cmbFields.SelectedItem.Text
            });
            CustomRatingProgress.CommitRatingsForUser(Page.User.Identity.Name, lst);

            gvCustomRatingItems.DataSource = crp.ProgressItems;
            gvCustomRatingItems.DataBind();
            UpdateMilestones();

            // Set up for the next one.
            cmbQueries.SelectedValue = txtMiFARRef.Text = txtMiTitle.Text = txtMiNote.Text = string.Empty;
            cmbFields.SelectedIndex = 0;
            decThreshold.Value = 0;
        }

        protected void gvCustomRatings_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {

        }
        #endregion
    }
}