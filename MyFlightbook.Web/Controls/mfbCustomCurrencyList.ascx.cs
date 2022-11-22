using MyFlightbook.Currency;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2017-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    public partial class mfbCustomCurrencyList : UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                RefreshCustomCurrencyList();
        }

        protected void mfbCustCurrency_CurrencyAdded(object sender, CustomCurrencyEventArgs e)
        {
            gvCustomCurrency.EditIndex = -1;
            RefreshCustomCurrencyList();
            pnlAddCustomCurrency_CollapsiblePanelExtender.ClientState = "true";
        }

        public void RefreshCustomCurrencyList()
        {
            gvCustomCurrency.DataSource = CustomCurrency.CustomCurrenciesForUser(Page.User.Identity.Name);
            gvCustomCurrency.DataBind();
        }

        protected void gvCustomCurrency_RowCommand(Object sender, CommandEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (String.Compare(e.CommandName, "_Delete", StringComparison.OrdinalIgnoreCase) == 0)
            {
                IEnumerable<CustomCurrency> rgCurrency = CustomCurrency.CustomCurrenciesForUser(Page.User.Identity.Name);
                int id = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
                foreach (CustomCurrency cc in rgCurrency)
                    if (cc.ID == id)
                    {
                        cc.FDelete();
                        gvCustomCurrency.EditIndex = -1;
                        RefreshCustomCurrencyList();
                        break;
                    }
            }
        }

        protected void gvCustomCurrency_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            Controls_mfbCustCurrency ccEdit = (Controls_mfbCustCurrency)gvCustomCurrency.Rows[e.RowIndex].FindControl("mfbEditCustCurrency");
            if (ccEdit.Commit())
            {
                gvCustomCurrency.EditIndex = -1;
                RefreshCustomCurrencyList();
            }
            else
                e.Cancel = true;
        }

        protected void gvCustomCurrency_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                if ((e.Row.RowState & DataControlRowState.Edit) == DataControlRowState.Edit)
                {
                    Controls_mfbCustCurrency ccEdit = (Controls_mfbCustCurrency)e.Row.FindControl("mfbEditCustCurrency");
                    ccEdit.Currency = (CustomCurrency)e.Row.DataItem;
                }
            }
        }

        protected void gvCustomCurrency_RowEditing(object sender, GridViewEditEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            gvCustomCurrency.EditIndex = e.NewEditIndex;
            RefreshCustomCurrencyList();
        }

        protected void gvCustomCurrency_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            gvCustomCurrency.EditIndex = -1;
            RefreshCustomCurrencyList();
        }

        protected void ckActive_CheckedChanged(object sender, EventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));

            CheckBox c = sender as CheckBox;
            GridViewRow gvr = c.NamingContainer as GridViewRow;
            List<CustomCurrency> rgcc = new List<CustomCurrency>(CustomCurrency.CustomCurrenciesForUser(Page.User.Identity.Name));
            CustomCurrency cc = rgcc[gvr.DataItemIndex];
            cc.IsActive = c.Checked;
            cc.FCommit();
            gvCustomCurrency.DataSource = rgcc;
            gvCustomCurrency.DataBind();
        }
    }
}