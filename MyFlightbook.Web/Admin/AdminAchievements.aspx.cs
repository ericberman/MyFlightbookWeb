using MyFlightbook.Achievements;
using System;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2009-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Admin
{
    public partial class AdminAchievements : AdminPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            CheckAdmin(Profile.GetUser(Page.User.Identity.Name).CanManageData);
            Master.SelectedTab = tabID.admDonations;
        }

        protected void btnInvalidateUserAchievements_Click(object sender, EventArgs e)
        {
            MyFlightbook.Profile.InvalidateAllAchievements();
        }

        protected void btnAddAirportAchievement_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtAirportAchievementList.Text) && !String.IsNullOrEmpty(txtAirportAchievementName.Text))
            {
                AirportListBadgeData.Add(txtAirportAchievementName.Text, txtAirportAchievementList.Text, txtOverlay.Text, ckBinaryAchievement.Checked, mfbDecEditBronze.IntValue, mfbDecEditSilver.IntValue, mfbDecEditGold.IntValue, mfbDecEditPlatinum.IntValue);
                txtAirportAchievementList.Text = txtAirportAchievementName.Text = string.Empty;
                gvAirportAchievements.DataBind();
            }
        }
    }
}