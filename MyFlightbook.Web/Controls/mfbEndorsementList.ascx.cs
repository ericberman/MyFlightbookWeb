using MyFlightbook;
using MyFlightbook.Instruction;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2010-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbEndorsementList : System.Web.UI.UserControl
{
    /// <summary>
    /// The name of the student to restrict on
    /// </summary>
    public string Student
    {
        get { return hdnStudent.Value; }
        set { hdnStudent.Value = value;  }
    }

    /// <summary>
    /// The name of the instructor to restrict on.
    /// </summary>
    public string Instructor
    {
        get { return hdnInstructor.Value; }
        set { hdnInstructor.Value = value;  }
    }

    protected bool CanDelete(Endorsement e)
    {
        return (e != null && e.StudentName.CompareCurrentCultureIgnoreCase(Page.User.Identity.Name) == 0);
    }

    public int RefreshEndorsements()
    {
        IEnumerable<Endorsement> rg = Endorsement.EndorsementsForUser(Student, Instructor);
        gvExistingEndorsements.DataSource = rg;
        gvExistingEndorsements.DataBind();
        int cItems = rg.Count();
        return cItems;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            lnkDownload.Visible = !String.IsNullOrEmpty(Instructor);
        }
    }
    protected void gvExistingEndorsements_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e != null && e.Row.RowType == DataControlRowType.DataRow)
        {
            Endorsement endorsement = (Endorsement)e.Row.DataItem;
            Controls_mfbEndorsement mfbEndorsement = (Controls_mfbEndorsement)e.Row.FindControl("mfbEndorsement1");
            mfbEndorsement.SetEndorsement(endorsement);
        }
    }
    protected void gvExistingEndorsements_RowCommand(object sender, CommandEventArgs e)
    {
        if (e == null || String.IsNullOrEmpty(e.CommandArgument.ToString()))
            return;

        int id = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);

        try
        {
            if (id <= 0)
                throw new MyFlightbookException("Invalid endorsement ID to delete");

            if (e.CommandName.CompareOrdinalIgnoreCase("_DeleteExternal") == 0 && !String.IsNullOrEmpty(e.CommandArgument.ToString()))
            {
                List<Endorsement> rgEndorsements = new List<Endorsement>(Endorsement.EndorsementsForUser(null, Page.User.Identity.Name));

                Endorsement en = rgEndorsements.FirstOrDefault(en2 => en2.ID == id);
                if (en == null)
                    throw new MyFlightbookException("ID of endorsement to delete is not found in owners endorsements");

                if (en.StudentType == Endorsement.StudentTypes.Member)
                    throw new MyFlightbookException(Resources.SignOff.errCantDeleteMemberEndorsement);

                en.FDelete();
                RefreshEndorsements();
            }
            else if (e.CommandName.CompareOrdinalIgnoreCase("_DeleteOwned") == 0 && !String.IsNullOrEmpty(e.CommandArgument.ToString()))
            {
                List<Endorsement> rgEndorsements = new List<Endorsement>(Endorsement.EndorsementsForUser(Page.User.Identity.Name, null));
                Endorsement en = rgEndorsements.FirstOrDefault(en2 => en2.ID == id);

                if (en == null)
                    throw new MyFlightbookException("Can't find endorsement with ID=" + id.ToString());

                if (en.StudentType == Endorsement.StudentTypes.External)
                    throw new MyFlightbookException("Can't delete external endorsement with ID=" + id.ToString());

                en.FDelete();
                RefreshEndorsements();
            }
        }
        catch (MyFlightbookException ex)
        {
            lblErr.Text = ex.Message;
        }
    }

    protected void lnkDownload_Click(object sender, EventArgs e)
    {
        gvDownload.DataSource = Endorsement.EndorsementsForUser(Student, Instructor);
        gvDownload.DataBind();
        Response.ContentType = "text/csv";
        // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
        string szFilename = String.Format(CultureInfo.CurrentCulture, "{0}-{1}-{2}", Branding.CurrentBrand.AppName, Resources.SignOff.DownloadEndorsementsFilename, String.IsNullOrEmpty(Student) ? Resources.SignOff.DownloadEndorsementsAllStudents : MyFlightbook.Profile.GetUser(Student).UserFullName);
        string szDisposition = String.Format(CultureInfo.InvariantCulture, "attachment;filename={0}.csv", Regex.Replace(szFilename, "[^0-9a-zA-Z-]", ""));
        Response.AddHeader("Content-Disposition", szDisposition);
        UTF8Encoding enc = new UTF8Encoding(true);    // to include the BOM
        Response.Write(Encoding.UTF8.GetString(enc.GetPreamble()));   // UTF-8 BOM
        Response.Write(gvDownload.CSVFromData());
        Response.End();
    }
}