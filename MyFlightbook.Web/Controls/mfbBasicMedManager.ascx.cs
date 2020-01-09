using MyFlightbook;
using MyFlightbook.Basicmed;
using System;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbBasicMedManager : System.Web.UI.UserControl
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void btnAddBasicMedEvent_Click(object sender, EventArgs e)
    {
        BasicMedEvent.BasicMedEventType et = (BasicMedEvent.BasicMedEventType)Enum.Parse(typeof(BasicMedEvent.BasicMedEventType), rblBasicMedAction.SelectedValue);
        BasicMedEvent bme = new BasicMedEvent(et, Page.User.Identity.Name)
        {
            EventDate = mfbBasicMedEventDate.Date,
            Description = txtBasicMedNotes.Text
        };

        try
        {
            bme.Commit();

            mfuBasicMedImages.ImageKey = bme.ImageKey;
            mfuBasicMedImages.ProcessUploadedImages();

            mfbBasicMedEventDate.Date = DateTime.Now;
            txtBasicMedNotes.Text = string.Empty;
            cpeBasicMedEvents.Collapsed = true;
            gvBasicMedEvents.EditIndex = -1;
            RefreshBasicMedEvents();
        }
        catch (MyFlightbookException ex)
        {
            lblBasicMedErr.Text = ex.Message;
        }
    }

    public void RefreshBasicMedEvents()
    {
        gvBasicMedEvents.DataSource = BasicMedEvent.EventsForUser(Page.User.Identity.Name);
        gvBasicMedEvents.DataBind();
    }

    #region Gridview management
    protected void gvBasicMedEvents_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            Controls_mfbImageList il = (Controls_mfbImageList)e.Row.FindControl("ilBasicMed");
            if (il != null)
            {
                il.Key = ((BasicMedEvent)e.Row.DataItem).ImageKey;
                il.Refresh();
            }
        }
    }

    protected void gvBasicMedEvents_RowCommand(object sender, CommandEventArgs e)
    {
        if (e != null && String.Compare(e.CommandName, "_Delete", StringComparison.OrdinalIgnoreCase) == 0)
        {
            int id = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            BasicMedEvent bme = BasicMedEvent.EventsForUser(Page.User.Identity.Name).First<BasicMedEvent>(bme2 => bme2.ID == id);
            if (bme != null)
            {
                bme.Delete();
                RefreshBasicMedEvents();
            }
        }
    }

    protected void gvBasicMedEvents_RowEditing(object sender, GridViewEditEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        gvBasicMedEvents.EditIndex = e.NewEditIndex;
        RefreshBasicMedEvents();
    }

    protected void gvBasicMedEvents_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
    {
        gvBasicMedEvents.EditIndex = -1;
        RefreshBasicMedEvents();
    }

    protected void gvBasicMedEvents_RowUpdating(object sender, GridViewUpdateEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        // We're only allowing image editing, at least for now.
        Controls_mfbMultiFileUpload mfu = (Controls_mfbMultiFileUpload)gvBasicMedEvents.Rows[e.RowIndex].FindControl("mfuBasicMedImages");
        mfu.ProcessUploadedImages();
        gvBasicMedEvents.EditIndex = -1;
        RefreshBasicMedEvents();
    }
    #endregion
}