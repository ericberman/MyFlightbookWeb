﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2019-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Templates
{
    public partial class mfbSelectTemplates : UserControl
    {
        public event EventHandler<PropertyTemplateEventArgs> TemplateSelected;
        public event EventHandler<PropertyTemplateEventArgs> TemplateUnselected;
        public event EventHandler<EventArgs> TemplatesReady;

        private const string szvsActive = "vsActiveTemplates";

        #region Properties
        /// <summary>
        /// The set of templates that are currently selected
        /// </summary>
        protected HashSet<int> ActiveTemplates
        {
            get
            {
                if (ViewState[szvsActive] == null)
                    ViewState[szvsActive] = new HashSet<int>();
                return (HashSet<int>)ViewState[szvsActive];
            }
        }

        /// <summary>
        /// Template for script to run (suppresses postback).  MUST be a format string with {0} for the id of the template and {1} for the client ID of the checkbox that was clicked
        /// </summary>
        public string ToggleClientScript { get; set; } = string.Empty;

        /// <summary>
        /// The templates to display
        /// </summary>
        public IEnumerable<TemplateCollection> GroupedTemplates { get; set; }

        protected IEnumerable<PropertyTemplate> UserTemplates { get; set; }

        private bool m_includeAutoTemplates = true;
        /// <summary>
        /// If true (default), automatic templates are included for selection
        /// </summary>
        public bool IncludeAutomaticTemplates
        {
            get { return m_includeAutoTemplates; }
            set { m_includeAutoTemplates = value; }
        }
        #endregion

        public void AddTemplate(int id)
        {
            ActiveTemplates.Add(id);
        }

        public void AddTemplates(IEnumerable<PropertyTemplate> rgpt)
        {
            if (rgpt == null)
                return;
            foreach (PropertyTemplate pt in rgpt)
                ActiveTemplates.Add(pt.ID);
        }

        public void RemoveTemplate(int id)
        {
            ActiveTemplates.Remove(id);
        }

        public void RemoveAllTemplates()
        {
            ActiveTemplates.Clear();
        }

        public void Refresh()
        {
            rptGroups.DataSource = GroupedTemplates;
            rptGroups.DataBind();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            UserTemplates = UserPropertyTemplate.TemplatesForUser(Page.User.Identity.Name, IncludeAutomaticTemplates);
            GroupedTemplates = TemplateCollection.GroupTemplates(UserTemplates);

            // No viewstate - refresh every time.
            Refresh();
            TemplatesReady?.Invoke(this, new EventArgs());
        }

        protected void ckActive_CheckedChanged(object sender, EventArgs e)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));

            CheckBox ck = sender as CheckBox;

            HiddenField h = (HiddenField)ck.NamingContainer.FindControl("hdnID");

            int id = Convert.ToInt32(h.Value, CultureInfo.InvariantCulture);

            if (ck.Checked)
            {
                ActiveTemplates.Add(id);
                Refresh();
                TemplateSelected?.Invoke(this, new PropertyTemplateEventArgs(UserTemplates.FirstOrDefault(pt => pt.ID == id)));
            }
            else
            {
                ActiveTemplates.Remove(id);
                Refresh();
                TemplateUnselected?.Invoke(this, new PropertyTemplateEventArgs(id));
            }
        }

        protected void rptTemplates_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            if (!String.IsNullOrEmpty(ToggleClientScript))
            {
                CheckBox ck = e.Item.FindControl("ckActive") as CheckBox;
                PropertyTemplate pt = e.Item.DataItem as PropertyTemplate;

                ck.AutoPostBack = false;
                ck.CheckedChanged -= new EventHandler(ckActive_CheckedChanged);
                ck.Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:{0};", String.Format(CultureInfo.InvariantCulture, ToggleClientScript, pt.ID, ck.ClientID));
            }
        }
    }
}