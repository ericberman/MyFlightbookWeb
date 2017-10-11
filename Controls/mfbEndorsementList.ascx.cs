using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;
using MyFlightbook.Instruction;

/******************************************************
 * 
 * Copyright (c) 2010-2016 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbEndorsementList : System.Web.UI.UserControl
{
    private string m_szStudent = string.Empty;
    private string m_szInstructor = string.Empty;

    /// <summary>
    /// The name of the student to restrict on
    /// </summary>
    public string Student
    {
        get { return m_szStudent; }
        set { m_szStudent = value;  }
    }

    /// <summary>
    /// The name of the instructor to restrict on.
    /// </summary>
    public string Instructor
    {
        get { return m_szInstructor; }
        set { m_szInstructor = value;  }
    }

    public int RefreshEndorsements()
    {
        IEnumerable<Endorsement> rg = Endorsement.EndorsementsForUser(m_szStudent, m_szInstructor);
        gvExistingEndorsements.DataSource = rg;
        gvExistingEndorsements.DataBind();
        int cItems = rg.Count();
        lblPreviousEndorsements.Visible = (cItems > 0);
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
        if (e != null && e.CommandName.CompareOrdinalIgnoreCase("_Delete") == 0 && !String.IsNullOrEmpty(e.CommandArgument.ToString()))
        {

            try
            {
                List<Endorsement> rgEndorsements = new List<Endorsement>(Endorsement.EndorsementsForUser(null, Page.User.Identity.Name));
                int id = Convert.ToInt32(e.CommandArgument, System.Globalization.CultureInfo.InvariantCulture);

                if (id <= 0)
                    throw new MyFlightbookException("Invalid endorsement ID to delete");

                Endorsement en = rgEndorsements.FirstOrDefault<Endorsement>(en2 => en2.ID == id);
                if (en == null)
                    throw new MyFlightbookException("ID of endorsement to delete is not found in owners endorsements");

                if (en.StudentType == Endorsement.StudentTypes.Member)
                    throw new MyFlightbookException(Resources.SignOff.errCantDeleteMemberEndorsement);

                en.FDelete();
                RefreshEndorsements();
            }
            catch (MyFlightbookException ex)
            {
                lblErr.Text = ex.Message;
            }
        }
    }

    protected void lnkDownload_Click(object sender, EventArgs e)
    {
        gvDownload.DataSource = Endorsement.EndorsementsForUser(m_szStudent, m_szInstructor);
        gvDownload.DataBind();
        Response.ContentType = "text/csv";
        // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
        string szFilename = String.Format(CultureInfo.CurrentCulture, "{0}-{1}-{2}", Branding.CurrentBrand.AppName, Resources.SignOff.DownloadEndorsementsFilename, String.IsNullOrEmpty(m_szStudent) ? Resources.SignOff.DownloadEndorsementsAllStudents : MyFlightbook.Profile.GetUser(m_szStudent).UserFullName);
        string szDisposition = String.Format(CultureInfo.InvariantCulture, "attachment;filename={0}.csv", Regex.Replace(szFilename, "[^0-9a-zA-Z-]", ""));
        Response.AddHeader("Content-Disposition", szDisposition);
        UTF8Encoding enc = new UTF8Encoding(true);    // to include the BOM
        Response.Write(Encoding.UTF8.GetString(enc.GetPreamble()));   // UTF-8 BOM
        Response.Write(gvDownload.CSVFromData());
        Response.End();
    }
}