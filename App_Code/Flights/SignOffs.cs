using MyFlightbook.Clubs;
using MyFlightbook.Encryptors;
using MyFlightbook.Image;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2010-2019 MyFlightbook LLC
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

        /// <summary>
        /// A scribble-signature for a digitized sig
        /// </summary>
        public byte[] DigitizedSig { get; set; }

        public string DigitizedSigLink
        {
            get { return ScribbleImage.DataLinkForByteArray(DigitizedSig); }
        }

        /// <summary>
        /// Is this an ad-hoc endorsement (scribble signature vs. member instructor?)
        /// </summary>
        public bool IsAdHocEndorsement
        {
            get { return String.IsNullOrEmpty(InstructorName) && DigitizedSig != null && DigitizedSig.Length > 0; }
        }

        /// <summary>
        /// Username of the instructor
        /// </summary>
        protected string InstructorName { get; set; }

        /// <summary>
        /// Date of the endorsement
        /// </summary>
        public DateTime Date { get; set; }

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
                throw new ArgumentNullException("dr");
            CFICertificate = dr["CFINum"].ToString();
            StudentName = dr["Student"].ToString();
            StudentType = (StudentTypes)Convert.ToInt16(dr["StudentType"], CultureInfo.InvariantCulture);
            ID = Convert.ToInt32(dr["id"], CultureInfo.InvariantCulture);
            InstructorName = dr["CFI"].ToString();
            Date = Convert.ToDateTime(dr["Date"], CultureInfo.InvariantCulture);
            CreationDate = Convert.ToDateTime(util.ReadNullableField(dr, "DateCreated", DateTime.MinValue), CultureInfo.InvariantCulture);
            CFIExpirationDate = Convert.ToDateTime(dr["CFIExpiration"], CultureInfo.InvariantCulture);
            EndorsementText = dr["Endorsement"].ToString();
            Title = dr["Title"].ToString();
            FARReference = dr["FARRef"].ToString();
            CFICachedName = util.ReadNullableString(dr, "CFIFullName");

            if (!(dr["FileSize"] is DBNull))
            {
                int FileSize = Convert.ToInt32(dr["FileSize"], CultureInfo.InvariantCulture);
                DigitizedSig = new byte[FileSize];
                dr.GetBytes(dr.GetOrdinal("DigitizedSignature"), 0, DigitizedSig, 0, FileSize);
            }
            else
                DigitizedSig = null;
        }
        #endregion

        protected void Validate()
        {
            if (StudentName.Length == 0)
                throw new MyFlightbookException(Resources.SignOff.errNoStudent);

            if (DigitizedSig == null || DigitizedSig.Length == 0)
            {
                // Not an ad-hoc endorsement: requires a valid instructor name and a valid expiration date
                if (String.IsNullOrWhiteSpace(InstructorName))
                    throw new MyFlightbookException(Resources.SignOff.errNoInstructor);
                if (!CFIExpirationDate.HasValue() || CFIExpirationDate.CompareTo(Date) < 0)
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

        public void FCommit()
        {
            string szQ = (this.ID < 0) ? "INSERT INTO endorsements SET CFI=?cfi, CFIFullName=?cfifullname, Student=?Student, StudentType=?studentType, Date=?dt, DateCreated=Now(), CFINum=?cfiNumber, CFIExpiration=?dtExp, Endorsement=?bodytext, Title=?title, FARRef=?farref, DigitizedSignature=?digsig " :
                "UPDATE endorsements SET CFI=?cfi, Student=?Student, StudentType=?studentType, Date=?dt, CFINum=?cfiNumber, CFIExpiration=?dtExp, Endorsement=?bodytext, Title=?title, FARRef=?farref, DigitizedSignature=?digsig WHERE ID=?id";

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
                cmd.Parameters.AddWithValue("digsig", IsAdHocEndorsement ? DigitizedSig : null);
            });

            if (dbh.LastError.Length > 0)
                throw new MyFlightbookException(Resources.SignOff.errCommitFailed + dbh.LastError);

            if (ID < 0)
                ID = dbh.LastInsertedRowId;
        }

        /// <summary>
        /// Delete the specified endorsement.  Note that this ONLY WORKS for StudentType = External
        /// </summary>
        public void FDelete()
        {
            DBHelper dbh = new DBHelper("DELETE FROM endorsements WHERE StudentType=?st AND ID=?id");
            dbh.DoNonQuery((cmd) =>
            {
                cmd.Parameters.AddWithValue("id", this.ID);
                cmd.Parameters.AddWithValue("st", (int)this.StudentType);
            });
        }

        /// <summary>
        /// Returns the endorsements for a user
        /// </summary>
        /// <param name="szInstructor">The username of the instructor, or null/empty for all records for the student</param>
        /// <param name="szStudent">The username of the student, or null/empty for all records for the instructor</param>
        /// <returns>An array of Endorsement objects representing the associated users</returns>
        public static IEnumerable<Endorsement> EndorsementsForUser(string szStudent, string szInstructor)
        {
            Boolean fStudent = !String.IsNullOrEmpty(szStudent);
            Boolean fInstructor = !String.IsNullOrEmpty(szInstructor);

            if (!fStudent && !fInstructor)
                throw new MyFlightbookException(Resources.SignOff.errNoStudentOrInstructor);

            string szQStudent = (fStudent ? "Student=?student" : "");
            string szQInstructor = (fInstructor ? "CFI=?cfi" : "");

            string szRestrict = (fStudent ? szQStudent : "") + (fStudent && fInstructor ? " AND " : "") + (fInstructor ? szQInstructor : "");
            List<Endorsement> lst = new List<Endorsement>();
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "SELECT *, LENGTH(DigitizedSignature) AS FileSize FROM Endorsements WHERE {0}", szRestrict));

            if (!dbh.ReadRows(
                (comm) =>
                {
                    comm.Parameters.AddWithValue("student", szStudent);
                    comm.Parameters.AddWithValue("cfi", szInstructor);
                },
                (dr) => { lst.Add(new Endorsement(dr)); }
                ))
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture,Resources.SignOff.errGetFailed, szStudent, szInstructor, dbh.LastError));

            return lst;
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
    }

    public class EndorsementType
    {
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
        public string BodyTemplate { get; set; }

        /// <summary>
        /// The title of the template
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Returns the full title for the endorsement type (including any relevant FAR
        /// </summary>
        public string FullTitle
        {
            get { return String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LocalizedJoinWithSpace, (FARReference.Length > 0) ? String.Format(CultureInfo.CurrentCulture, "({0})", FARReference) : String.Empty, Title); }
        }
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
                throw new ArgumentNullException("dr");
            this.ID = Convert.ToInt32(dr["id"], CultureInfo.InvariantCulture);
            this.FARReference = dr["FARRef"].ToString();
            this.Title = dr["Title"].ToString();
            this.BodyTemplate = dr["Text"].ToString();
        }
        #endregion

        public void FCommit()
        {
            string szQ = (this.ID < 0) ? "INSERT INTO endorsementtemplates SET FARRef=?fref, Title=?title, Text=?txt" :
                "UPDATE endorsementtemplates SET FARRef=?fref, Title=?title, Text=?txt WHERE ID=?id";

            DBHelper dbh = new DBHelper(szQ);
            dbh.DoNonQuery(
                (cmd) =>
                {
                    cmd.Parameters.AddWithValue("id", this.ID);
                    cmd.Parameters.AddWithValue("fref", this.FARReference);
                    cmd.Parameters.AddWithValue("txt", this.BodyTemplate);
                    cmd.Parameters.AddWithValue("title", this.Title);
                });
        }

        public static IEnumerable<EndorsementType> LoadTemplates(string szSearch = null)
        {
            string szRestriction = string.Empty;
            List<MySqlParameter> lstParams = new List<MySqlParameter>();

            string[] rgTerms = (szSearch == null) ? new string[0] : Regex.Split(szSearch, "\\s", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture,"Unable to load templates:", dbh.LastError));

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
    }

    /// <summary>
    /// Encapsulates an outstanding request from one pilot to another to establish a relationship of one sort or another between them
    /// </summary>
    public class CFIStudentMapRequest
    {
        public enum RoleType { RoleStudent = 0, RoleCFI, RoleInviteJoinClub, RoleRequestJoinClub };

        #region Properties
        public string RequestingUser { get; set; }
        public string TargetUser { get; set; }
        public Club ClubToJoin { get; set; }
        public RoleType Requestedrole { get; set; }
        #endregion

        #region Constructors
        public CFIStudentMapRequest()
        {
            RequestingUser = TargetUser = string.Empty;
            ClubToJoin = null;
            Requestedrole = RoleType.RoleStudent;
        }

        /// <summary>
        /// Create a new request from the specified string
        /// </summary>
        /// <param name="szInit">The initialization string</param>
        public CFIStudentMapRequest(string szInit) : this()
        {
            InitFromString(szInit);
        }

        /// <summary>
        /// Create a new request with parameters
        /// </summary>
        /// <param name="szRequestingUser">Username of the requesting user</param>
        /// <param name="szTargetEmail">EMAIL address of the target user</param>
        /// <param name="clubToJoin">The club to join, if appropriate</param>
        /// <param name="rt">Relationship request.</param>
        public CFIStudentMapRequest(string szRequestingUser, string szTargetEmail, RoleType rt, Club clubToJoin = null) : this()
        {
            RequestingUser = szRequestingUser;
            TargetUser = szTargetEmail;
            Requestedrole = rt;
            ClubToJoin = clubToJoin;
        }
        #endregion

        /// <summary>
        /// Create an initialization string for this request
        /// </summary>
        /// <returns>The string</returns>
        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture,"{0};{1};{2};{3}", ((int)Requestedrole).ToString(CultureInfo.InvariantCulture), RequestingUser, TargetUser, ClubToJoin == null ? Club.ClubIDNew : ClubToJoin.ID);
        }

        /// <summary>
        /// Initializes the request from a string (mirrors ToString())
        /// </summary>
        /// <param name="sz">The string</param>
        private void InitFromString(string sz)
        {
            string[] rgParams = sz.Split(';');
            Requestedrole = (RoleType)Convert.ToInt32(rgParams[0], CultureInfo.InvariantCulture);
            RequestingUser = rgParams[1];
            TargetUser = rgParams[2];
            ClubToJoin = null;
            int id;
            if (rgParams.Length > 2 && (Requestedrole == RoleType.RoleInviteJoinClub || Requestedrole == RoleType.RoleRequestJoinClub)
                && Int32.TryParse(rgParams[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
                ClubToJoin = Club.ClubWithID(id); 
        }

        /// <summary>
        /// Initializes the request from an encrypted string
        /// </summary>
        /// <param name="szRequest">The encrypted request string</param>
        public void DecryptRequest(string szRequest)
        {
            InitFromString(new PeerRequestEncryptor().Decrypt(szRequest));
        }

        /// <summary>
        /// Creates an encrypted string for the request which can be passed around.
        /// </summary>
        /// <returns>The encrypted string</returns>
        public string EncryptRequest()
        {
            return new PeerRequestEncryptor().Encrypt(this.ToString());
        }

        /// <summary>
        /// Send the request via email
        /// </summary>
        public void Send()
        {
            Profile pf = Profile.GetUser(RequestingUser);
            using (MailMessage msg = new MailMessage())
            {
                msg.From = new MailAddress(Branding.CurrentBrand.EmailAddress, String.Format(CultureInfo.CurrentCulture, Resources.SignOff.EmailSenderAddress, Branding.CurrentBrand.AppName, pf.UserFullName));
                msg.ReplyToList.Add(new MailAddress(pf.Email, pf.UserFullName));
                msg.To.Add(new MailAddress(TargetUser));
                msg.Subject = String.Format(CultureInfo.CurrentCulture, Resources.SignOff.InvitationMessageSubject, Branding.CurrentBrand.AppName, pf.UserFullName);

                string szTemplate;
                string szRole = string.Empty;

                switch (Requestedrole)
                {
                    case RoleType.RoleCFI:
                        szTemplate = Branding.ReBrand(Resources.SignOff.RoleInvitation);
                        szRole = Resources.SignOff.RoleInstructor;
                        break;
                    case RoleType.RoleStudent:
                        szTemplate = Branding.ReBrand(Resources.SignOff.RoleInvitation);
                        szRole = Resources.SignOff.RoleStudent;
                        break;
                    case RoleType.RoleInviteJoinClub:
                        szTemplate = Branding.ReBrand(Resources.Club.ClubInviteJoin);
                        break;
                    case RoleType.RoleRequestJoinClub:
                        szTemplate = Branding.ReBrand(Resources.Club.ClubRequestJoin);
                        break;
                    default:
                        throw new MyFlightbookException("Invalid role");
                }

                string szCallBackUrl = String.Format(CultureInfo.InvariantCulture,"http://{0}{1}/Member/AddRelationship.aspx?req={2}", HttpContext.Current.Request.Url.Host, HttpContext.Current.Request.ApplicationPath, HttpContext.Current.Server.UrlEncode(EncryptRequest()));

                string szRequestor = String.Format(CultureInfo.CurrentCulture, "{0} ({1})", pf.UserFullName, pf.Email);

                msg.Body = szTemplate.Replace("<% Requestor %>", szRequestor).Replace("<% Role %>", szRole).Replace("<% ConfirmRoleLink %>", szCallBackUrl).Replace("<% ClubName %>", ClubToJoin == null ? string.Empty : ClubToJoin.Name);
                util.SendMessage(msg);
            }
        }
    }

    public class EndorsementEventArgs : EventArgs
    {
        public Endorsement Endorsement { get; set; }

        public EndorsementEventArgs(Endorsement e = null)
            : base()
        {
            this.Endorsement = e;
        }
    }

    public class CFIStudentMap
    {
        private const string szQStudentsForUser = "SELECT users.*, students.CanViewStudent FROM users INNER JOIN students ON users.username=students.StudentName WHERE students.CFIName=?uname";
        private const string szQInstructorsForUser = "SELECT users.*, students.CanViewStudent FROM users INNER JOIN students ON users.username=students.CFIName WHERE students.StudentName=?uname";

        #region Properties
        /// <summary>
        /// Returns the students for an instructor.  Call GetStudents first or this will be null
        /// </summary>
        public IEnumerable<InstructorStudent> Students { get; set; }

        /// <summary>
        /// Returns the instructors for a student.  Will be null if GetInstructors hasn't been called
        /// </summary>
        public IEnumerable<InstructorStudent> Instructors { get; set; }

        /// <summary>
        /// The user for whom this entitity 
        /// </summary>
        public string User { get; set; }
        #endregion

        /// <summary>
        /// Returns the students or instructors for the current user.
        /// </summary>
        /// <param name="fStudents">True to return students of the current user, false for instructors</param>
        /// <returns>An array of Profile objects representing the associated users</returns>
        private IEnumerable<InstructorStudent> GetAssociatedUsers(Boolean fStudents)
        {
            if (User.Length == 0)
                throw new MyFlightbookException("No user specified for GetAssociatedUsers");

            DBHelper dbh = new DBHelper(fStudents ? szQStudentsForUser : szQInstructorsForUser);
            List<InstructorStudent> lst = new List<InstructorStudent>();

            if (!dbh.ReadRows(
                (cmd) => { cmd.Parameters.AddWithValue("uname", User); },
                (dr) => { lst.Add(new InstructorStudent(dr)); }))
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture,"Unable to get {0} for user {1}; exception: {2}", fStudents ? "students" : "instructors", User, dbh.LastError));

            return lst;
        }

        /// <summary>
        /// Get the students associated with the current user
        /// </summary>
        /// <returns>An array of Student objects representing the students.</returns>
        private IEnumerable<InstructorStudent> LoadStudents()
        {
            return Students = GetAssociatedUsers(true);
        }

        /// <summary>
        /// Gets the instructors associated with the current user
        /// </summary>
        /// <returns>An array of profile objects representing the students.</returns>
        private IEnumerable<InstructorStudent> LoadInstructors()
        {
            return Instructors = GetAssociatedUsers(false);
        }

        /// <summary>
        /// Returns the instructorstudent in an array of them by username
        /// </summary>
        /// <param name="rgpf">An enumerable of instructor/students</param>
        /// <param name="szName">The name to match on</param>
        /// <returns>the resulting InstructorStudent, or null</returns>
        public InstructorStudent GetInstructorStudent(IEnumerable<InstructorStudent> rgpf, string szName)
        {
            if (rgpf == null)
                throw new ArgumentNullException("rgpf");
            foreach (InstructorStudent pf in rgpf)
                if (String.Compare(pf.UserName, szName, StringComparison.OrdinalIgnoreCase) == 0 || String.Compare(pf.Email, szName, StringComparison.OrdinalIgnoreCase) == 0)
                    return pf;
            return null;
        }

        /// <summary>
        /// Determines if the user is already a student of the specified user
        /// </summary>
        /// <param name="szInstructor">The name of the instructor</param>
        /// <returns>True if the user is a student of the instructor</returns>
        public Boolean IsStudentOf(string szInstructor)
        {
            if (Instructors == null)
                LoadInstructors();
            return (GetInstructorStudent(Instructors, szInstructor) != null);
        }

        /// <summary>
        /// Determines if the user is already an instructor of the specified user
        /// </summary>
        /// <param name="szStudent">The name of the student</param>
        /// <returns>True if the user is a student of the instructor</returns>
        public Boolean IsInstructorOf(string szStudent)
        {
            if (Students == null)
                LoadStudents();
            return (GetInstructorStudent(Students, szStudent) != null);
        }

        /// <summary>
        /// Executes the specified query on the row containing the specified student and instructor
        /// </summary>
        /// <param name="szQ">The query</param>
        /// <param name="szInstructor">The instructor</param>
        /// <param name="szStudent">The Student</param>
        private void ModifyTable(string szQ, string szInstructor, string szStudent)
        {
            new DBHelper(szQ).DoNonQuery(
                (cmd) =>
                {
                    cmd.Parameters.AddWithValue("student", szStudent);
                    cmd.Parameters.AddWithValue("cfi", szInstructor);
                });

            // refresh any tables.
            LoadStudents();
            LoadInstructors();
        }

        /// <summary>
        /// Inserts a record mapping a student to an instructor
        /// </summary>
        /// <param name="szInstructor">The instructor's name</param>
        /// <param name="szStudent">The student's name</param>
        private void InsertRecord(string szInstructor, string szStudent)
        {
            ModifyTable("INSERT INTO students SET StudentName=?student, CFIName=?cfi", szInstructor, szStudent);
        }

        /// <summary>
        /// Inserts a record mapping a student to an instructor
        /// </summary>
        /// <param name="szInstructor">The instructor's name</param>
        /// <param name="szStudent">The student's name</param>
        private void DeleteRecord(string szInstructor, string szStudent)
        {
            ModifyTable("DELETE FROM students WHERE StudentName=?student AND CFIName=?cfi", szInstructor, szStudent);
        }

        /// <summary>
        /// Adds another user as an instructor of the current user
        /// </summary>
        /// <param name="szInstructor">The username of the instructor to add</param>
        public void AddInstructor(string szInstructor)
        {
            if (!IsStudentOf(szInstructor))
                InsertRecord(szInstructor, User);
        }

        /// <summary>
        /// Adds another user as a student of the current user
        /// </summary>
        /// <param name="szStudent">The name of the student to add</param>
        public void AddStudent(string szStudent)
        {
            if (!IsInstructorOf(szStudent))
                InsertRecord(User, szStudent);
        }

        /// <summary>
        /// Removes an instructor for the current user
        /// </summary>
        /// <param name="szInstructor">The username of the instructor to remove</param>
        public void RemoveInstructor(string szInstructor)
        {
            if (!IsStudentOf(szInstructor))
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture,"User {0} is not an instructor of user {1}", szInstructor, User));
            DeleteRecord(szInstructor, User);
        }

        /// <summary>
        /// Removes a student for the current user
        /// </summary>
        /// <param name="szStudent">The username of the student to remove</param>
        public void RemoveStudent(string szStudent)
        {
            if (!IsInstructorOf(szStudent))
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture,"User {0} is not a student of user {1}", szStudent, User));
            DeleteRecord(User, szStudent);
        }

        /// <summary>
        /// Creates a request object for the current user to request a relationship with another user
        /// </summary>
        /// <param name="rt">The type of relationship being requested</param>
        /// <param name="szTargetEmail">The EMAIL address of the target of the request</param>
        /// <returns>CFIStudentMapRequest object</returns>
        public CFIStudentMapRequest GetRequest(CFIStudentMapRequest.RoleType rt, string szTargetEmail)
        {
            Profile pf = Profile.GetUser(User);
            if (String.Compare(pf.Email, szTargetEmail, StringComparison.OrdinalIgnoreCase) == 0)
                throw new MyFlightbookException(Resources.SignOff.errCantTeachSelf);

            switch (rt)
            {
                case CFIStudentMapRequest.RoleType.RoleCFI:
                    {
                        if (IsStudentOf(szTargetEmail))
                            throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.SignOff.errAlreadyInstructor, szTargetEmail, pf.UserFullName));
                    }
                    break;
                case CFIStudentMapRequest.RoleType.RoleStudent:
                    {
                        if (pf.Certificate.Length == 0)
                            throw new MyFlightbookException(Resources.SignOff.errNeedCertificate);

                        if (IsInstructorOf(szTargetEmail))
                            throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.SignOff.errAlreadyStudent, szTargetEmail, pf.UserFullName));
                    }
                    break;
                default:
                    throw new MyFlightbookException("Unknown role type");

            }

            return new CFIStudentMapRequest(User, szTargetEmail, rt);
        }

        /// <summary>
        /// Execute the request
        /// </summary>
        public void ExecuteRequest(CFIStudentMapRequest cmr)
        {
            if (cmr == null)
                throw new ArgumentNullException("cmr");
            Profile pf = Profile.GetUser(User);
            if (String.Compare(cmr.TargetUser, pf.Email, StringComparison.OrdinalIgnoreCase) != 0)
                throw new MyFlightbookValidationException(Resources.SignOff.errWrongAddress);

            Profile pfRequestor = Profile.GetUser(cmr.RequestingUser);
            if (!pfRequestor.IsValid())
                throw new MyFlightbookValidationException(Resources.SignOff.errUnknownRequestor);

            switch (cmr.Requestedrole)
            {
                case CFIStudentMapRequest.RoleType.RoleCFI:
                    // They are requesting that you (User) be a CFI, so they want to be the student.
                    AddStudent(cmr.RequestingUser);
                    break;
                case CFIStudentMapRequest.RoleType.RoleStudent:
                    // They are requesting that you (User) be a student, so they want to be your CFI.
                    AddInstructor(cmr.RequestingUser);
                    break;
                case CFIStudentMapRequest.RoleType.RoleInviteJoinClub:
                case CFIStudentMapRequest.RoleType.RoleRequestJoinClub:
                    if (cmr.ClubToJoin == null)
                        throw new MyFlightbookException(Resources.Club.errNoClubInRequest);
                    ClubMember cm = new ClubMember(cmr.ClubToJoin.ID, cmr.Requestedrole == CFIStudentMapRequest.RoleType.RoleInviteJoinClub ? pf.UserName : pfRequestor.UserName, ClubMember.ClubMemberRole.Member);
                    cm.FCommitClubMembership();
                    break;
                default:
                    throw new MyFlightbookValidationException(Resources.SignOff.errInvalidRequest);
            }
        }

        /// <summary>
        /// Set the permissions for the instructor to be able to view the student's logbook.
        /// </summary>
        /// <param name="instructorstudent"></param>
        public void SetCFIPermissions(InstructorStudent instructorstudent)
        {
            if (String.IsNullOrEmpty(this.User))
                throw new MyFlightbookException("No username for CFIStudentmap");

            DBHelper dbh = new DBHelper("UPDATE students SET CanViewStudent=?canView WHERE StudentName=?user AND CFIName=?instructor");
            dbh.DoNonQuery((comm) =>
                {
                    // Sanity check - can't add to logbook if you can't view it.
                    if (!instructorstudent.CanViewLogbook)
                        instructorstudent.CanAddLogbook = false;

                    uint perms = ((uint)(instructorstudent.CanViewLogbook ? InstructorStudent.PermissionMask.ViewLogbook : 0) | (uint) (instructorstudent.CanAddLogbook ? InstructorStudent.PermissionMask.AddLogbook : 0));
                    comm.Parameters.AddWithValue("canView", perms);
                    comm.Parameters.AddWithValue("user", this.User);
                    comm.Parameters.AddWithValue("instructor", instructorstudent.UserName);
                });
            // refresh
            LoadInstructors();
        }

        #region Constructors
        public CFIStudentMap()
        {
        }

        public CFIStudentMap(string szUser) : this()
        {
            User = szUser;
            LoadStudents();
            LoadInstructors();
        }
        #endregion
    }
}