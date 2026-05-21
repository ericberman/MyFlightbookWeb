using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Simple property type for fast/condensed serialization/database lookup
    /// Consists of just 3 things:
    /// a) property id
    /// b) property type id
    /// c) string representation of value.
    /// </summary>
    [Serializable]
    public class SimpleFlightProperty
    {
        #region properties
        public int PropID { get; set; } = CustomFlightProperty.idCustomFlightPropertyNew;
        public int PropTypeID { get; set; } = (int)CustomPropertyType.KnownProperties.IDPropInvalid;
        public string ValueString { get; set; }
        #endregion

        /// <summary>
        /// Converts a simpleflightproperty to a CustomFlightProperty
        /// </summary>
        /// <param name="idFlight">The ID of the flight to which the property is attached</param>
        /// <param name="ci">If provided, the culture to parse in.  If null, invariant culture is used.</param>
        /// <param name="defaultDate">The default date to use if a naked time is encountered</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="MyFlightbookException"></exception>
        public CustomFlightProperty ToProperty(int idFlight, CultureInfo ci, DateTime? defaultDate)
        {
            if (PropTypeID == (int)CustomPropertyType.KnownProperties.IDPropInvalid)
                throw new InvalidOperationException("No property type specified");

            CustomPropertyType cpt = CustomPropertyType.GetCustomPropertyType(PropTypeID) ?? throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Unable to find custom property type with idproptype {0}", PropTypeID));
            CustomFlightProperty cfp = new CustomFlightProperty(cpt) { PropID = this.PropID, FlightID = idFlight };

            ci = ci ?? CultureInfo.InvariantCulture;    // if null, use invariant culture
            // Set the value from the string
            if (!String.IsNullOrEmpty(ValueString))
            {
                switch (cpt.Type)
                {
                    case CFPPropertyType.cfpBoolean:
                        cfp.BoolValue = Convert.ToBoolean(ValueString, ci);
                        break;
                    case CFPPropertyType.cfpCurrency:
                    case CFPPropertyType.cfpDecimal:
                        cfp.DecValue = (ci != CultureInfo.InvariantCulture) ? ValueString.SafeParseDecimal() : Convert.ToDecimal(ValueString, CultureInfo.InvariantCulture);
                        break;
                    case CFPPropertyType.cfpDate:
                    case CFPPropertyType.cfpDateTime:
                        cfp.DateValue = ValueString.ParseUTCDateTime(defaultDate);
                        break;
                    case CFPPropertyType.cfpInteger:
                        cfp.IntValue = Convert.ToInt32(ValueString, ci);
                        break;
                    case CFPPropertyType.cfpString:
                        cfp.TextValue = cpt.IsAllCaps ? ValueString.ToUpper(ci) : ValueString;
                        break;
                }
            }

            return cfp;
        }
    }
}
