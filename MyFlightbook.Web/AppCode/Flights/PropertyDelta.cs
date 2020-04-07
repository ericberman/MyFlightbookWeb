using System;
using System.Collections.Generic;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2008-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    [Serializable]
    public class PropertyDelta : IComparable, IEquatable<PropertyDelta>
    {
        public enum ChangeType { Unchanged, Added, Deleted, Modified }

        #region Properties
        /// <summary>
        /// The display name of the property/field that was changed
        /// </summary>
        public string PropName { get; set; }

        /// <summary>
        /// The old value for the property/field, null or empty if it wasn't previously present (has been added)
        /// </summary>
        public string OldValue { get; set; }

        /// <summary>
        /// The new value for the property/field, null or empty if it has been deleted
        /// </summary>
        public string NewValue { get; set; }

        public ChangeType Change
        {
            get
            {
                if (String.IsNullOrEmpty(OldValue) && String.IsNullOrEmpty(NewValue))
                    return ChangeType.Unchanged;
                else if (String.IsNullOrEmpty(OldValue) && !String.IsNullOrEmpty(NewValue))
                    return ChangeType.Added;
                else if (String.IsNullOrEmpty(NewValue) && !String.IsNullOrEmpty(OldValue))
                    return ChangeType.Deleted;
                else if (NewValue.Trim().CompareCurrentCulture(OldValue.Trim()) == 0)
                    return ChangeType.Unchanged;
                else
                    return ChangeType.Modified;
            }
        }
        #endregion

        #region Constructors
        public PropertyDelta()
        {
            PropName = string.Empty;
        }

        public PropertyDelta(string name, string oldVal, string newVal) : this()
        {
            PropName = name;
            OldValue = oldVal;
            NewValue = newVal;
        }
        #endregion

        public static void AddPotentialChange(string name, string oldVal, string newVal, IList<PropertyDelta> lst)
        {
            if (lst == null)
                throw new ArgumentNullException(nameof(lst));
            if (String.IsNullOrEmpty(name))
                throw new ArgumentOutOfRangeException(nameof(name));

            PropertyDelta pd = new PropertyDelta(name, oldVal, newVal);
            if (pd.Change != ChangeType.Unchanged)
                lst.Add(pd);
        }

        public override string ToString()
        {
            switch (Change)
            {
                default:
                case ChangeType.Unchanged:
                    return string.Empty;
                case ChangeType.Added:
                    return String.Format(CultureInfo.CurrentCulture, "{0}: {1} ({2})", Resources.LogbookEntry.CompareAdded, PropName, NewValue);
                case ChangeType.Deleted:
                    return String.Format(CultureInfo.CurrentCulture, "{0}: {1} ({2})", Resources.LogbookEntry.CompareDeleted, PropName, OldValue);
                case ChangeType.Modified:
                    return String.Format(CultureInfo.CurrentCulture, "{0}: {1}: {2} ==> {3}", Resources.LogbookEntry.CompareModified, PropName, OldValue, NewValue);
            }
        }

        #region IComparable
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            PropertyDelta pd = obj as PropertyDelta;

            if (pd.Change == Change)
                return PropName.CompareCurrentCultureIgnoreCase(pd.PropName);
            else
                return Change.CompareTo(pd.Change);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            PropertyDelta pd = obj as PropertyDelta;
            if (pd == null)
                return false;

            return Equals(pd);
        }

        public bool Equals(PropertyDelta other)
        {
            return other != null &&
                   PropName == other.PropName &&
                   OldValue == other.OldValue &&
                   NewValue == other.NewValue;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 31089781;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PropName);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(OldValue);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(NewValue);
                return hashCode;
            }
        }

        public static bool operator ==(PropertyDelta left, PropertyDelta right)
        {
            return EqualityComparer<PropertyDelta>.Default.Equals(left, right);
        }

        public static bool operator !=(PropertyDelta left, PropertyDelta right)
        {
            return !(left == right);
        }

        public static bool operator <(PropertyDelta left, PropertyDelta right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(PropertyDelta left, PropertyDelta right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(PropertyDelta left, PropertyDelta right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(PropertyDelta left, PropertyDelta right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion
    }
}

