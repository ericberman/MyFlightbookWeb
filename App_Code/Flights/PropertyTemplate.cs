using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Serialization;

/******************************************************
 * 
 * Copyright (c) 2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Templates
{
    public enum PropertyTemplateGroup { Automatic, Training, Checkrides, Missions }

    /// <summary>
    /// PropertyTemplate - basic functionality, base class for other templates
    /// </summary>
    [Serializable]
    public abstract class PropertyTemplate
    {
        protected HashSet<int> m_propertyTypes = new HashSet<int>();
        protected const int idTemplateNew = -1;

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
        /// Owner of the template - empty if public
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// Original creator of the template (for public templates)
        /// </summary>
        public string OriginalOwner { get; set; }

        /// <summary>
        /// what group does this propertytemplate fall under?
        /// </summary>
        public PropertyTemplateGroup Group { get; set; }

        /// <summary>
        /// The set of properties for this template
        /// </summary>
        public IEnumerable<int> PropertyTypes { get { return m_propertyTypes; } }

        /// <summary>
        /// Is this available for all users?
        /// </summary>
        public bool IsPublic { get; set; }

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

        public bool ContainsProperty(CustomPropertyType cpt) { return m_propertyTypes.Contains(cpt.PropTypeID); }

        public void AddProperty(int idProptype) { m_propertyTypes.Add(idProptype); }

        public void AddProperty(CustomPropertyType cpt) { m_propertyTypes.Add(cpt.PropTypeID); }

        public void RemoveProperty(int idPropType) { m_propertyTypes.Remove(idPropType); }

        public void RemoveProperty(CustomPropertyType cpt) { m_propertyTypes.Remove(cpt.PropTypeID); }
        #endregion

        #region constructors
        public PropertyTemplate()
        {
            ID = idTemplateNew;
            Name = Description = Owner = OriginalOwner = string.Empty;
            Group = PropertyTemplateGroup.Training;
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
                default:
                    return string.Empty;
            }
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, Name, NameForGroup(Group));
        }
    }

    /// <summary>
    /// Built-in property template for MRU functionality (previously used properties)
    /// </summary>
    [Serializable]
    public class MRUPropertyTemplate : PropertyTemplate
    {
        public override IEnumerable<string> PropertyNames { get { return new string[0]; } }

        public MRUPropertyTemplate() : base()
        {
            Group = PropertyTemplateGroup.Automatic;
            Name = Resources.LogbookEntry.TemplateMRU;
            Description = Resources.LogbookEntry.TemplateMRUDescription;
        }

        public MRUPropertyTemplate(string szuser) : this()
        {
            Owner = OriginalOwner = szuser;
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
            Group = PropertyTemplateGroup.Automatic;
            Name = Resources.LogbookEntry.TemplateSimBasic;
            Description = Resources.LogbookEntry.TemplateSimBasicDesc;
            m_propertyTypes = new HashSet<int>() { (int)CustomPropertyType.KnownProperties.IDPropSimRegistration, (int)CustomPropertyType.KnownProperties.IDPropGroundInstructionReceived };
        }
    }

    [Serializable]
    public class AnonymousPropertyTeamplate : PropertyTemplate
    {
        public AnonymousPropertyTeamplate() : base()
        {
            Group = PropertyTemplateGroup.Automatic;
            Name = Resources.LogbookEntry.TemplateAnonymousBasic;
            Description = Resources.LogbookEntry.TemplateAnonymousBasicDesc;
            m_propertyTypes = new HashSet<int>() { (int)CustomPropertyType.KnownProperties.IDPropAircraftRegistration };
        }
    }


    /// <summary>
    /// Property template class that can be saved/deleted/read from the database
    /// </summary>
    [Serializable]
    public class PersistablePropertyTemplate : PropertyTemplate
    {
        #region Properties
        public override bool IsMutable { get { return true; } }
        #endregion

        #region Constructors
        protected PersistablePropertyTemplate() : base() { }

        protected PersistablePropertyTemplate(MySqlDataReader dr) : this()
        {
            InitFromDataReader(dr);
        }

        protected PersistablePropertyTemplate(int id) : this()
        {
            DBHelper dbh = new DBHelper("SELECT * FROM propertytemplate WHERE id=?id");
            dbh.ReadRow(
                (comm) => { comm.Parameters.AddWithValue("id", id); },
                (dr) => { InitFromDataReader(dr); });
        }
        #endregion

        #region Database
        protected void InitFromDataReader(MySqlDataReader dr)
        {
            ID = Convert.ToInt32(dr["id"], CultureInfo.InvariantCulture);
            Name = (string)dr["name"];
            Description = util.ReadNullableString(dr, "description");
            Owner = (string)dr["owner"];
            OriginalOwner = (string)dr["originalowner"];
            Group = (PropertyTemplateGroup)Convert.ToUInt32(dr["templategroup"], CultureInfo.InvariantCulture);
            IsPublic = Convert.ToBoolean(dr["public"], CultureInfo.InvariantCulture);
            m_propertyTypes = new HashSet<int>(JsonConvert.DeserializeObject<int[]>((string)dr["properties"]));
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
                "{0} propertytemplate SET name=?name, description=?description, owner=?owner, originalowner=?originalowner, templategroup=?group, properties=?props, public=?public {1}",
                ID == idTemplateNew ? "INSERT INTO" : "UPDATE",
                ID == idTemplateNew ? string.Empty : String.Format(CultureInfo.InvariantCulture, "WHERE id=?id")));

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
            });
        }

        /// <summary>
        /// Copies a public template for the user.  DOES NOT COMMIT!
        /// MODIFIES THIS OBJECT
        /// </summary>
        /// <param name="szUser"></param>
        public void CopyPublicTemplate(string szUser)
        {
            if (String.IsNullOrWhiteSpace(szUser))
                throw new MyFlightbookValidationException("Trying to consume for empty user");
            if (String.IsNullOrWhiteSpace(OriginalOwner))
                throw new MyFlightbookValidationException("Consumed templates need an original owner");
            if (!IsPublic)
                throw new MyFlightbookValidationException("Can't consume a non-published template");
            OriginalOwner = Owner;
            Owner = szUser;
            ID = idTemplateNew;
            IsPublic = false;
        }

        public void Delete()
        {
            DBHelper dbh = new DBHelper("DELETE FROM propertytemplate WHERE ID=?id");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("id", ID);
            });
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

        /// <summary>
        /// Returns a pseudo propertytemplate that reflects a merge of the provided templates.
        /// </summary>
        /// <param name="lstIn"></param>
        /// <returns></returns>
        public static PropertyTemplate MergedTemplate(IEnumerable<PropertyTemplate> lstIn)
        {
            if (lstIn == null)
                return null;

            UserPropertyTemplate pt = new UserPropertyTemplate();
            foreach (UserPropertyTemplate ptIn in lstIn)
                pt.m_propertyTypes.UnionWith(ptIn.m_propertyTypes);

            return pt;
        }

        #region Database
        /// <summary>
        /// Returns the property templates for the specified user
        /// </summary>
        /// <param name="szUser"></param>
        /// <returns></returns>
        public static IEnumerable<PropertyTemplate> TemplatesForUser(string szUser)
        {
            List<PropertyTemplate> lst = new List<PropertyTemplate>() { new MRUPropertyTemplate(szUser), new SimPropertyTemplate(), new AnonymousPropertyTeamplate() };
            DBHelper dbh = new DBHelper("SELECT * FROM propertytemplate WHERE owner=?user");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("user", szUser); },
            (dr) => { lst.Add(new UserPropertyTemplate(dr)); });
            return lst;
        }

        /// <summary>
        /// Returns all public property templates
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<PropertyTemplate> PublicTemplates()
        {
            List<PropertyTemplate> lst = new List<PropertyTemplate>();
            DBHelper dbh = new DBHelper("SELECT * FROM propertytemplate WHERE public <> 0");
            dbh.ReadRows((comm) => { },
            (dr) => { lst.Add(new UserPropertyTemplate(dr)); });
            return lst;
        }
        #endregion
    }

    public class TemplateCollection
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

            foreach (PropertyTemplate pt in rgTemplates)
            {
                List<PropertyTemplate> lst;
                if (d.TryGetValue(pt.Group, out lst))
                    lst.Add(pt);
                else
                    d[pt.Group] = new List<PropertyTemplate>() { pt };
            }

            List<TemplateCollection> lstOut = new List<TemplateCollection>();
            foreach (PropertyTemplateGroup ptg in d.Keys)
                lstOut.Add(new TemplateCollection(ptg, d[ptg]));

            return lstOut;
        }
    }

    public class PropertyTemplateEventArgs : EventArgs
    {
        public PropertyTemplate propertyTemplate { get; set; }

        public PropertyTemplateEventArgs(PropertyTemplate pt) : base()
        {
            propertyTemplate = pt;
        }
    }
}
