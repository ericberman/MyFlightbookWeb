using MyFlightbook;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2013-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_AllMakes : Page
{
    private const string szCacheKeyModels = "keyAllModelsByManufacturer";
    private IDictionary<int, List<MakeModel>> ModelsByManufacturer
    {
        get
        {
            Dictionary<int, List<MakeModel>> d = (Dictionary<int, List<MakeModel>>)Cache[szCacheKeyModels];
            if (d == null)
            {
                d = new Dictionary<int, List<MakeModel>>();
                Collection<MakeModel> allModels = MakeModel.MatchingMakes();

                foreach (MakeModel m in allModels)
                {
                    // skip any sim/generic-only types
                    if (m.AllowedTypes != AllowedAircraftTypes.Any)
                        continue;

                    if (!d.ContainsKey(m.ManufacturerID))
                        d[m.ManufacturerID] = new List<MakeModel>();
                    ((List<MakeModel>)d[m.ManufacturerID]).Add(m);
                }

                Cache.Add(szCacheKeyModels, d, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, 30, 0), System.Web.Caching.CacheItemPriority.BelowNormal, null);
            }
            return (IDictionary<int, List<MakeModel>>) d;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        // This page is for search engines by default.
        if (Page.User.Identity.IsAuthenticated && String.IsNullOrEmpty(Request.PathInfo))
        {
            Response.Redirect("~/Member/Makes.aspx", true);
            return;
        }

        if (!IsPostBack)
        {
            Response.Cache.SetExpires(DateTime.Now.AddSeconds(1209600));
            Response.Cache.SetCacheability(System.Web.HttpCacheability.Public);
            Response.Cache.SetValidUntilExpires(true);
            
            this.Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.Makes.AllMakesTitle, Branding.CurrentBrand.AppName);
            string[] rgIds = Request.PathInfo.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            int clevels = rgIds.Length;
            if (clevels >= mvLevelToShow.Views.Count)
                return;
            mvHeader.ActiveViewIndex = mvLevelToShow.ActiveViewIndex = clevels;
            
            try
            {
                switch (clevels)
                {
                    case 0: // base page - show manufacturers
                        {
                            List<Manufacturer> lst = new List<Manufacturer>(Manufacturer.CachedManufacturers());
                            lst.RemoveAll(man => man.AllowedTypes != AllowedAircraftTypes.Any);
                            gvManufacturers.DataSource = lst;
                            gvManufacturers.DataBind();
                        }
                        break;
                    case 1: // specific manufacturer - show their models
                            // No images, just for performance
                        {
                            int idMan = Convert.ToInt32(rgIds[0], CultureInfo.InvariantCulture);
                            if (ModelsByManufacturer.ContainsKey(idMan))
                            {
                                gvMakes.DataSource = ModelsByManufacturer[idMan];
                                gvMakes.DataBind();
                            }
                            else
                                throw new System.Web.HttpException(404, "Not found");
                        }
                        break;
                    case 2: // specific model - show all aircraft
                        {
                            int idMan = Convert.ToInt32(rgIds[0], CultureInfo.InvariantCulture);
                            int idModel = Convert.ToInt32(rgIds[1], CultureInfo.InvariantCulture);

                            if (!ModelsByManufacturer.ContainsKey(idMan))
                                throw new System.Web.HttpException(404, "Not found");

                            MakeModel m = ModelsByManufacturer[idMan].Find(mm => mm.MakeModelID == idModel);

                            if (m == null)
                                throw new System.Web.HttpException(404, "Not found");

                            rptAttributes.DataSource = m.AttributeList();
                            rptAttributes.DataBind();
                            lblModel.Text = m.DisplayName;

                            List<Aircraft> lst = new List<Aircraft>();
                            // UserAircraft.GetAircraftForUser is pretty heavyweight, especially for models witha  lot of aircraft like C-152.
                            // We just don't need that much detail, since we're just binding images by ID and tailnumbers
                            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "SELECT idaircraft, tailnumber FROM aircraft WHERE idmodel=?modelid AND tailnumber NOT LIKE '{0}%' AND instanceType=1 ORDER BY tailnumber ASC", CountryCodePrefix.szAnonPrefix));
                            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("modelid", idModel); },
                                (dr) =>
                                {
                                    int idaircraft = Convert.ToInt32(dr["idaircraft"], CultureInfo.InvariantCulture);
                                    string tailnumber = (string)dr["tailnumber"];
                                    lst.Add(new Aircraft() { AircraftID = idaircraft, TailNumber = tailnumber });
                                });
                            gvAircraft.DataSource = lst;
                            gvAircraft.DataBind();
                        }
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

            Controls_mfbImageList mfbIl = (Controls_mfbImageList) e.Row.FindControl("mfbAircraftImages");
            mfbIl.Key = ac.AircraftID.ToString(CultureInfo.InvariantCulture);
            mfbIl.AltText = "Image of " + ac.TailNumber;

            mfbIl.Visible = (mfbIl.Refresh() > 0); // only add image list if there are images.
        }
    }
}