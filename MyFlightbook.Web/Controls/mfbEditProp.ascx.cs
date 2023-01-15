using System;
using System.Globalization;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2013-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Controls.FlightEditing
{
    public partial class mfbEditProp : UserControl
    {
        private CustomFlightProperty m_fp;

        #region properties
        public string Username { get; set; }

        /// <summary>
        /// The flight property being edited
        /// </summary>
        public CustomFlightProperty FlightProperty
        {
            get
            {
                FromForm();
                return m_fp;
            }
            set
            {
                m_fp = value;
                ToForm();
            }
        }

        /// <summary>
        /// If this property supports cross-fill, the details are provided here.
        /// </summary>
        public CrossFillDescriptor CrossFillDescriptor { get; set; }
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void FromForm()
        {
            CustomFlightProperty fp = m_fp;
            switch (fp.PropertyType.Type)
            {
                case CFPPropertyType.cfpBoolean:
                    fp.BoolValue = ckValue.Checked;
                    break;
                case CFPPropertyType.cfpInteger:
                    fp.IntValue = mfbDecEdit.IntValue;
                    break;
                case CFPPropertyType.cfpDecimal:
                case CFPPropertyType.cfpCurrency:
                    fp.DecValue = mfbDecEdit.Value;
                    break;
                case CFPPropertyType.cfpDate:
                    fp.DateValue = mfbTypeInDate.Date;
                    break;
                case CFPPropertyType.cfpDateTime:
                    fp.DateValue = mfbDateTime.DateAndTime;
                    break;
                case CFPPropertyType.cfpString:
                    fp.TextValue = txtString.Text;
                    break;
                default:
                    throw new MyFlightbookException(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Unknown property type: {0}", fp.PropertyType.Type));
            }
        }

        protected void ToForm()
        {
            CustomFlightProperty fp = m_fp;

            TimeZoneInfo tz = Profile.GetUser(Page.User.Identity.Name).PreferredTimeZone;
            lblPropName.Text = (fp.PropertyType.Type == CFPPropertyType.cfpDateTime) ? fp.PropertyType.Title.IndicateUTCOrCustomTimeZone(tz) : fp.PropertyType.Title;
            lblPropName.ToolTip = (fp.PropertyType.Type == CFPPropertyType.cfpDateTime && tz.Id.CompareCurrentCultureIgnoreCase(TimeZoneInfo.Utc.Id) != 0) ? tz.DisplayName : string.Empty;
            mfbTooltip.Visible = !String.IsNullOrEmpty(mfbTooltip.BodyContent = fp.PropertyType.Type == CFPPropertyType.cfpDateTime ? fp.PropertyType.Description.Replace("(UTC)", tz.DisplayName) : fp.PropertyType.Description);

            if (CrossFillDescriptor!= null)
            {
                mfbDecEdit.CrossFillScript = CrossFillDescriptor.ScriptRef;
                mfbDecEdit.CrossFillTip = CrossFillDescriptor.Tip;
            }

            switch (fp.PropertyType.Type)
            {
                case CFPPropertyType.cfpBoolean:
                    {
                        lblPropName.AssociatedControlID = ckValue.ID;
                        ckValue.Checked = fp.BoolValue;
                        mvProp.SetActiveView(vwBool);
                    }
                    break;
                case CFPPropertyType.cfpInteger:
                    mfbDecEdit.IntValue = fp.IntValue;
                    mfbDecEdit.EditingMode = Controls_mfbDecimalEdit.EditMode.Integer;
                    mvProp.SetActiveView(vwDecimal);
                    break;
                case CFPPropertyType.cfpDecimal:
                    mvProp.SetActiveView(vwDecimal);    // need to do this before setting the cross-fill image to visible
                    // Set the cross-fill source before setting the editing mode.
                    mfbDecEdit.EditingMode = (!fp.PropertyType.IsBasicDecimal && Profile.GetUser(Page.User.Identity.Name).UsesHHMM ? Controls_mfbDecimalEdit.EditMode.HHMMFormat : Controls_mfbDecimalEdit.EditMode.Decimal);
                    mfbDecEdit.Value = fp.DecValue;
                    break;
                case CFPPropertyType.cfpCurrency:
                    mfbDecEdit.EditingMode = Controls_mfbDecimalEdit.EditMode.Currency;
                    mfbDecEdit.Value = fp.DecValue;
                    mvProp.SetActiveView(vwDecimal);
                    break;
                case CFPPropertyType.cfpDate:
                    mfbTypeInDate.Date = fp.DateValue;
                    mvProp.SetActiveView(vwDate);
                    break;
                case CFPPropertyType.cfpDateTime:
                    mfbDateTime.DateAndTime = fp.DateValue;
                    mvProp.SetActiveView(vwDateTime);
                    break;
                case CFPPropertyType.cfpString:
                    txtString.Text = fp.TextValue;
                    mvProp.SetActiveView(vwText);
                    // Autocomplete uses the target user's previous values.
                    autocompleteStringProp.ContextKey = String.Format(CultureInfo.InvariantCulture, "{0};{1}", Username ?? Page.User.Identity.Name, fp.PropTypeID.ToString(CultureInfo.InvariantCulture));
                    break;
                default:
                    throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Unknown property type: {0}", fp.PropertyType.Type));
            }
        }
    }
}