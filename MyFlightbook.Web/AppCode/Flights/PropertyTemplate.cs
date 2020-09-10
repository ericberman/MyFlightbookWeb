using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;

/******************************************************
 * 
 * Copyright (c) 2019-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Templates
{
    public enum PropertyTemplateGroup { Automatic, Training, Checkrides, Missions, Roles, Lessons }

    public enum KnownTemplateIDs { None = 0, ID_NEW = -1, ID_MRU = -2, ID_SIM = -3, ID_ANON = -4, ID_STUDENT = -5 }

    /// <summary>
    /// PropertyTemplate - basic functionality, base class for other templates
    /// </summary>
    [Serializable]
    public class PropertyTemplate : IComparable, IEquatable<PropertyTemplate>
    {
        protected HashSet<int> m_propertyTypes { get; private set; } = new HashSet<int>();

        #region Properties
        public int ID { get; set; }
        /// <summary>
        /// Name of the template
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of what this template does
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// what group does this propertytemplate fall under?
        /// </summary>
        [XmlIgnore]
        public PropertyTemplateGroup Group { get; set; }

        /// <summary>
        /// For serialization - allow groups as integers
        /// </summary>
        public int GroupAsInt
        {
            get { return (int)Group; }
            set { Group = (PropertyTemplateGroup)value; }
        }

        /// <summary>
        /// Display name for the group, for serialization.
        /// </summary>
        public string GroupDisplayName
        {
            get { return PropertyTemplate.NameForGroup(Group); }
            set { /* Just for serialization */}
        }

        /// <summary>
        /// The set of properties for this template; unused except for serialization
        /// </summary>
        public HashSet<int> PropertyTypes
        {
            get { return m_propertyTypes; }
            private set { m_propertyTypes = value; }
        }

        /// <summary>
        /// Should this be used by default for this user?  (Not mutually exclusive except with MRU)
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Can this be modified in the database, or is it "Built in"?
        /// </summary>
        public virtual bool IsMutable { get { return false; } }

        public virtual IEnumerable<string> PropertyNames
        {
            get
            {
                IEnumerable<CustomPropertyType> rgTypes = CustomPropertyType.GetCustomPropertyTypes(m_propertyTypes);
                return rgTypes.Select(cpt => cpt.Title);
            }
        }
        #endregion

        #region Membership
        public bool ContainsProperty(CustomPropertyType.KnownProperties kp) { return m_propertyTypes.Contains((int)kp); }

        public bool ContainsProperty(int idproptype) { return m_propertyTypes.Contains(idproptype); }

        public bool ContainsProperty(CustomPropertyType cpt) { return cpt != null && m_propertyTypes.Contains(cpt.PropTypeID); }

        public void AddProperty(int idProptype) { m_propertyTypes.Add(idProptype); }

        public void AddProperty(CustomPropertyType cpt)
        {
            if (cpt == null)
                throw new ArgumentNullException(nameof(cpt));
            m_propertyTypes.Add(cpt.PropTypeID);
        }

        public void RemoveProperty(int idPropType) { m_propertyTypes.Remove(idPropType); }

        public void RemoveProperty(CustomPropertyType cpt)
        {
            if (cpt == null)
                throw new ArgumentNullException(nameof(cpt));
            m_propertyTypes.Remove(cpt.PropTypeID);
        }
        #endregion

        #region constructors
        public PropertyTemplate()
        {
            ID = (int) KnownTemplateIDs.ID_NEW;
            Name = Description = string.Empty;
            Group = PropertyTemplateGroup.Training;
        }

        public PropertyTemplate(PropertyTemplate ptSrc) : this()
        {
            if (ptSrc != null)
            {
                ID = ptSrc.ID;
                Name = ptSrc.Name;
                Description = ptSrc.Description;
                Group = ptSrc.Group;
                PropertyTypes = ptSrc.PropertyTypes;
                IsDefault = ptSrc.IsDefault;
            }
        }
        #endregion

        #region static helpers
        public static string NameForGroup(PropertyTemplateGroup pg)
        {
            switch (pg)
            {
                case PropertyTemplateGroup.Automatic:
                    return Resources.LogbookEntry.templateGroupAuto;
                case PropertyTemplateGroup.Checkrides:
                    return Resources.LogbookEntry.templateGroupCheckrides;
                case PropertyTemplateGroup.Missions:
                    return Resources.LogbookEntry.templateGroupMissions;
                case PropertyTemplateGroup.Training:
                    return Resources.LogbookEntry.templateGroupTraining;
                case PropertyTemplateGroup.Roles:
                    return Resources.LogbookEntry.templateGroupRole;
                case PropertyTemplateGroup.Lessons:
                    return Resources.LogbookEntry.templateGroupLessons;
                default:
                    return string.Empty;
            }
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, Name, NameForGroup(Group));
        }

        /// <summary>
        /// Returns a pseudo propertytemplate that reflects a merge of the provided templates.
        /// </summary>
        /// <param name="lstIn"></param>
        /// <returns></returns>
        public static PropertyTemplate MergedTemplate(IEnumerable<PropertyTemplate> lstIn)
        {
            if (lstIn == null)
                return null;

            PropertyTemplate pt = new UserPropertyTemplate();
            foreach (PropertyTemplate ptIn in lstIn)
                pt.m_propertyTypes.UnionWith(ptIn.m_propertyTypes);

            return pt;
        }

        #region IComparable
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            return Name.CompareCurrentCultureIgnoreCase(((PropertyTemplate)obj).Name);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PropertyTemplate);
        }

        public bool Equals(PropertyTemplate other)
        {
            return other != null &&
                   ID == other.ID &&
                   Name == other.Name &&
                   Description == other.Description &&
                   Group == other.Group &&
                   GroupAsInt == other.GroupAsInt &&
                   GroupDisplayName == other.GroupDisplayName &&
                   EqualityComparer<HashSet<int>>.Default.Equals(PropertyTypes, other.PropertyTypes) &&
                   IsDefault == other.IsDefault &&
                   IsMutable == other.IsMutable &&
                   EqualityComparer<IEnumerable<string>>.Default.Equals(PropertyNames, other.PropertyNames);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 697218593;
                hashCode = hashCode * -1521134295 + ID.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Description);
                hashCode = hashCode * -1521134295 + Group.GetHashCode();
                hashCode = hashCode * -1521134295 + GroupAsInt.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(GroupDisplayName);
                hashCode = hashCode * -1521134295 + EqualityComparer<HashSet<int>>.Default.GetHashCode(PropertyTypes);
                hashCode = hashCode * -1521134295 + IsDefault.GetHashCode();
                hashCode = hashCode * -1521134295 + IsMutable.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<IEnumerable<string>>.Default.GetHashCode(PropertyNames);
                return hashCode;
            }
        }

        public static bool operator ==(PropertyTemplate left, PropertyTemplate right)
        {
            return EqualityComparer<PropertyTemplate>.Default.Equals(left, right);
        }

        public static bool operator !=(PropertyTemplate left, PropertyTemplate right)
        {
            return !(left == right);
        }

        public static bool operator <(PropertyTemplate left, PropertyTemplate right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(PropertyTemplate left, PropertyTemplate right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(PropertyTemplate left, PropertyTemplate right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(PropertyTemplate left, PropertyTemplate right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion
    }

    #region Built-in automatic templates
    /// <summary>
    /// Built-in property template for MRU functionality (previously used properties)
    /// </summary>
    [Serializable]
    public class MRUPropertyTemplate : PropertyTemplate
    {
        public override IEnumerable<string> PropertyNames { get { return Array.Empty<string>(); } }

        public MRUPropertyTemplate() : base()
        {
            ID = (int) KnownTemplateIDs.ID_MRU;
            Group = PropertyTemplateGroup.Automatic;
            Name = Resources.LogbookEntry.TemplateMRU;
            Description = Resources.LogbookEntry.TemplateMRUDescription;
        }

        public MRUPropertyTemplate(string szuser) : this()
        {
            m_propertyTypes.Clear();
            CustomPropertyType[] rgTypes = CustomPropertyType.GetCustomPropertyTypes(szuser);
            foreach (CustomPropertyType cpt in rgTypes)
                if (cpt.IsFavorite)
                    m_propertyTypes.Add(cpt.PropTypeID);
        }
    }

    [Serializable]
    public class SimPropertyTemplate : PropertyTemplate
    {
        public SimPropertyTemplate() : base()
        {
            ID = (int)KnownTemplateIDs.ID_SIM;
            Group = PropertyTemplateGroup.Automatic;
            Name = Resources.LogbookEntry.TemplateSimBasic;
            Description = Resources.LogbookEntry.TemplateSimBasicDesc;
            m_propertyTypes.Clear();
            m_propertyTypes.Add((int)CustomPropertyType.KnownProperties.IDPropSimRegistration);
            m_propertyTypes.Add((int)CustomPropertyType.KnownProperties.IDPropGroundInstructionReceived);
        }
    }

    [Serializable]
    public class AnonymousPropertyTemplate : PropertyTemplate
    {
        public AnonymousPropertyTemplate() : base()
        {
            ID = (int)KnownTemplateIDs.ID_ANON;
            Group = PropertyTemplateGroup.Automatic;
            Name = Resources.LogbookEntry.TemplateAnonymousBasic;
            Description = Resources.LogbookEntry.TemplateAnonymousBasicDesc;
            m_propertyTypes.Clear();
            m_propertyTypes.Add((int)CustomPropertyType.KnownProperties.IDPropAircraftRegistration);
        }
    }

    [Serializable]
    public class StudentPropertyTemplate : PropertyTemplate
    {
        public StudentPropertyTemplate() : base()
        {
            ID = (int)KnownTemplateIDs.ID_STUDENT;
            Group = PropertyTemplateGroup.Automatic;
            Name = Resources.LogbookEntry.TemplateStudent;
            Description = Resources.LogbookEntry.TemplateStudentDesc;
            m_propertyTypes.Clear();
            m_propertyTypes.Add((int)CustomPropertyType.KnownProperties.IDPropGroundInstructionReceived);
            m_propertyTypes.Add((int)CustomPropertyType.KnownProperties.IDPropInstructorName);
        }
    }
    #endregion

    /// <summary>
    /// Property template class that can be saved/deleted/read from the database
    /// </summary>
    [Serializable]
    public abstract class PersistablePropertyTemplate : PropertyTemplate
    {
        #region Properties
        /// <summary>
        /// Owner of the template - empty if public
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// Original creator of the template (for public templates)
        /// </summary>
        public string OriginalOwner { get; set; }

        public override bool IsMutable { get { return true; } }
        #endregion

        #region Constructors
        protected PersistablePropertyTemplate() : base()
        {
            Owner = OriginalOwner = string.Empty;
        }

        protected PersistablePropertyTemplate(MySqlDataReader dr) : this()
        {
            InitFromDataReader(dr);
        }

        /// <summary>
        /// Is this available for all users?
        /// </summary>
        public bool IsPublic { get; set; }

        protected PersistablePropertyTemplate(int id) : this()
        {
            DBHelper dbh = new DBHelper("SELECT * FROM propertytemplate WHERE id=?id");
            dbh.ReadRow(
                (comm) => { comm.Parameters.AddWithValue("id", id); },
                (dr) => { InitFromDataReader(dr); });
        }
        #endregion

        #region Database
        /// <summary>
        /// Called to invalidate any cached entries, if caching is done by a subclass
        /// </summary>
        protected virtual void InvalidateCache() { }

        protected void InitFromDataReader(MySqlDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
            ID = Convert.ToInt32(dr["id"], CultureInfo.InvariantCulture);
            Name = (string)dr["name"];
            Description = util.ReadNullableString(dr, "description");
            Owner = (string)dr["owner"];
            OriginalOwner = (string)dr["originalowner"];
            Group = (PropertyTemplateGroup)Convert.ToUInt32(dr["templategroup"], CultureInfo.InvariantCulture);
            IsPublic = Convert.ToBoolean(dr["public"], CultureInfo.InvariantCulture);
            IsDefault = Convert.ToBoolean(dr["isdefault"], CultureInfo.InvariantCulture);
            m_propertyTypes.Clear();
            int[] rgids = JsonConvert.DeserializeObject<int[]>((string)dr["properties"]);
            foreach (int i in rgids)
                m_propertyTypes.Add(i);
        }

        /// <summary>
        /// Throws an exception if not valid.
        /// </summary>
        public virtual void Validate()
        {
            if (String.IsNullOrWhiteSpace(Name))
                throw new MyFlightbookValidationException(Resources.LogbookEntry.errTemplateNoName);

            if (m_propertyTypes.Count == 0)
                throw new MyFlightbookValidationException(Resources.LogbookEntry.errTemplateNoProperties);
        }

        /// <summary>
        /// Saves the template to the database.
        /// Throws an exception if validation fails.
        /// </summary>
        public void Commit()
        {
            Validate();

            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture,
                "{0} propertytemplate SET name=?name, description=?description, owner=?owner, originalowner=?originalowner, templategroup=?group, properties=?props, public=?public, isdefault=?def {1}",
                ID == (int)KnownTemplateIDs.ID_NEW ? "INSERT INTO" : "UPDATE",
                ID == (int)KnownTemplateIDs.ID_NEW ? string.Empty : String.Format(CultureInfo.InvariantCulture, "WHERE id=?id")));

            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("id", ID);
                comm.Parameters.AddWithValue("name", Name.LimitTo(45));
                comm.Parameters.AddWithValue("description", String.IsNullOrEmpty(Description) ? null : Description.LimitTo(255));
                comm.Parameters.AddWithValue("owner", Owner);
                comm.Parameters.AddWithValue("originalowner", OriginalOwner);
                comm.Parameters.AddWithValue("group", (int)Group);
                comm.Parameters.AddWithValue("props", JsonConvert.SerializeObject(m_propertyTypes));
                comm.Parameters.AddWithValue("public", IsPublic);
                comm.Parameters.AddWithValue("def", IsDefault);
            });
            
            if (ID == (int)KnownTemplateIDs.ID_NEW && dbh.LastInsertedRowId > 0)
                ID = dbh.LastInsertedRowId;

            InvalidateCache();
        }

        public void Delete()
        {
            DBHelper dbh = new DBHelper("DELETE FROM propertytemplate WHERE ID=?id");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("id", ID);
            });
            InvalidateCache();
        }
        #endregion
    }

    /// <summary>
    /// Property template that can be created by a user.
    /// </summary>
    [Serializable]
    public class UserPropertyTemplate : PersistablePropertyTemplate
    {
        #region constructors
        public UserPropertyTemplate() : base() { }

        public UserPropertyTemplate(MySqlDataReader dr) : base(dr) { }

        public UserPropertyTemplate(int id) : base(id) { }
        #endregion

        #region Caching
        private const string szCacheKey = "cachedPropTemplates";

        protected override void InvalidateCache()
        {
            if (String.IsNullOrWhiteSpace(Owner))
                return;

            Profile.GetUser(Owner).AssociatedData.Remove(szCacheKey);
        }

        private static List<PropertyTemplate> CachedTemplatesForUser(string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));
            if (String.IsNullOrWhiteSpace(szUser))
                return null;

            return (List<PropertyTemplate>) Profile.GetUser(szUser).CachedObject(szCacheKey);
        }
        #endregion

        /// <summary>
        /// Creates a copy of a public template for the user.
        /// </summary>
        /// <param name="szUser"></param>
        public PersistablePropertyTemplate CopyPublicTemplate(string szUser)
        {
            if (String.IsNullOrWhiteSpace(szUser))
                throw new MyFlightbookValidationException("Trying to consume for empty user");
            if (!IsPublic)
                throw new MyFlightbookValidationException("Can't consume a non-published template");
            UserPropertyTemplate upt = new UserPropertyTemplate();
            // Initialize from this
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(this), upt);
            upt.OriginalOwner = Owner;
            upt.Owner = szUser;
            upt.ID = (int)KnownTemplateIDs.ID_NEW;
            upt.IsPublic = false;
            return upt;
        }

        #region Database
        /// <summary>
        /// Returns the property templates for the specified user.  Cached for performance
        /// </summary>
        /// <param name="szUser"></param>
        /// <param name="fIncludeAutomatic">True if automatic templates shoudl be included</param>
        /// <returns></returns>
        public static IEnumerable<PropertyTemplate> TemplatesForUser(string szUser, bool fIncludeAutomatic = true)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));

            List<PropertyTemplate> lst = CachedTemplatesForUser(szUser);

            if (lst == null)
            {
                lst = new List<PropertyTemplate>();
                DBHelper dbh = new DBHelper("SELECT * FROM propertytemplate WHERE owner=?user");
                dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("user", szUser); },
                (dr) => { lst.Add(new UserPropertyTemplate(dr)); });
                Profile.GetUser(szUser).AssociatedData[szCacheKey] = lst;
            }

            // Add in the automatic properties as well.
            // But do it in a NEW list so as not to affect what's in the cache
            // We do this fresh every time, since MRUPropertyTemplate could have changed (due to blocklisting), but is itself cached.
            List<PropertyTemplate> lstResult = new List<PropertyTemplate>(lst);
            if (fIncludeAutomatic)
                lstResult.AddRange(AutomaticTemplatesForUser(szUser));
            return lstResult;
        }

        public static IEnumerable<PropertyTemplate> DefaultTemplatesForUser(string szUser)
        {
            List<PropertyTemplate> lst = new List<PropertyTemplate>(TemplatesForUser(szUser, false));
            lst.RemoveAll(pt => !pt.IsDefault);
            return lst;
        }

        /// <summary>
        /// Returns the automatic templates for the user, but NOT the student template, which is added only for CFI's creating an entry for a student.
        /// </summary>
        /// <param name="szUser"></param>
        /// <returns></returns>
        public static IEnumerable<PropertyTemplate> AutomaticTemplatesForUser(string szUser)
        {
            return new PropertyTemplate[] { new MRUPropertyTemplate(szUser), new SimPropertyTemplate(), new AnonymousPropertyTemplate() };
        }

        /// <summary>
        /// Returns all public property templates
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<UserPropertyTemplate> PublicTemplates()
        {
            List<UserPropertyTemplate> lst = new List<UserPropertyTemplate>();
            DBHelper dbh = new DBHelper("SELECT * FROM propertytemplate WHERE public <> 0");
            dbh.ReadRows((comm) => { },
            (dr) => { lst.Add(new UserPropertyTemplate(dr)); });
            return lst;
        }
        #endregion
    }

    public class TemplateCollection : IComparable, IEquatable<TemplateCollection>
    {
        #region Properties
        public PropertyTemplateGroup Group { get; set; }

        public string GroupName { get { return PropertyTemplate.NameForGroup(Group); } }

        public IEnumerable<PropertyTemplate> Templates { get; set; }
        #endregion

        public TemplateCollection(PropertyTemplateGroup ptg, IEnumerable<PropertyTemplate> rgTemplates)
        {
            Group = ptg;
            Templates = rgTemplates;
        }

        public static IEnumerable<TemplateCollection> GroupTemplates(IEnumerable<PropertyTemplate> rgTemplates)
        {
            Dictionary<PropertyTemplateGroup, List<PropertyTemplate>> d = new Dictionary<PropertyTemplateGroup, List<PropertyTemplate>>();

            if (rgTemplates == null)
                throw new ArgumentNullException(nameof(rgTemplates));

            foreach (PropertyTemplate pt in rgTemplates)
            {
                if (d.TryGetValue(pt.Group, out List<PropertyTemplate> lst))
                    lst.Add(pt);
                else
                    d[pt.Group] = new List<PropertyTemplate>() { pt };
            }

            List<TemplateCollection> lstOut = new List<TemplateCollection>();
            foreach (PropertyTemplateGroup ptg in d.Keys)
            {
                // sort all but automatic - those are in a pre-defined order.
                if (ptg != PropertyTemplateGroup.Automatic)
                    d[ptg].Sort();
                lstOut.Add(new TemplateCollection(ptg, d[ptg]));
            }
            lstOut.Sort();

            return lstOut;
        }

        #region IComparable
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            TemplateCollection tc = obj as TemplateCollection;

            // Automatic is always first - so if either this or the other is automatic, sort by ordinal value; otherwise, sort by name
            if (Group == PropertyTemplateGroup.Automatic || tc.Group == PropertyTemplateGroup.Automatic)
                return ((int)Group).CompareTo((int)tc.Group);
            else
                return PropertyTemplate.NameForGroup(Group).CompareCurrentCultureIgnoreCase(PropertyTemplate.NameForGroup(tc.Group));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TemplateCollection);
        }

        public bool Equals(TemplateCollection other)
        {
            return other != null &&
                   Group == other.Group &&
                   GroupName == other.GroupName &&
                   EqualityComparer<IEnumerable<PropertyTemplate>>.Default.Equals(Templates, other.Templates);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = -607834157;
                hashCode = hashCode * -1521134295 + Group.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(GroupName);
                hashCode = hashCode * -1521134295 + EqualityComparer<IEnumerable<PropertyTemplate>>.Default.GetHashCode(Templates);
                return hashCode;
            }
        }

        public static bool operator ==(TemplateCollection left, TemplateCollection right)
        {
            return EqualityComparer<TemplateCollection>.Default.Equals(left, right);
        }

        public static bool operator !=(TemplateCollection left, TemplateCollection right)
        {
            return !(left == right);
        }

        public static bool operator <(TemplateCollection left, TemplateCollection right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(TemplateCollection left, TemplateCollection right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(TemplateCollection left, TemplateCollection right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(TemplateCollection left, TemplateCollection right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion
    }

    [Serializable]
    public class TemplatePropTypeBundle
    {
        #region Properties
        public Collection <CustomPropertyType> UserProperties { get; private set; }
        public Collection<PropertyTemplate> UserTemplates { get; private set; }
        #endregion

        #region Constructors
        public TemplatePropTypeBundle()
        {
            UserProperties = new Collection<CustomPropertyType>();;
            UserTemplates = new Collection<PropertyTemplate>();
        }

        public TemplatePropTypeBundle(string szUser) : this()
        {
            UserProperties = new Collection<CustomPropertyType>(new List<CustomPropertyType>(CustomPropertyType.GetCustomPropertyTypes(szUser)));
            // For serialization to work with propertytemplates, the objects need to actually be propertytemplates, not subclasses.
            List<PropertyTemplate> lst = new List<PropertyTemplate>();
            IEnumerable<PropertyTemplate> rgpt = UserPropertyTemplate.TemplatesForUser(szUser, true);
            foreach (PropertyTemplate pt in rgpt)
                lst.Add(new PropertyTemplate(pt));
            UserTemplates = new Collection<PropertyTemplate>(lst);
        }
        #endregion
    }

    public class PropertyTemplateEventArgs : EventArgs
    {
        public PropertyTemplate Template { get; set; }

        public int TemplateID { get; set; }

        public PropertyTemplateEventArgs(PropertyTemplate pt) : base()
        {
            Template = pt;
            TemplateID = pt == null ? (int) KnownTemplateIDs.ID_NEW : pt.ID;
        }

        public PropertyTemplateEventArgs(int id) : base()
        {
            Template = null;
            TemplateID = id;
        }
    }
}
