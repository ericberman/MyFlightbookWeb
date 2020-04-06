using System;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2008-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.WizardPage
{
    /// <summary>
    /// Implements a page with a wizard styled for MyFlightbook - steps on top.
    /// To use, you MUST subclass this page.
    /// The wizard MUST also contain a header with a repeater with an ID of "SideBarList" in it that binds to Container.DataItem, like below
    /// Finally, you MUST add this line to Page_Load:
    /// InitWizard(wz);
    /// where "wz" is the wizard control to use
    /// </summary>
    /*
       <HeaderTemplate>
            <div style="width:100%">
                <asp:Repeater ID="SideBarList" runat="server">
                    <ItemTemplate>
                        <span class="<%# GetClassForWizardStep(Container.DataItem) %>">
                            &nbsp;
                            <%# DataBinder.Eval(Container, "DataItem.Title") %>
                        </span>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </HeaderTemplate>
    */
    public class MFBWizardPage : System.Web.UI.Page
    {
        // Thanks to http://weblogs.asp.net/grantbarrington/archive/2009/08/11/styling-the-asp-net-wizard-control-to-have-the-steps-across-the-top.aspx for how to do this.
        protected void mfbwizard_PreRender(object sender, EventArgs e)
        {
            Repeater SideBarList = StyledWizard.FindControl("HeaderContainer").FindControl("SideBarList") as Repeater;

            SideBarList.DataSource = StyledWizard.WizardSteps;
            SideBarList.DataBind();
        }

        public string GetClassForWizardStep(object wizardStep)
        {
            if (!(wizardStep is WizardStep step))
                return string.Empty;

            if (StyledWizard == null)
                throw new InvalidOperationException("InitWizard was not called");

            int stepIndex = StyledWizard.WizardSteps.IndexOf(step);

            if (stepIndex < StyledWizard.ActiveStepIndex)
                return "wizStepCompleted";
            else if (stepIndex > StyledWizard.ActiveStepIndex)
                return "wizStepFuture";
            else
                return "wizStepInProgress";
        }

        protected Wizard StyledWizard { get; set; }

        protected void InitWizard(Wizard wz)
        {
            StyledWizard = wz ?? throw new ArgumentNullException(nameof(wz));

            wz.PreRender += new EventHandler(mfbwizard_PreRender);
        }
    }
}