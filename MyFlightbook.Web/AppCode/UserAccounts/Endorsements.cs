using MyFlightbook.Image;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2010-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Instruction
{
    /// <summary>
    /// Available endorsement modes.
    /// </summary>
    public enum EndorsementMode
    {
        /// <summary>
        /// Indicates that the instructor is issuing a digitally signed endorsement
        /// </summary>
        InstructorPushAuthenticated,
        /// <summary>
        /// Indicates that the instructor is recording an endorsement given off-line
        /// </summary>
        InstructorOfflineStudent,
        /// <summary>
        /// Indicates that the student is driving, instructor will provide their password
        /// </summary>
        StudentPullAuthenticated,
        /// <summary>
        /// Indicates that the student is driving, instructor will scribble
        /// </summary>
        StudentPullAdHoc
    }

    /// <summary>
    /// Summary description for Endorsement
    /// </summary>
    [Serializable]
    public class Endorsement
    {
        public enum StudentTypes { Member, External }

        public enum EndorsementKind { Regular, Image }

        public enum CategorySource {  None = 0, Template = 1, Rule = 2, Model = 3, User = 4 }

        #region Properties
        /// <summary>
        /// UserName of the student being endorsed
        /// </summary>
        public string StudentName { get; set; }

        /// <summary>
        /// The type of student - if it's a member, they have an account.
        /// </summary>
        public StudentTypes StudentType { get; set; }

        /// <summary>
        /// Quick utility routine to test for member-based endorsements
        /// </summary>
        public bool IsMemberEndorsement
        {
            get { return StudentType == StudentTypes.Member; }
        }

        /// <summary>
        /// Quick utility routine to test for external endorsements
        /// </summary>
        public bool IsExternalEndorsement
        {
            get { return StudentType == StudentTypes.External; }
        }

        private byte[] digitizedSig;

        /// <summary>
        /// A scribble-signature for a digitized sig
        /// </summary>
        public byte[] GetDigitizedSig()
        {
            return digitizedSig;
        }

        /// <summary>
        /// A scribble-signature for a digitized sig
        /// </summary>
        public void SetDigitizedSig(byte[] value)
        {
            digitizedSig = value;
        }

        public string DigitizedSigLink
        {
            get { return ScribbleImage.DataLinkForByteArray(GetDigitizedSig()); }
        }

        /// <summary>
        /// Is this an ad-hoc endorsement (scribble signature vs. member instructor?)
        /// </summary>
        public bool IsAdHocEndorsement
        {
            get { return String.IsNullOrEmpty(InstructorName) && HasDigitizedSig; }
        }

        public bool HasDigitizedSig { get { return digitizedSig != null && digitizedSig.Length > 0; } }

        /// <summary>
        /// Username of the instructor
        /// </summary>
        public string InstructorName { get; set; }

        /// <summary>
        /// Date of the endorsement
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Date that the endorsement was deleted by the student, if deleted.
        /// </summary>
        public DateTime? DateDeleted { get; set; }

        /// <summary>
        /// The timestamp of the date's creation
        /// </summary>
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Expiration of the instructor's certificate
        /// </summary>
        public DateTime CFIExpirationDate { get; set; }

        /// <summary>
        /// Instructor's certificate #
        /// </summary>
        public string CFICertificate { get; set; }

        /// <summary>
        /// The text of the endorsement.
        /// </summary>
        public string EndorsementText { get; set; }

        /// <summary>
        /// The ID for the endorsement
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The title of the endorsement
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The FAR referenced by the endorsement
        /// </summary>
        public string FARReference { get; set; }

        /// <summary>
        /// Fully qualified title, including a FAR if any
        /// </summary>
        public string FullTitleAndFar
        {
            get { return String.Format(CultureInfo.CurrentCulture, "{0}{1}", Title, FARReference.Length > 0 ? String.Format(CultureInfo.CurrentCulture, ": FAR {0}", FARReference) : ""); }
        }

        /// <summary>
        /// Return the full name of the student
        /// </summary>
        public string StudentDisplayName
        {
            get { return StudentType == StudentTypes.Member ? (Profile.GetUser(StudentName).UserFullName) : StudentName; }
        }

        /// <summary>
        /// Full name of the cached CFI name, in case the instructor's account is ever deleted, or for ad-hoc endorsements.
        /// </summary>
        public string CFICachedName { get; set; }

        /// <summary>
        /// Return the full name of the instructor.
        /// </summary>
        public string CFIDisplayName
        {
            get
            {
                string szFullName = Profile.GetUser(InstructorName).UserFullName;
                return String.IsNullOrEmpty(InstructorName) || String.IsNullOrEmpty(szFullName) ? CFICachedName : szFullName;
            }
        }

        /// <summary>
        /// The category for this endorsement
        /// </summary>
        public EndorsementCategory Category { get; set; } = EndorsementCategory.None;

        /// <summary>
        /// Where did the categorization come from?
        /// </summary>
        public CategorySource Source { get; set; } = CategorySource.None;

        /// <summary>
        /// Is this a regular endorsement or an encapsulated image?
        /// </summary>
        public EndorsementKind Kind { get; set; } = EndorsementKind.Regular;

        /// <summary>
        /// For EndorsementKind.Image, what is the thumbfile for this?  Note that virtualpathid will depend on whether studentkind is external or internal
        /// </summary>
        public string ThumbFileName { get; set; } = null;

        /// <summary>
        /// For EndorsementKind.Image, if this was scanned, on what page was the endorsement text found?
        /// </summary>
        public int PageNumber { get; set; } = 0;
        /// <summary>
        /// For EndorsementKind.Image, the raw text of any OCR
        /// </summary>
        public string RawText { get; set; } = null;
        /// <summary>
        /// If this came from a template, lets preserve its source ID.  0 = unknown (legacy).
        /// </summary>
        public int SourceTemplateID { get; set; } = 0;
        #endregion

        #region Constructors
        public Endorsement(string szInstructorUsername)
        {
            InstructorName = szInstructorUsername;
            StudentName = CFICertificate = EndorsementText = Title = FARReference = string.Empty;
            StudentType = StudentTypes.Member;
            Date = CreationDate = CFIExpirationDate = DateTime.Now;
            ID = -1;
        }

        protected Endorsement(MySqlDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
            CFICertificate = dr["CFINum"].ToString();
            StudentName = dr["Student"].ToString();
            StudentType = (StudentTypes)Convert.ToInt16(dr["StudentType"], CultureInfo.InvariantCulture);
            ID = Convert.ToInt32(dr["id"], CultureInfo.InvariantCulture);
            InstructorName = dr["CFI"].ToString();
            Date = Convert.ToDateTime(dr["Date"], CultureInfo.InvariantCulture);
            DateDeleted = dr["DateDeleted"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(dr["DateDeleted"], CultureInfo.InvariantCulture);
            CreationDate = Convert.ToDateTime(util.ReadNullableField(dr, "DateCreated", DateTime.MinValue), CultureInfo.InvariantCulture);
            CFIExpirationDate = Convert.ToDateTime(dr["CFIExpiration"], CultureInfo.InvariantCulture);
            EndorsementText = dr["Endorsement"].ToString();
            Title = dr["Title"].ToString();
            FARReference = dr["FARRef"].ToString();
            CFICachedName = util.ReadNullableString(dr, "CFIFullName");

            Category = (EndorsementCategory)Convert.ToInt32(dr["Category"], CultureInfo.InvariantCulture);
            Source = (CategorySource) Convert.ToInt32(dr["CategorySource"], CultureInfo.InvariantCulture);
            Kind = (EndorsementKind)Convert.ToInt32(dr["Kind"], CultureInfo.InvariantCulture);
            ThumbFileName = util.ReadNullableString(dr, "ThumbFileName");
            PageNumber = Convert.ToInt32(dr["PageNumber"], CultureInfo.InvariantCulture);
            RawText = util.ReadNullableString(dr, "RawText");
            SourceTemplateID = Convert.ToInt32(dr["TemplateID"], CultureInfo.InvariantCulture);

            byte[] rgb;

            if (!(dr["FileSize"] is DBNull))
            {
                int FileSize = Convert.ToInt32(dr["FileSize"], CultureInfo.InvariantCulture);
                rgb = new byte[FileSize];
                dr.GetBytes(dr.GetOrdinal("DigitizedSignature"), 0, rgb, 0, FileSize);
            }
            else
                rgb = CFIStudentMap.DefaultScribbleForInstructor(InstructorName);

            if (rgb != null && rgb.Length == 0)
                rgb = null;

            SetDigitizedSig(rgb);
        }

        /// <summary>
        /// Do not use - meant only for passing parameters
        /// </summary>
        public Endorsement() : this(string.Empty) { }
        #endregion

        protected void Validate()
        {
            if (StudentName.Length == 0)
                throw new MyFlightbookException(Resources.SignOff.errNoStudent);

            if (Date.CompareTo(DateTime.Now.AddDays(1).Date) > 0)
                throw new MyFlightbookException(Resources.SignOff.errNoPostDating);

            if (GetDigitizedSig() == null || GetDigitizedSig().Length == 0)
            {
                // Not an ad-hoc endorsement: requires a valid instructor name and a valid expiration date
                if (String.IsNullOrWhiteSpace(InstructorName))
                    throw new MyFlightbookException(Resources.SignOff.errNoInstructor);
                bool fOptionalExpiration = DateTime.Now.Date.CompareTo(new DateTime(2024, 12, 1, 0, 0, 0)) >= 0;
                if ((!fOptionalExpiration && !CFIExpirationDate.HasValue()) || (CFIExpirationDate.HasValue() && CFIExpirationDate.CompareTo(Date) < 0))
                    throw new MyFlightbookException(Resources.SignOff.errExpiredCertificate);

                if (StudentType == StudentTypes.Member && !new CFIStudentMap(InstructorName).IsInstructorOf(StudentName))
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.SignOff.errNoRelationship, InstructorName, StudentName));
            }
            else
            {
                // Verify that we don't also have an instructor name
                if (!String.IsNullOrWhiteSpace(InstructorName))
                    throw new MyFlightbookValidationException(Resources.SignOff.errBothScribbleAndCFI);
                // And no expiration is OK (e.g., for ground instructors), but it needs to be after date of issuance of endorsement
                if (CFIExpirationDate.HasValue() && CFIExpirationDate.CompareTo(Date) < 0)
                    throw new MyFlightbookException(Resources.SignOff.errExpiredCertificate);
            }
            if (CFICertificate.Length == 0)
                throw new MyFlightbookException(Resources.SignOff.errNoCertificate);
            if (EndorsementText.Length == 0)
                throw new MyFlightbookException(Resources.SignOff.errNoEndorsement);
            if (Title.Length == 0)
                throw new MyFlightbookException(Resources.SignOff.errNoTitle);
        }

        /// <summary>
        /// Commits the endorsement.  The bulk of an endorsement is only ever read-only, so this will fail if it is not new
        /// </summary>
        /// <exception cref="MyFlightbookException"></exception>
        public void FCommit()
        {
            if (this.ID >= 0)
                throw new InvalidOperationException("Can't update an endorsement");
            string szQ = "INSERT INTO endorsements SET CFI=?cfi, CFIFullName=?cfifullname, Student=?Student, StudentType=?studentType, Date=?dt, DateCreated=Now(), DateDeleted=NULL, CFINum=?cfiNumber, CFIExpiration=?dtExp, Endorsement=?bodytext, Title=?title, FARRef=?farref, DigitizedSignature=?digsig, Category=?category, CategorySource = ?source, Kind = ?kind, ThumbFileName = ?thumbfilename, PageNumber = ?pageNum, RawText = ?rawText, TemplateID = ?templateID";

            Validate();

            DBHelper dbh = new DBHelper(szQ);
            dbh.DoNonQuery((cmd) =>
            {
                cmd.Parameters.AddWithValue("id", ID);
                cmd.Parameters.AddWithValue("cfi", InstructorName);
                cmd.Parameters.AddWithValue("Student", StudentName);
                cmd.Parameters.AddWithValue("studentType", (int)StudentType);
                cmd.Parameters.AddWithValue("dt", Date);
                cmd.Parameters.AddWithValue("cfiNumber", CFICertificate);
                cmd.Parameters.AddWithValue("dtExp", CFIExpirationDate);
                cmd.Parameters.AddWithValue("bodytext", EndorsementText);
                cmd.Parameters.AddWithValue("title", Title);
                cmd.Parameters.AddWithValue("farref", FARReference);
                cmd.Parameters.AddWithValue("cfifullname", IsAdHocEndorsement ? CFICachedName : Profile.GetUser(InstructorName).UserFullName);
                cmd.Parameters.AddWithValue("digsig", IsAdHocEndorsement ? GetDigitizedSig() : null);
                cmd.Parameters.AddWithValue("category", (int)Category);
                cmd.Parameters.AddWithValue("source", (int)Source);
                cmd.Parameters.AddWithValue("kind", (int)Kind);
                cmd.Parameters.AddWithValue("thumbfilename", ThumbFileName);
                cmd.Parameters.AddWithValue("pageNum", PageNumber);
                cmd.Parameters.AddWithValue("rawText", RawText);
                cmd.Parameters.AddWithValue("templateID", SourceTemplateID);
            });

            if (dbh.LastError.Length > 0)
                throw new MyFlightbookException(Resources.SignOff.errCommitFailed + dbh.LastError);

            ID = dbh.LastInsertedRowId;
        }

        /// <summary>
        /// Delete the specified endorsement.  Note that the endorsement is never actually removed; it is simply marked as deleted.
        /// </summary>
        public void FDelete()
        {
            DBHelper dbh = new DBHelper("UPDATE endorsements SET DateDeleted=Now() WHERE StudentType=?st AND ID=?id");
            dbh.DoNonQuery((cmd) =>
            {
                cmd.Parameters.AddWithValue("id", this.ID);
                cmd.Parameters.AddWithValue("st", (int)this.StudentType);
            });
        }

        /// <summary>
        /// Updates the category for the specified endorsement.  This is entirely user controlled - does NOT touch the body of the endorsement
        /// </summary>
        /// <param name="ec">The new category</param>
        /// <param name="cs">The source of the category (user by default)</param>
        public void FUpdateCategory(EndorsementCategory ec, CategorySource cs = CategorySource.User)
        {
            if (ID < 0)
                throw new InvalidOperationException("Can't update an endorsement that hasn't been committed");
            DBHelper dbh = new DBHelper("UPDATE endorsements SET Category=?cat, CategorySource = ?src WHERE StudentType=?st AND ID=?id");
            dbh.DoNonQuery((cmd) =>
            {
                cmd.Parameters.AddWithValue("id", ID);
                cmd.Parameters.AddWithValue("st", (int)StudentType);
                cmd.Parameters.AddWithValue("cat", (int)ec);
                cmd.Parameters.AddWithValue("src", (int) cs);
            });
        }

        /// <summary>
        /// Returns the endorsements for a user
        /// </summary>
        /// <param name="szInstructor">The username of the instructor, or null/empty for all records for the student</param>
        /// <param name="szStudent">The username of the student, or null/empty for all records for the instructor</param>
        /// <param name="fIncludeDeleted">True to include deleted endorsements</param>
        /// <returns>An array of Endorsement objects representing the associated users</returns>
        public static IEnumerable<Endorsement> EndorsementsForUser(string szStudent, string szInstructor, bool fIncludeDeleted = false)
        {
            if (String.IsNullOrEmpty(szStudent) && String.IsNullOrEmpty(szInstructor))
                throw new MyFlightbookException(Resources.SignOff.errNoStudentOrInstructor);

            List<string> lstConditions = new List<string>();
            if (!String.IsNullOrEmpty(szStudent))
                lstConditions.Add("Student=?student");
            if (!String.IsNullOrEmpty(szInstructor))
                lstConditions.Add("CFI=?cfi");
            if (!fIncludeDeleted)
                lstConditions.Add("DateDeleted IS NULL");

            string szRestrict = String.Join(" AND ", lstConditions);

            List<Endorsement> lst = new List<Endorsement>();
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "SELECT *, LENGTH(DigitizedSignature) AS FileSize FROM Endorsements WHERE {0} ORDER BY DATE DESC",
                szRestrict
                ));

            if (!dbh.ReadRows(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("student", szStudent);
                    comm.Parameters.AddWithValue("cfi", szInstructor);
                },
                (dr) => { lst.Add(new Endorsement(dr)); }
                ))
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, Resources.SignOff.errGetFailed, szStudent, szInstructor, dbh.LastError));

            return lst;
        }

        /// <summary>
        /// Removes all endorsements issued by the specified instructor from an enumerable of endorsements.  Useful for when viewing all student endorsements to see yours (as an instructor), followed by others.
        /// </summary>
        /// <param name="rgIn">The enumerable of endorsements</param>
        /// <param name="szInstructor">The instructor to strip</param>
        /// <returns></returns>
        public static IEnumerable<Endorsement> RemoveEndorsementsByInstructor(IEnumerable<Endorsement> rgIn, string szInstructor)
        {
            if (String.IsNullOrEmpty(szInstructor) || rgIn == null)
                return rgIn;
            List<Endorsement> lst = new List<Endorsement>(rgIn);
            lst.RemoveAll(e => e.InstructorName.CompareCurrentCultureIgnoreCase(szInstructor) == 0);
            return lst;
        }

        /// <summary>
        /// Populates a data table from the set of endorsements
        /// </summary>
        /// <param name="rgEndorsements">The endorsements that provide the data</param>
        /// <param name="dt">The data table to populate; the caller is responsible for disposing.</param>
        public static void EndorsementsToDataTable(IEnumerable<Endorsement> rgEndorsements, DataTable dt)
        {
            if (rgEndorsements == null)
                throw new ArgumentNullException(nameof(rgEndorsements));
            if (dt == null)
                throw new ArgumentNullException(nameof(dt));

            dt.Columns.Add(new DataColumn(Resources.SignOff.EditEndorsementDatePrompt, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.SignOff.DownloadEndorsementFARRef, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.SignOff.DownloadEndorsementText, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.SignOff.EditEndorsementDateCreatedPrompt, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.SignOff.EditEndorsementStudentPrompt, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.SignOff.EditEndorsementInstructorPrompt, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.SignOff.EditEndorsementCFIPrompt, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.SignOff.EditEndorsementExpirationPrompt, typeof(string)));
            dt.Columns.Add(new DataColumn(Resources.LocalizedText.Note, typeof(string)));

            foreach (Endorsement e in rgEndorsements)
            {
                DataRow dr = dt.NewRow();

                dr[0] = e.Date.YMDString();
                dr[1] = e.FullTitleAndFar;
                dr[2] = e.EndorsementText;
                dr[3] = e.CreationDate.YMDString();
                dr[4] = e.StudentDisplayName;
                dr[5] = e.CFIDisplayName;
                dr[6] = e.CFICertificate;
                dr[7] = e.CFIExpirationDate.YMDString();
                dr[8] = e.DateDeleted.HasValue ? String.Format(CultureInfo.CurrentCulture, Resources.SignOff.EndorsementDeleted, e.DateDeleted.Value) : string.Empty;

                dt.Rows.Add(dr);
            }
        }

        public static Endorsement EndorsementWithID(int id)
        {
            DBHelper dbh = new DBHelper("SELECT *, LENGTH(DigitizedSignature) AS FileSize FROM Endorsements WHERE id=?idEndorsement");
            Endorsement en = null;
            dbh.ReadRow((comm) => { comm.Parameters.AddWithValue("idEndorsement", id); }, (dr) => { en = new Endorsement(dr); });
            return en;
        }

        /// <summary>
        /// Renders the endorsement to HTML.  We render explicitly so that writing to a ZIP - which may have neither page context nor HttpContext.Current - can work.
        /// </summary>
        /// <param name="tw"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void RenderHTML(HtmlTextWriter tw)
        {
            if (tw == null)
                throw new ArgumentNullException(nameof(tw));

            tw.AddAttribute("style", "padding: 5px;");
            tw.RenderBeginTag(HtmlTextWriterTag.Div);

            tw.AddAttribute("class", "endorsement");
            tw.RenderBeginTag(HtmlTextWriterTag.Table);
            if (IsExternalEndorsement)
            {
                // disclaimer
                tw.RenderBeginTag(HtmlTextWriterTag.Tr);
                tw.AddAttribute("style", "font-weight: bold; background-color:darkgray; color:white");
                tw.AddAttribute("colspan", "2");
                tw.RenderBeginTag(HtmlTextWriterTag.Td);
                tw.Write(Branding.ReBrand(Resources.SignOff.ExternalEndorsementDisclaimer));
                tw.RenderEndTag(); // td
                tw.RenderEndTag(); // tr
            }

            if (DateDeleted != null)
            {
                tw.RenderBeginTag(HtmlTextWriterTag.Tr);
                tw.AddAttribute("style", "font-weight: bold; background-color:darkgray; color:red");
                tw.AddAttribute("colspan", "2");
                tw.RenderBeginTag(HtmlTextWriterTag.Td);
                tw.Write(String.Format(CultureInfo.CurrentCulture, Resources.SignOff.EndorsementDeleted, DateDeleted.Value));
                tw.RenderEndTag();  // td
                tw.RenderEndTag();  // tr
            }

            // Body text
            tw.RenderBeginTag(HtmlTextWriterTag.Tr);
            tw.AddAttribute("colspan", "2");
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            if (!IsAdHocEndorsement)
            {
                tw.AddAttribute("style", "float:right; margin: 3px;");
                tw.AddAttribute("title", Resources.SignOff.EndorsementValid);
                tw.AddAttribute("alt", Resources.SignOff.EndorsementValid);
                tw.RenderBeginTag(HtmlTextWriterTag.Div);
                tw.AddAttribute("src", "~/images/sigok.png".ToAbsoluteBrandedUri().ToString());
                tw.RenderBeginTag(HtmlTextWriterTag.Img);
                tw.RenderEndTag();  // img
                tw.RenderEndTag();  // div
            }

            // Text and FAR row
            tw.AddAttribute("style", "font-weight:bold;");
            tw.RenderBeginTag(HtmlTextWriterTag.Div);
            tw.Write(HttpUtility.HtmlEncode(FullTitleAndFar));
            tw.RenderEndTag();  // div

            tw.RenderBeginTag(HtmlTextWriterTag.Hr);
            tw.RenderEndTag();  // hr

            tw.Write(HttpUtility.HtmlEncode(EndorsementText));

            tw.RenderBeginTag(HtmlTextWriterTag.Hr);
            tw.RenderEndTag();  // hr

            tw.RenderEndTag(); // td
            tw.RenderEndTag(); // row

            // Date row
            tw.RenderBeginTag(HtmlTextWriterTag.Tr);
            tw.AddAttribute("style", "font-weight: bold;");
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(Resources.SignOff.EditEndorsementDatePrompt);
            tw.RenderEndTag();  // td
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(Date.ToShortDateString());
            tw.RenderEndTag();  // td
            tw.RenderEndTag();  // trw

            // if dates are off row
            if (CreationDate.Date.CompareTo(Date.Date) != 0)
            {
                tw.RenderBeginTag(HtmlTextWriterTag.Tr);
                tw.AddAttribute("style", "font-weight: bold;");
                tw.RenderBeginTag(HtmlTextWriterTag.Td);
                tw.Write(Resources.SignOff.EditEndorsementDateCreatedPrompt);
                tw.RenderEndTag();  // td

                if (CreationDate.Date.Subtract(Date).Days > 10)
                    tw.AddAttribute("style", "font-weight: bold;");
                tw.RenderBeginTag(HtmlTextWriterTag.Td);
                tw.Write(CreationDate.ToShortDateString());
                tw.Write(" (UTC)");
                tw.RenderEndTag();  // td

                tw.RenderEndTag();  // tr
            }

            // Student name row
            tw.RenderBeginTag(HtmlTextWriterTag.Tr);
            tw.AddAttribute("style", "font-weight: bold");
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(Resources.SignOff.EditEndorsementStudentPrompt);
            tw.RenderEndTag();  // td

            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(HttpUtility.HtmlEncode(StudentDisplayName));
            tw.RenderEndTag();  // td
            tw.RenderEndTag();  // tr

            // CFI Display nam row
            tw.RenderBeginTag(HtmlTextWriterTag.Tr);
            tw.AddAttribute("style", "font-weight: bold");
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(Resources.SignOff.EditEndorsementInstructorPrompt);
            tw.RenderEndTag();  // td

            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(HttpUtility.HtmlEncode(CFIDisplayName));
            tw.RenderEndTag();  // td
            tw.RenderEndTag();  // tr

            // CFI Certificate Row
            tw.RenderBeginTag(HtmlTextWriterTag.Tr);
            tw.AddAttribute("style", "font-weight: bold");
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(Resources.SignOff.EditEndorsementCFIPrompt);
            tw.RenderEndTag();  // td

            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(HttpUtility.HtmlEncode(CFICertificate));
            tw.RenderEndTag();  // td

            tw.RenderEndTag();  // tr

            // CFI Certificate Expiration Row
            tw.RenderBeginTag(HtmlTextWriterTag.Tr);
            tw.AddAttribute("style", "font-weight: bold");
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(Resources.SignOff.EditEndorsementExpirationPrompt);
            tw.RenderEndTag();  // td
            tw.RenderBeginTag(HtmlTextWriterTag.Td);
            tw.Write(CFIExpirationDate.HasValue() ? CFIExpirationDate.ToShortDateString() : Resources.SignOff.EndorsementNoDate);
            tw.RenderEndTag();  // td
            tw.RenderEndTag();  // tr

            // Digitized Signature row
            if (HasDigitizedSig)
            {
                tw.RenderBeginTag(HtmlTextWriterTag.Tr);
                tw.AddAttribute("colspan", "2");
                tw.RenderBeginTag(HtmlTextWriterTag.Td);
                tw.AddAttribute("src", DigitizedSigLink);
                tw.RenderBeginTag(HtmlTextWriterTag.Img);
                tw.RenderEndTag();  // td
                tw.RenderEndTag();  // img
                tw.RenderEndTag();  // tr
            }

            tw.RenderEndTag();  // table

            tw.RenderEndTag();  // div
        }

        public string RenderHTML()
        {
            using (StringWriter sw = new StringWriter())
            {
                using (HtmlTextWriter tw = new HtmlTextWriter(sw))
                {
                    RenderHTML(tw);
                    return sw.ToString();
                }
            }
        }
    }

    /// <summary>
    /// And interface for the endorsement control to implement, allowing it to be called by app_code
    /// </summary>
    public interface IEndorsementListUpdate
    {
        /// <summary>
        /// Specifies the endorsement to display
        /// </summary>
        /// <param name="e">The endorsement</param>
        void SetEndorsement(Endorsement e);

        /// <summary>
        /// In case we don't have a context (e.g., nightly backup), render the control to a textwriter.
        /// </summary>
        /// <param name="tw"></param>
        void RenderHTML(Endorsement e, System.Web.UI.HtmlTextWriter tw);
    }

    public class EndorsementType
    {
        public const int IDCustomTemplate = 1;

        #region Properties
        /// <summary>
        /// The ID of the endorsement type
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// A reference to the FAR, if appropriate
        /// </summary>
        public string FARReference { get; set; }

        /// <summary>
        /// The template for the body, will have substitutions
        /// </summary>
        [Required]
        [DataType(DataType.MultilineText)]
        public string BodyTemplate { get; set; }

        /// <summary>
        /// The title of the template
        /// </summary>
        [Required]
        public string Title { get; set; }

        /// <summary>
        /// Returns the full title for the endorsement type (including any relevant FAR
        /// </summary>
        public string FullTitle
        {
            get { return String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, (FARReference.Length > 0) ? String.Format(CultureInfo.CurrentCulture, "({0})", FARReference) : String.Empty, Title); }
        }

        public EndorsementCategory DefaultCategory { get; set; } = EndorsementCategory.None;
        #endregion

        #region constructors
        public EndorsementType()
        {
            ID = -1;
            Title = BodyTemplate = FARReference = string.Empty;
        }

        protected EndorsementType(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
            ID = Convert.ToInt32(dr["id"], CultureInfo.InvariantCulture);
            FARReference = dr["FARRef"].ToString();
            Title = dr["Title"].ToString();
            BodyTemplate = dr["Text"].ToString();
            DefaultCategory = (EndorsementCategory) Convert.ToInt32(util.ReadNullableField(dr, "DefaultCategory", (int)EndorsementCategory.None), CultureInfo.InvariantCulture);
        }
        #endregion

        public void FCommit()
        {
            string szQ = (this.ID < 0)
                ? "INSERT INTO endorsementtemplates SET FARRef=?fref, Title=?title, Text=?txt, DefaultCategory=?defaultcat"
                : "UPDATE endorsementtemplates SET FARRef=?fref, Title=?title, Text=?txt, DefaultCategory=?defaultcat WHERE ID=?id";

            DBHelper dbh = new DBHelper(szQ);
            dbh.DoNonQuery(
                (cmd) =>
                {
                    cmd.Parameters.AddWithValue("id", this.ID);
                    cmd.Parameters.AddWithValue("fref", this.FARReference);
                    cmd.Parameters.AddWithValue("txt", this.BodyTemplate);
                    cmd.Parameters.AddWithValue("title", this.Title);
                    cmd.Parameters.AddWithValue("defaultcat", (int)this.DefaultCategory);
                });
        }

        public static IEnumerable<EndorsementType> LoadTemplates(string szSearch = null)
        {
            string szRestriction = string.Empty;
            List<MySqlParameter> lstParams = new List<MySqlParameter>();

            string[] rgTerms = (szSearch == null) ? Array.Empty<string>() : RegexUtility.WhiteSpace.Split(szSearch);

            if (rgTerms != null && rgTerms.Length > 0)
            {
                List<string> lstWhere = new List<string>();
                int i = 1;
                foreach (string szTerm in rgTerms)
                {
                    if (!string.IsNullOrEmpty(szTerm))
                    {
                        MySqlParameter param = new MySqlParameter(String.Format(CultureInfo.InvariantCulture, "param{0}", i++), String.Format(CultureInfo.InvariantCulture, "%{0}%", szTerm));
                        lstParams.Add(param);
                        lstWhere.Add(String.Format(CultureInfo.InvariantCulture, " (CONCAT(Title, ' ', FarRef, ' ', Text) LIKE ?{0}) ", param.ParameterName));
                    }
                }

                if (lstWhere.Count > 0)
                    szRestriction = String.Format(CultureInfo.InvariantCulture, " WHERE {0} ", string.Join(" AND ", lstWhere));
            }

            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM endorsementtemplates {0} ORDER BY FARRef", szRestriction));
            List<EndorsementType> lst = new List<EndorsementType>();

            if (!dbh.ReadRows((comm) =>
            {
                foreach (MySqlParameter p in lstParams)
                    comm.Parameters.Add(p);
            },
                (dr) => { lst.Add(new EndorsementType(dr)); }))
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Unable to load templates: {0}", dbh.LastError));

            return lst;
        }

        /// <summary>
        /// Retrieve a single endorsement by ID
        /// </summary>
        /// <param name="id">The ID of the requested template</param>
        /// <returns>The requested template, NULL if not found.</returns>
        public static EndorsementType GetEndorsementByID(int id)
        {
            IEnumerable<EndorsementType> rg = LoadTemplates();
            foreach (EndorsementType et in rg)
                if (et.ID == id)
                    return et;
            return null;
        }

        public static string EndorsementMajorCategory(int category)
        {
            switch (category)
            {
                case 0: return Resources.Endorsements.EndorsementCategoryNone;
                case 1: return Resources.Endorsements.EndorsementCategoryStudent;
                case 2: return Resources.Endorsements.EndorsementCategoryTestPrep;
                case 3: return Resources.Endorsements.EndorsementCategoryAircraft;
                case 4: return Resources.Endorsements.EndorsementCategoryReviews;
                case 5: return Resources.Endorsements.EndorsementCategoryWINGS;
                case 6: return Resources.Endorsements.EndorsementCategoryInstructor;
                case 7: return Resources.Endorsements.EndorsementCategorySecurity;
                case 8: return Resources.Endorsements.EndorsementCategoryDocuments;
                case 9: return Resources.Endorsements.EndorsementCategoryOther;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), $"Endorsement category {category} is not in a valid range");
            }
        }

        public static string EndorsementMajorCategory(EndorsementCategory category)
        {
            return EndorsementMajorCategory(MajorCategoryForSubCategory(category));
        }

        public static int MajorCategoryForSubCategory(EndorsementCategory category)
        {
            return (int)category / 100;
        }

