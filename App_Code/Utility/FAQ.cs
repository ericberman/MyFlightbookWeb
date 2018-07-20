using HtmlAgilityPack;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2008-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
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
        public string AnswerPlainText { get; private set; }
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

        private const string szFAQCacheKey = "cachekeyFAQItems";

        public static IEnumerable<FAQItem> CachedFAQItems
        {
            get
            {
                if (HttpRuntime.Cache == null)
                    return AllFAQItems;
                if (HttpRuntime.Cache[szFAQCacheKey] == null)
                    HttpRuntime.Cache.Add(szFAQCacheKey, AllFAQItems, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, 30, 0), System.Web.Caching.CacheItemPriority.BelowNormal, null);
                return (IEnumerable<FAQItem>)HttpRuntime.Cache[szFAQCacheKey];
            }
        }

        public static void FlushFAQCache()
        {
            if (HttpRuntime.Cache != null)
                HttpRuntime.Cache.Remove(szFAQCacheKey);
        }

        #region constructors
        public FAQItem()
        {
            Category = Question = Answer = AnswerPlainText = string.Empty;
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
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(Answer);
            StringBuilder sb = new StringBuilder();
            foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//text()"))
                sb.Append(node.InnerText);
            AnswerPlainText = sb.ToString();
            IsSelected = false;
        }
        #endregion

        /// <summary>
        /// Determines if this FAQ item contains the specified words (should be to-upper'd for case invariance before passing in)
        /// </summary>
        /// <param name="searchTerms">An enumerable of search terms</param>
        /// <returns>True if the question or answer contains the terms</returns>
        public bool ContainsWords(IEnumerable<string> searchTerms)
        {
            if (searchTerms == null)
                return false;

            string qUpper = Question.ToUpper(CultureInfo.CurrentCulture);
            string aUpper = AnswerPlainText.ToUpper(CultureInfo.CurrentCulture);

            foreach (string sz in searchTerms)
            {
                if (String.IsNullOrWhiteSpace(sz))
                    continue;

                if (!qUpper.Contains(sz) && !aUpper.Contains(sz))
                    return false;
            }

            return true;
        }
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

        /// <summary>
        /// Returns a categorized (grouped) list of all FAQ items, reading from the cache as needed
        /// </summary>
        public static IEnumerable<FAQGroup> CategorizedFAQs
        {
            get { return CategorizeFAQItems(FAQItem.CachedFAQItems); }
        }

        /// <summary>
        /// Returns a categorized (grouped) list of all FAQ items matching the specified search string
        /// </summary>
        /// <param name="lstWords"></param>
        /// <returns></returns>
        public static IEnumerable<FAQGroup> CategorizedFAQItemsContainingWords(string szSearch)
        {
            if (String.IsNullOrWhiteSpace(szSearch))
                return CategorizedFAQs;

            string[] words = Regex.Split(szSearch.ToUpper(CultureInfo.CurrentCulture), "\\s");
            List<FAQItem> lst = new List<FAQItem>(FAQItem.CachedFAQItems);
            lst.RemoveAll(fi => !fi.ContainsWords(words));
            return CategorizeFAQItems(lst);
        }

        /// <summary>
        /// Categorizes a set of FAQItems into groups
        /// </summary>
        /// <param name="lstIn">The FAQItems</param>
        /// <returns>A set of FAQGroups containing the input items, grouped.</returns>
        public static IEnumerable<FAQGroup> CategorizeFAQItems(IEnumerable<FAQItem> lstIn)
        {
            if (lstIn == null)
                return new FAQGroup[0];

            Dictionary<string, FAQGroup> dict = new Dictionary<string, FAQGroup>();
            foreach (FAQItem fi in lstIn)
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
    #endregion
}