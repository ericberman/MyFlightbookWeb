using MyFlightbook.Checklists;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_ChecklistControls_ChecklistItem : System.Web.UI.UserControl
{
    #region Properties
    private ChecklistRow m_checklistRow;

    public ChecklistRow DataItem
    {
        get { return m_checklistRow; }
        set
        {
            m_checklistRow = value;
            Bind();
        }
    }

    protected bool NoHeader { get; set; }

    protected string CssClassForContentStyle(ContentStyle style)
    {
        switch (style)
        {
            default:
            case ContentStyle.Normal:
                return "checklistContentStyleNormal";
            case ContentStyle.Emphasis:
                return "checklistContentStyleEmphasis";
            case ContentStyle.Emergency:
                return "checklistContentStyleEmergency";
        }
    }
    #endregion

    protected void Bind()
    {
        if (m_checklistRow == null)
            return;

        ContainerRow containerRow = m_checklistRow as ContainerRow;
        ContentRow contentRow = m_checklistRow as ContentRow;
        CheckboxRow checkboxRow = m_checklistRow as CheckboxRow;

        // Container rows are subclasses of contentrow, so check for contentrow last.
        if (containerRow != null)
        {
            mvItem.SetActiveView(vwRepeater);
            if (!NoHeader)
                lblHeader.Text = HttpUtility.HtmlEncode(m_checklistRow.Content ?? string.Empty);

            List<ChecklistRow> contents = new List<ChecklistRow>();
            List<ChecklistRow> tabs = new List<ChecklistRow>();
            List<ChecklistRow> headers = new List<ChecklistRow>();
            List<ChecklistRow> subheaders = new List<ChecklistRow>();


            foreach (ChecklistRow ckl in containerRow.ContainedItems)
            {
                if (ckl is TabContainer)
                    tabs.Add(ckl);
                else if (ckl is HeaderContainer)
                    headers.Add(ckl);
                else if (ckl is SubHeaderContainer)
                    subheaders.Add(ckl);
                else
                    contents.Add(ckl);
            }

            // OK.  First bind the leaf content nodes - plain text and checkbox items.
            rptRows.DataSource = contents;
            rptRows.DataBind();

            // Now bind subheaders
            if (subheaders.Count != 0)
            {
                rptSubHeaders.DataSource = subheaders;
                rptSubHeaders.DataBind();
            }

            // Now bind the header rows:
            accordionRows.DataSource = headers;
            accordionRows.DataBind();

            // Finally, do any tabs
            for (int i = 0; i < tabs.Count; i++)
            {
                ChecklistRow tabRow = tabs[i];
                AjaxControlToolkit.TabPanel tp = new AjaxControlToolkit.TabPanel();
                tabRows.Tabs.Add(tp);
                tp.HeaderText = HttpUtility.HtmlEncode(tabRow.Content);
                tp.ID = String.Format(CultureInfo.InvariantCulture, "cklTabItem{0}", i);
                Controls_ChecklistControls_ChecklistItem ckli = (Controls_ChecklistControls_ChecklistItem)LoadControl("~/Controls/ChecklistControls/ChecklistItem.ascx");
                tp.Controls.Add(ckli);
                ckli.NoHeader = true;
                ckli.DataItem = tabRow;
            }
        }
        else if (checkboxRow != null || contentRow != null)
        {
            rptRows.DataSource = new ChecklistRow[] { m_checklistRow };
            rptRows.DataBind();
        }
    }

    protected void accordion_ItemDataBound(object sender, AjaxControlToolkit.AccordionItemEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        if (e.ItemType == AjaxControlToolkit.AccordionItemType.Content)
        {
            Controls_ChecklistControls_ChecklistItem ckli = (Controls_ChecklistControls_ChecklistItem)LoadControl("~/Controls/ChecklistControls/ChecklistItem.ascx");
            e.AccordionItem.Controls.Add(ckli);
            ckli.NoHeader = true;
            ckli.DataItem = ((ContainerRow)e.AccordionItem.DataItem);
            }
    }

    private const string szJScriptClickHandlers =
@"function clickRow(e, sender) {
        var target = document.getElementById(sender.getAttribute(""associatedcheck""));
        target.checked = !target.checked;
        if (target.onchange != null)
            target.onchange(e, target);
    }
       
    function onCheckClick(e) {
        var e = window.event || e;
        e.cancelBubble = true;
        e.stopPropagation();
}

    function startEngine(e, sender) {
        if (sender.checked)
            window.alert('Start Engine');
    }

    function stopEngine(e, sender) {
        if (sender.checked)
            window.alert('Stop Engine');
    }

    function blockOut(e, sender) {
        if (sender.checked)
            window.alert('Block Out');
    }

    function blockIn(e, sender) {
        if (sender.checked)
            window.alert('Block In');
    }
";

    protected void Page_Load(object sender, EventArgs e)
    {
        Page.ClientScript.RegisterClientScriptBlock(GetType(), "rowClick", szJScriptClickHandlers, true);
    }

    protected void rptSubHeaders_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        Controls_ChecklistControls_ChecklistItem ckli = (Controls_ChecklistControls_ChecklistItem)LoadControl("~/Controls/ChecklistControls/ChecklistItem.ascx");
        e.Item.Controls.Add(ckli);
        ckli.DataItem = (ChecklistRow)e.Item.DataItem;
    }

    protected void rptRows_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        HtmlTableRow tr = (HtmlTableRow)e.Item.FindControl("rowCheckItem");
        CheckBox ck = (CheckBox)e.Item.FindControl("ckItem");
        tr.Attributes["associatedcheck"] = ck.ClientID;

        ChecklistRow ckr = ((ChecklistRow)e.Item.DataItem);
        CheckboxRow ckb = ckr as CheckboxRow;
        if (ckb != null)
        {
            switch (ckb.Action)
            {
                case CheckboxAction.None:
                    break;
                case CheckboxAction.StartEngine:
                    ck.InputAttributes["onchange"] = "startEngine(event,this);";
                    break;
                case CheckboxAction.StopEngine:
                    ck.InputAttributes["onchange"] = "stopEngine(event,this);";
                    break;
                case CheckboxAction.BlockOut:
                    ck.InputAttributes["onchange"] = "blockOut(event,this);";
                    break;
                case CheckboxAction.BlockIn:
                    ck.InputAttributes["onchange"] = "blockIn(event,this);";
                    break;
            }
        }
    }
}