#pragma warning disable CA1502 
        public static string EndorsementCategoryName(EndorsementCategory category)
        {
            switch (category)
            {
                case EndorsementCategory.None: return Resources.Endorsements.EndorsementCategoryNone;
                case EndorsementCategory.PresoloKnowledge: return Resources.Endorsements.SubCatPresoloKnowledge;
                case EndorsementCategory.PresoloFlight: return Resources.Endorsements.SubCatPresoloFlight;
                case EndorsementCategory.Solo: return Resources.Endorsements.SubCatSolo;
                case EndorsementCategory.SoloLocalAndAirspace: return Resources.Endorsements.SubCatSoloLocalAndAirspace;
                case EndorsementCategory.SoloCrossCountry: return Resources.Endorsements.SubCatSoloCrossCountry;
                case EndorsementCategory.RecreationalPrivileges: return Resources.Endorsements.SubCatRecreationalPrivileges;
                case EndorsementCategory.StageCheck: return Resources.Endorsements.SubCatStageCheck;
                case EndorsementCategory.KnowledgeTestPrep: return Resources.Endorsements.SubCatKnowledgeTestPrep;
                case EndorsementCategory.KnowledgeTestDeficiencies: return Resources.Endorsements.SubCatKnowledgeTestDeficiencies;
                case EndorsementCategory.PracticalTestPrep: return Resources.Endorsements.SubCatPracticalTestPrep;
                case EndorsementCategory.Retest: return Resources.Endorsements.SubCatRetest;
                case EndorsementCategory.TestResult: return Resources.Endorsements.SubCatTestResult;
                case EndorsementCategory.CertificationTrainingProgram: return Resources.Endorsements.SubCatCertificationTrainingProgram;
                case EndorsementCategory.Complex: return Resources.Endorsements.SubCatComplex;
                case EndorsementCategory.HighPerformance: return Resources.Endorsements.SubCatHighPerformance;
                case EndorsementCategory.Tailwheel: return Resources.Endorsements.SubCatTailwheel;
                case EndorsementCategory.HighAltitude: return Resources.Endorsements.SubCatHighAltitude;
                case EndorsementCategory.WithoutRating: return Resources.Endorsements.SubCatWithoutRating;
                case EndorsementCategory.GliderAndTowing: return Resources.Endorsements.SubCatGliderAndTowing;
                case EndorsementCategory.NightVisionGoggles: return Resources.Endorsements.SubCatNightVisionGoggles;
                case EndorsementCategory.LightSport: return Resources.Endorsements.SubCatLightSport;
                case EndorsementCategory.SecondInCommand: return Resources.Endorsements.SubCatSecondInCommand;
                case EndorsementCategory.TypeAndDifferences: return Resources.Endorsements.SubCatTypeAndDifferences;
                case EndorsementCategory.Checkout: return Resources.Endorsements.SubCatCheckout;
                case EndorsementCategory.OperationalAuthorization: return Resources.Endorsements.SubCatOperationalAuthorization;
                case EndorsementCategory.FlightReview: return Resources.Endorsements.SubCatFlightReview;
                case EndorsementCategory.InstrumentProficiencyCheck: return Resources.Endorsements.SubCatInstrumentProficiencyCheck;
                case EndorsementCategory.ProficiencyCheck: return Resources.Endorsements.SubCatProficiencyCheck;
                case EndorsementCategory.WingsActivity: return Resources.Endorsements.SubCatWingsActivity;
                case EndorsementCategory.WingsPhase: return Resources.Endorsements.SubCatWingsPhase;
                case EndorsementCategory.SafetyCourse: return Resources.Endorsements.SubCatSafetyCourse;
                case EndorsementCategory.SpinTraining: return Resources.Endorsements.SubCatSpinTraining;
                case EndorsementCategory.InstructorQualification: return Resources.Endorsements.SubCatInstructorQualification;
                case EndorsementCategory.GroundInstructorRecency: return Resources.Endorsements.SubCatGroundInstructorRecency;
                case EndorsementCategory.Citizenship: return Resources.Endorsements.SubCatCitizenship;
                case EndorsementCategory.SecurityAwareness: return Resources.Endorsements.SubCatSecurityAwareness;
                case EndorsementCategory.FlightTrainingSecurityProgram: return Resources.Endorsements.SubCatFlightTrainingSecurityProgram;
                case EndorsementCategory.PilotCertificate: return Resources.Endorsements.SubCatPilotCertificate;
                case EndorsementCategory.Medical: return Resources.Endorsements.SubCatMedical;
                case EndorsementCategory.LogbookPages: return Resources.Endorsements.SubCatLogbookPages;
                case EndorsementCategory.TrainingRecord: return Resources.Endorsements.SubCatTrainingRecord;
                case EndorsementCategory.OtherDocument: return Resources.Endorsements.SubCatOtherDocument;
                case EndorsementCategory.Other: return Resources.Endorsements.SubCatOther;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), $"Endorsement category {category} is not in a valid range");
            }
        }
