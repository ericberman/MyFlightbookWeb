using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;

public partial class Controls_PrintingLayouts_pageFooter : System.Web.UI.UserControl
{
    #region Properties
    public string UserName { get; set; }

    public int PageNum { get; set; }

    public int TotalPages { get; set; }

    public bool ShowFooter
    {
        get { return pnlPageCount.Visible; }
        set { pnlPageCount.Visible = lblCertification.Visible = value; }
    }

    private Profile m_user = null;
    protected Profile CurrentUser
    {
        get
        {
            if (m_user == null)
                m_user = MyFlightbook.Profile.GetUser(UserName);
            return m_user;
        }
    }

    [TemplateContainer(typeof(NotesTemplate)), PersistenceMode(PersistenceMode.InnerDefaultProperty), TemplateInstance(TemplateInstance.Single)]
    public ITemplate LayoutNotes { get; set; }
    #endregion

    protected override void OnInit(EventArgs e)
    {
        if (LayoutNotes != null)
            LayoutNotes.InstantiateIn(plcMiddle);
        base.OnInit(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected class NotesTemplate : Control, INamingContainer
    {
        public NotesTemplate()
        {
        }
    }
}