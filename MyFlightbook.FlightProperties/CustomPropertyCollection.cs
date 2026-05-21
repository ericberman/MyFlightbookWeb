using System;
using System.Collections;
using System.Collections.Generic;

/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    [Serializable]
    public class CustomPropertyCollection : ICollection<CustomFlightProperty>
    {
        #region Properties
        private readonly Dictionary<int, CustomFlightProperty> m_dictProps;

        public int Count
        {
            get { return m_dictProps.Count; }
        }

        bool ICollection<CustomFlightProperty>.IsReadOnly => false;
        #endregion

        #region Constructors
        public CustomPropertyCollection()
        {
            m_dictProps = new Dictionary<int, CustomFlightProperty>();
        }

        /// <summary>
        /// Creates a property collection initialized from the specified properties.  Null and default values are ignored, the last of any duplicate will survive.
        /// </summary>
        /// <param name="rgcfp">The inbound set of properties</param>
        /// <param name="fIncludeExisting">If true, includes default values if they are existing properties (so that they can be deleted later).</param>
        public CustomPropertyCollection(IEnumerable<CustomFlightProperty> rgcfp, bool fIncludeExisting = false) : this()
        {
            if (rgcfp == null)
                return;
            foreach (CustomFlightProperty cfp in rgcfp)
                if (cfp != null && (!cfp.IsDefaultValue || (fIncludeExisting && cfp.IsExisting)))
                    m_dictProps[cfp.PropTypeID] = cfp;
        }
        #endregion

        #region ICollection
        public IEnumerator<CustomFlightProperty> GetEnumerator()
        {
            return m_dictProps.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_dictProps.Values.GetEnumerator();
        }

        bool ICollection<CustomFlightProperty>.Contains(CustomFlightProperty item)
        {
            return m_dictProps.ContainsKey(item.PropTypeID);
        }

        void ICollection<CustomFlightProperty>.CopyTo(CustomFlightProperty[] array, int arrayIndex)
        {
            m_dictProps.Values.CopyTo(array, arrayIndex);
        }

        bool ICollection<CustomFlightProperty>.Remove(CustomFlightProperty item)
        {
            return RemoveItem(item.PropTypeID);
        }

        /// <summary>
        /// Adds the specified item, replacing it if it already exists.  If null, it will be ignored.  If default value, it will be removed.
        /// </summary>
        /// <param name="item"></param>
        public void Add(CustomFlightProperty item)
        {
            if (item == null)
                return;
            m_dictProps[item.PropTypeID] = item;
            if (item.IsDefaultValue)
                m_dictProps.Remove(item.PropTypeID);
        }
        #endregion

        #region Helper Utilities
        /// <summary>
        /// Convenience to iterate over the events
        /// </summary>
        /// <param name="a"></param>
        public void ForEachEvent(Action<CustomFlightProperty> a)
        {
            if (a == null)
                return;
            foreach (CustomFlightProperty cfp in m_dictProps.Values)
                a(cfp);
        }

        /// <summary>
        /// Find an event with the specified propertytype
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public CustomFlightProperty GetEventWithTypeID(CustomPropertyType.KnownProperties id)
        {
            return GetEventWithTypeID((int)id);
        }

        public CustomFlightProperty GetEventWithTypeID(int id)
        {
            return m_dictProps.TryGetValue(id, out CustomFlightProperty cfp) ? cfp : null;
        }

        public CustomFlightProperty this[int id]
        {
            get { return GetEventWithTypeID(id); }
        }

        public CustomFlightProperty this[CustomPropertyType.KnownProperties id]
        {
            get { return GetEventWithTypeID(id); }
        }

        public CustomFlightProperty GetEventWithTypeIDOrNew(CustomPropertyType.KnownProperties id)
        {
            return GetEventWithTypeIDOrNew((int)id);
        }

        public CustomFlightProperty GetEventWithTypeIDOrNew(int id)
        {
            return GetEventWithTypeID(id) ?? new CustomFlightProperty(CustomPropertyType.GetCustomPropertyType(id));
        }

        /// <summary>
        /// Check if a given property exists for this flight
        /// </summary>
        /// <param name="id">The property type ID</param>
        /// <returns>True if it exists.</returns>
        public bool PropertyExistsWithID(CustomPropertyType.KnownProperties id)
        {
            return m_dictProps.ContainsKey((int)id);
        }

        /// <summary>
        /// Returns the first (if any) event that matches the specified predicate
        /// </summary>
        /// <param name="p">The predicate</param>
        /// <returns>First matching hit, or null</returns>
        public CustomFlightProperty FindEvent(Predicate<CustomFlightProperty> p)
        {
            if (p == null)
                throw new ArgumentNullException(nameof(p));
            foreach (CustomFlightProperty cfp in m_dictProps.Values)
                if (p(cfp))
                    return cfp;
            return null;
        }

        /// <summary>
        /// Get the time for a given property (e.g., solo time), 0 if not present
        /// </summary>
        /// <param name="id">The known property</param>
        /// <returns>The time, 0 if not present</returns>
        public decimal TimeForProperty(CustomPropertyType.KnownProperties id)
        {
            CustomFlightProperty cfp = GetEventWithTypeID(id);
            return cfp == null ? 0.0M : cfp.DecValue;
        }

        /// <summary>
        /// Returns the sum of decimal values for properties matching the specified predicate
        /// </summary>
        /// <param name="p">The predicate</param>
        /// <returns>The sum of decvalues.</returns>
        public decimal TotalTimeForPredicate(Predicate<CustomFlightProperty> p)
        {
            if (p == null)
                throw new ArgumentNullException(nameof(p));
            decimal d = 0.0M;
            foreach (CustomFlightProperty cfp in m_dictProps.Values)
                if (p(cfp))
                    d += cfp.DecValue;
            return d;
        }

        /// <summary>
        /// Returns the sum of integer values for properties matching the specified predicate
        /// </summary>
        /// <param name="p">The predicate</param>
        /// <returns>The sum of decvalues.</returns>
        public int TotalCountForPredicate(Predicate<CustomFlightProperty> p)
        {
            if (p == null)
                throw new ArgumentNullException(nameof(p));
            int i = 0;
            foreach (CustomFlightProperty cfp in m_dictProps.Values)
                if (p(cfp))
                    i += cfp.IntValue;
            return i;
        }

        /// <summary>
        /// Safely return the string value for a string property
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string StringValueForProperty(CustomPropertyType.KnownProperties id)
        {
            CustomFlightProperty cfp = GetEventWithTypeID(id);
            return (cfp == null) ? string.Empty : cfp.TextValue;
        }

        /// <summary>
        /// Safely return the Integer value for a count property
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int IntValueForProperty(CustomPropertyType.KnownProperties id)
        {
            CustomFlightProperty cfp = GetEventWithTypeID(id);
            return (cfp == null) ? 0 : cfp.IntValue;
        }

        /// <summary>
        /// Safely return the decimal value for a decimalproperty
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public decimal DecimalValueForProperty(CustomPropertyType.KnownProperties id)
        {
            CustomFlightProperty cfp = GetEventWithTypeID(id);
            return (cfp == null) ? 0.0M : cfp.DecValue;
        }
        #endregion

        #region Adding/removing items
        /// <summary>
        /// Adds the specified fliht properties.  Default properties are stripped.
        /// </summary>
        /// <param name="rgcfp"></param>
        public void AddItems(IEnumerable<CustomFlightProperty> rgcfp)
        {
            if (rgcfp == null)
                throw new ArgumentNullException(nameof(rgcfp));
            foreach (CustomFlightProperty cfp in rgcfp)
                Add(cfp);
        }

        /// <summary>
        /// Removes all items from the collection
        /// </summary>
        public void Clear()
        {
            m_dictProps.Clear();
        }

        /// <summary>
        /// Replaces the flight properties with the specified items. Default items will be skipped.
        /// </summary>
        /// <param name="rgcfp"></param>
        public void SetItems(IEnumerable<CustomFlightProperty> rgcfp)
        {
            if (rgcfp == null)
                throw new ArgumentNullException(nameof(rgcfp));
            Clear();
            AddItems(rgcfp);
        }

        /// <summary>
        /// Removes the specified known property, if present.
        /// </summary>
        /// <param name="id"></param>
        public bool RemoveItem(CustomPropertyType.KnownProperties id)
        {
            return RemoveItem((int)id);
        }

        /// <summary>
        /// Removes the specified known property by proptype ID, if present.
        /// </summary>
        /// <param name="idPropType"></param>
        public bool RemoveItem(int idPropType)
        {
            return m_dictProps.Remove(idPropType);
        }
        #endregion
    }
}
