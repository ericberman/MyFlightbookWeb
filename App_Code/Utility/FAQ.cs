using System;
using System.Collections.Generic;
using System.Globalization;
using MySql.Data.MySqlClient;

/******************************************************
 * 
 * Copyright (c) 2008-2016 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    #region FAQ
    /// <summary>
    /// An individual question with answer
    /// </summary>
    public class FAQItem
    {
        #region properties
        public int idFAQ { get; set; }
        public string Category { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public bool IsSelected { get; set; }
        #endregion

        public static IEnumerable<FAQItem> AllFAQItems
        {
            get
            {
                List<FAQItem> lst = new List<FAQItem>();
                DBHelper dbh = new DBHelper("SELECT * FROM FAQ ORDER BY Category ASC, Question ASC");
                dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new FAQItem(dr)); });
                return lst;
            }
        }

        #region constructors
        public FAQItem()
        {
            Category = Question = Answer = string.Empty;
            IsSelected = false;
            idFAQ = -1;
        }

        protected FAQItem(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            idFAQ = Convert.ToInt32(dr["idFAQ"], CultureInfo.InvariantCulture);
            Category = Branding.ReBrand(dr["Category"].ToString());
            Question = Branding.ReBrand(dr["Question"].ToString());
            Answer = Branding.ReBrand(dr["Answer"].ToString());
            IsSelected = false;
        }
        #endregion
    }

    /// <summary>
    /// A group of FAQItems in a particular category
    /// </summary>
    public class FAQGroup
    {
        #region properties
        /// <summary>
        /// The category for the group
        /// </summary>
        public string Category { get; set; }

        private List<FAQItem> m_lstFAQs = null;

        /// <summary>
        /// The FAQ Items
        /// </summary>
        public IEnumerable<FAQItem> Items
        {
            get
            {
                if (m_lstFAQs == null)
                    m_lstFAQs = new List<FAQItem>();
                return m_lstFAQs;
            }
        }
        #endregion

        #region constructors
        public FAQGroup()
        {
            Category = string.Empty;
        }

        public FAQGroup(string category, IEnumerable<FAQItem> lst) : this()
        {
            Category = category;
            m_lstFAQs = new List<FAQItem>(lst);
        }
        #endregion

        public static IEnumerable<FAQGroup> CategorizedFAQs
        {
            get
            {
                IEnumerable<FAQItem> lstSrc = FAQItem.AllFAQItems;
                Dictionary<string, FAQGroup> dict = new Dictionary<string, FAQGroup>();
                foreach (FAQItem fi in lstSrc)
                {
                    if (!dict.ContainsKey(fi.Category))
                        dict[fi.Category] = new FAQGroup(fi.Category, new FAQItem[] { fi });
                    else
                        dict[fi.Category].m_lstFAQs.Add(fi);
                }

                List<FAQGroup> lstResult = new List<FAQGroup>();
                foreach (string key in dict.Keys)
                    lstResult.Add(dict[key]);

                return lstResult;
            }
        }
    }
    #endregion
}