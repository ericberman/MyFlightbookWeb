using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;

/******************************************************
 * 
 * Copyright (c) 2007-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    /// <summary>
    /// Represents a given state of currency for a given attribute; fairly generic
    /// </summary>
    [Serializable]
    [DataContract]
    public abstract class CurrencyStatusItemBase : IComparable, IEquatable<CurrencyStatusItemBase>
    {
        public enum CurrencyGroups { None, FlightExperience, FlightReview, Aircraft, AircraftDeadline, Certificates, Medical, Deadline, CustomCurrency }

        #region properties
        /// <summary>
        /// The specific currency attribute (e.g., "Instrument flight", "BFR Due," or "VOR Check"
        /// </summary>
        [DataMember]
        public string Attribute { get; set; }

        /// <summary>
        /// The value or description of the state
        /// </summary>
        [DataMember]
        public string Value { get; set; }

        /// <summary>
        /// Everything OK?  Expired?  Close to expiration?
        /// </summary>
        [DataMember]
        public CurrencyState Status { get; set; }

        /// <summary>
        /// What is the gap between current state and some bad state (e.g., how long ago did it expire?  How soon will an inspection be due?
        /// </summary>
        [DataMember]
        public string Discrepancy { get; set; }

        /// <summary>
        /// The ID of the resource to which this is linked (typically aircraft)
        /// </summary>
        [DataMember]
        public int AssociatedResourceID { get; set; }

        /// <summary>
        /// The kind of resource to which this 
        /// </summary>
        [DataMember]
        public CurrencyGroups CurrencyGroup { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a currency status item in-place
        /// </summary>
        public CurrencyStatusItemBase()
        {
            Attribute = Value = Discrepancy = string.Empty;
            Status = CurrencyState.OK;
            AssociatedResourceID = 0;
            CurrencyGroup = CurrencyGroups.None;
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} - {1} ({2}, {3})", Attribute, Value, Status.ToString(), Discrepancy);
        }

        #region IComparable
        /// <summary>
        /// When sorting, we can group custom currencies together and we can group aircraft deadlines with aircraft maintenance
        /// </summary>
        /// <param name="cg"></param>
        /// <returns></returns>
        protected static int GroupSortBucket(CurrencyGroups cg)
        {
            switch (cg)
            {
                case CurrencyGroups.None:
                    return 0;
                case CurrencyGroups.FlightExperience:
                case CurrencyGroups.CustomCurrency:
                    return 1;
                case CurrencyGroups.Aircraft:
                case CurrencyGroups.AircraftDeadline:
                    return 2;
                case CurrencyGroups.FlightReview:
                    return 3;
                case CurrencyGroups.Certificates:
                    return 4;
                case CurrencyGroups.Medical:
                    return 5;
                case CurrencyGroups.Deadline:
                    return 6;
                default:
                    return (int)cg;
            }
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (!(obj is CurrencyStatusItemBase csi))
                throw new InvalidCastException("obj is not currencystatusitem, it is " + obj.GetType().ToString());

            int gspThis = GroupSortBucket(CurrencyGroup);
            int gspThat = GroupSortBucket(csi.CurrencyGroup);
            return gspThis.CompareTo(gspThat);  // don't subsort on name because the ordering of that is fine as is.
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CurrencyStatusItemBase);
        }

        public bool Equals(CurrencyStatusItemBase other)
        {
            return other != null &&
                   Attribute == other.Attribute &&
                   Value == other.Value &&
                   Status == other.Status &&
                   Discrepancy == other.Discrepancy &&
                   CurrencyGroup == other.CurrencyGroup;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = -1537879147;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Attribute);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Value);
                hashCode = hashCode * -1521134295 + Status.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Discrepancy);
                hashCode = hashCode * -1521134295 + CurrencyGroup.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(CurrencyStatusItemBase left, CurrencyStatusItemBase right)
        {
            return EqualityComparer<CurrencyStatusItemBase>.Default.Equals(left, right);
        }

        public static bool operator !=(CurrencyStatusItemBase left, CurrencyStatusItemBase right)
        {
            return !(left == right);
        }

        public static bool operator <(CurrencyStatusItemBase left, CurrencyStatusItemBase right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(CurrencyStatusItemBase left, CurrencyStatusItemBase right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(CurrencyStatusItemBase left, CurrencyStatusItemBase right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(CurrencyStatusItemBase left, CurrencyStatusItemBase right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion
    }
}