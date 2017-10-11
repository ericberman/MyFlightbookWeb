using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Globalization;
using MyFlightbook;
using MyFlightbook.Image;

/******************************************************
 * 
 * Copyright (c) 2013-2016 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Public_AllMakes : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Page.User.Identity.IsAuthenticated && String.IsNullOrEmpty(Request.PathInfo))
        {
            Response.Redirect("~/Member/Makes.aspx", true);
            return;
        }

        if (!IsPostBack)
        {
            this.Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.Makes.AllMakesTitle, Branding.CurrentBrand.AppName);
            string[] rgIds = Request.PathInfo.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            int clevels = rgIds.Length;
            if (clevels >= mvLevelToShow.Views.Count)
                return;
            mvLevelToShow.ActiveViewIndex = clevels;

            try
            {
                switch (clevels)
                {
                    case 0: // base page - show manufacturers
                        gvManufacturers.DataSource = Manufacturer.AllManufacturers();
                        gvManufacturers.DataBind();
                        break;
                    case 1: // specific manufacturer - show their models
                            // No images, just for performance
                        gvMakes.DataSource = MakeModel.MatchingMakes(Convert.ToInt32(rgIds[0], CultureInfo.InvariantCulture));
                        gvMakes.DataBind();
                        break;
                    case 2: // specific model - show all aircraft
                        int idModel = Convert.ToInt32(rgIds[1], CultureInfo.InvariantCulture);
                        UserAircraft ua = new UserAircraft(string.Empty);
                        Aircraft[] rgac = ua.GetAircraftForUser(UserAircraft.AircraftRestriction.AllMakeModel, idModel);
                        gvAircraft.DataSource = rgac;
                        gvAircraft.DataBind();
                        break;
                }
            }
            catch (FormatException)
            {
                Response.Clear();
                Response.StatusCode = 404;
                Response.End();
            }
        }
    }

    protected void MakesRowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e != null && e.Row.RowType == DataControlRowType.DataRow)
        {
            Controls_mfbMakeListItem mli = (Controls_mfbMakeListItem)e.Row.FindControl("mfbMakeListItem1");
            MakeModel mm = (MakeModel) e.Row.DataItem;
            mli.Model = mm;
            mli.ModelLink.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "{0}/{1}", Request.Url.LocalPath, mm.MakeModelID);
        }
    }

    public void AddPictures(Object sender, GridViewRowEventArgs e)
    {
        if (e != null && e.Row.RowType == DataControlRowType.DataRow)
        {
            Aircraft ac = (Aircraft)e.Row.DataItem;

            Controls_mfbImageList mfbIl = (Controls_mfbImageList)LoadControl("~/Controls/mfbImageList.ascx");
            mfbIl.Key = ac.AircraftID.ToString(CultureInfo.InvariantCulture);
            mfbIl.AltText = "Image of " + ac.TailNumber;
            mfbIl.ImageClass = MFBImageInfo.ImageClass.Aircraft;
            mfbIl.CanEdit = false;
            mfbIl.Columns = 3;
            mfbIl.MaxImage = -1;

            if (mfbIl.Refresh() > 0) // only add image list if there are images.
            {
                PlaceHolder p = (PlaceHolder)e.Row.FindControl("plcImages");
                p.Controls.Add(mfbIl);
            }

            // Show aircraft capabilities too.
            FormView fv = (FormView)e.Row.FindControl("fvModelCaps");
            MakeModel m = MakeModel.GetModel(ac.ModelID);
            fv.DataSource = new MakeModel[] { m };
            fv.DataBind();
            Label lblCatClass = (Label)e.Row.FindControl("lblCatClass");
            lblCatClass.Text = m.CategoryClassDisplay;
        }
    }
}