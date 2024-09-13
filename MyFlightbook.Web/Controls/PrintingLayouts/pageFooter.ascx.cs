using System;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2020-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/


namespace MyFlightbook.Printing
{
    public partial class pageFooter : UserControl
    {
        #region Properties
        private string m_userName = string.Empty;

        public string UserName
        {
            get { return m_userName; }
            set
            {
                m_userName = value;
                CurrentUser = Profile.GetUser(value);
                lblShowModified.Visible = CurrentUser.PreferenceExists(MFBConstants.keyTrackOriginal);
            }
        }

        protected string UserFullName
        {
            get { return String.IsNullOrEmpty(CurrentUser?.FirstName ?? string.Empty + CurrentUser?.LastName ?? string.Empty) ? string.Empty : String.Format(System.Globalization.CultureInfo.CurrentCulture, "[{0}]", CurrentUser.UserFullName); }
        }

        public int PageNum { get; set; }

        public int TotalPages { get; set; }

        public bool ShowFooter
        {
            get { return divpagefooter.Visible; }
            set { divpagefooter.Visible = value; }
        }

        protected Profile CurrentUser { get; set; }

        [TemplateContainer(typeof(NotesTemplate)), PersistenceMode(PersistenceMode.InnerDefaultProperty), TemplateInstance(TemplateInstance.Single)]
        public ITemplate LayoutNotes { get; set; }
        #endregion

        protected override void OnInit(EventArgs e)
        {
            LayoutNotes?.InstantiateIn(plcMiddle);
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
}