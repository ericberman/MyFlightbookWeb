using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2010-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Controls.Prefs
{
    public partial class mfbPropertyBlocklist : UserControl
    {
        private Profile m_pf;

        protected void Page_Load(object sender, EventArgs e)
        {
            m_pf = Profile.GetUser(Page.User.Identity.Name);
            if (!IsPostBack)
                UpdateBlocklist();
        }

        #region Property Blocklist
        protected void UpdateBlocklist(bool fForceDB = false)
        {
            rptUsedProps.DataSource = new List<CustomPropertyType>(CustomPropertyType.GetCustomPropertyTypes(Page.User.Identity.Name, fForceDB)).FindAll(cpt => cpt.IsFavorite);
            rptUsedProps.DataBind();
            List<CustomPropertyType> lstBlocklist = new List<CustomPropertyType>(CustomPropertyType.GetCustomPropertyTypes(m_pf.BlocklistedProperties));
            lstBlocklist.Sort((cpt1, cpt2) => { return cpt1.SortKey.CompareCurrentCultureIgnoreCase(cpt2.SortKey); });
            rptBlockList.DataSource = lstBlocklist;
            rptBlockList.DataBind();
        }

        protected void btnAllowList_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtPropID.Text) && !String.IsNullOrEmpty(txtPropID.Text.Trim()))
            {
                try
                {
                    int idPropType = Convert.ToInt32(txtPropID.Text, CultureInfo.InvariantCulture);
                    m_pf.BlocklistedProperties.RemoveAll(id => id == idPropType);
                    m_pf.FCommit();
                    UpdateBlocklist(true);
                }
                catch
                {
                    throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "Error Parsing proptype '{0}' for allowlist in invariant culture.  Current culture is {1}.", txtPropID.Text, CultureInfo.CurrentCulture.DisplayName));
                }
            }
        }

        protected void btnBlockList_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtPropID.Text) && !String.IsNullOrEmpty(txtPropID.Text.Trim()))
            {
                try
                {
                    int idPropType = Convert.ToInt32(txtPropID.Text, CultureInfo.InvariantCulture);
                    if (!m_pf.BlocklistedProperties.Contains(idPropType))
                    {
                        m_pf.BlocklistedProperties.Add(idPropType);
                        m_pf.FCommit();
                        UpdateBlocklist(true);
                    }
                }
                catch
                {
                    throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "Error Parsing proptype '{0}' for blocklist in invariant culture.  Current culture is {1}.", txtPropID.Text, CultureInfo.CurrentCulture.DisplayName));
                }
            }
        }
        #endregion
    }
}