#pragma warning restore CA1502
    }

    public enum EndorsementCategory
    {
        None = 0,
        /* 100 -199: Student Endorsements */
        PresoloKnowledge = 101, /* E.g. 61.87(b) */
        PresoloFlight = 102, /* E.g. 61.87(c) */
        Solo = 103, /* E.g. 61.87(n)/(p)/(o), SFAR 73 2(b)(3)/(4), "cleared for solo" */
        SoloLocalAndAirspace = 104, /* E.g. 61.93(b)(1), 61.94, 61.95, 61.101(b)/(d) */
        SoloCrossCountry = 105, /* E.g. 61.93(c), 61.93(b)(2), 61.101(c) */
        RecreationalPrivileges = 106, /* E.g. 61.101(g)/(j) */
        StageCheck = 107, /* E.g. stage/progress checks, "10-hour dual check", SACAA progress checks */
        /* 200 - 299 : Test Prep and Checkrides */
        KnowledgeTestPrep = 201, /* E.g. 61.35, 61.105(a), 61.65(b), 61.125, home study, 61.183(d)/(f), 61.405(a)(1) */
        KnowledgeTestDeficiencies = 202, /* E.g. 61.39(a)(6)(iii) */
        PracticalTestPrep = 203, /* E.g. 61.39(a), 61.107/109, 61.65(c)/(d), 61.127/129, 61.63, 61.157, 61.160, 61.183(g), 61.187, 61.309–313, 61.321 (ready), 61.409–419 (ready) */
        Retest = 204, /* E.g. 61.49 */
        TestResult = 205, /* E.g. 61.189 record of test result, "PASSED" titles, knowledge test reports, 61.321/61.409 passed */
        CertificationTrainingProgram = 206, /* E.g. 61.153(e) ATP CTP */
        /* 300 - 399 : Aircraft & Privileges */
        Complex = 301, /* E.g. 61.31(e) */
        HighPerformance = 302, /* E.g. 61.31(f) */
        Tailwheel = 303, /* E.g. 61.31(i) */
        HighAltitude = 304, /* E.g. 61.31(g) */
        WithoutRating = 305, /* E.g. 61.31(d)(2) */
        GliderAndTowing = 306, /* E.g. 61.31(j), 61.69 */
        NightVisionGoggles = 307, /* E.g. 61.31(k) */
        LightSport = 308, /* E.g. 61.325, 61.327, 61.329 */
        SecondInCommand = 309, /* E.g. 61.55, 61.159(c), 135.99(c) */
        TypeAndDifferences = 310, /* E.g. SFAR 73 2(a)(3) and 2(b)(1)/(2), type/differences training, EASA variants */
        Checkout = 311, /* E.g. club, rental and insurance checkouts */
        OperationalAuthorization = 312, /* E.g. RVSM, PBN, oceanic RNP, DC SFRA, mountain, 90-day endorsement, 61.52 ultralight credit */
        /* 400 - 499 : Proficiency and Recency */
        FlightReview = 401, /* E.g. 61.56, SFAR 73 2(c) flight reviews */
        InstrumentProficiencyCheck = 402, /* E.g. 61.57(d) */
        ProficiencyCheck = 403, /* E.g. 61.57(g), 61.58, LPC/OPC/PPC, foreign revalidations */
        /* 500 - 599 : WINGS and Safety */
        WingsActivity = 501, /* E.g. AC 61-91 activity */
        WingsPhase = 502, /* E.g. AC 61-91 phase */
        SafetyCourse = 503, /* E.g. FAASTeam and other course certificates */
        /* 600 - 699 : Instructor and Training */
        SpinTraining = 601, /* E.g. 61.183(i), 61.405(b)(1)(ii) */
        InstructorQualification = 602, /* E.g. 61.195, SFAR 73 2(b)(5), "qualified to train first-time CFIs" */
        GroundInstructorRecency = 603, /* E.g. 61.217 */
        /* 700 - 799 : Security and Citizenship */
        Citizenship = 701, /* E.g. 49 CFR 1552.3(h), 1552.15(c), passports and birth certificates */
        SecurityAwareness = 702, /* E.g. 49 CFR 1552.23 */
        FlightTrainingSecurityProgram = 703, /* E.g. FTSP approvals */
        /* 800 - 899 : Documentation */
        PilotCertificate = 801, /* E.g. certificates and licenses, temporary airman certificates */
        Medical = 802, /* E.g. medicals, BasicMed */
        LogbookPages = 803, /* E.g. logbook scans */
        TrainingRecord = 804, /* E.g. ground instruction logs, training summaries, syllabus records */
        OtherDocument = 805, /* E.g. FCC permits, sim LOAs, ID cards */
        /* 900 - 999 : Other */
        Other = 901, /* E.g. classified, but fits nowhere */
    }
}