using MyFlightbook.Clubs;
using MyFlightbook.Encryptors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mail;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2010-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Instruction
{
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
            if (szInit == null)
                throw new ArgumentNullException(nameof(szInit));
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
            if (sz == null)
                return;

            string[] rgParams = sz.Split(';');

            if (rgParams.Length < 3)
                throw new ArgumentException("Invalid request relationship request string: " + sz);

            Requestedrole = (RoleType)Convert.ToInt32(rgParams[0], CultureInfo.InvariantCulture);
            RequestingUser = rgParams[1];
            TargetUser = rgParams[2];
            ClubToJoin = null;
            if (rgParams.Length > 2 && (Requestedrole == RoleType.RoleInviteJoinClub || Requestedrole == RoleType.RoleRequestJoinClub)
                && Int32.TryParse(rgParams[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                ClubToJoin = Club.ClubWithID(id);
        }

        /// <summary>
        /// Initializes the request from an encrypted string
        /// </summary>
        /// <param name="szRequest">The encrypted request string</param>
        public CFIStudentMapRequest DecryptRequest(string szRequest)
        {
            InitFromString(new PeerRequestEncryptor().Decrypt(szRequest));
            return this;
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

                string szCallBackUrl = String.Format(CultureInfo.InvariantCulture,"{0}?req={1}", "~/mvc/Training/AddRelationship".ToAbsoluteURL(util.RequestContext.CurrentRequestUrl), HttpUtility.UrlEncode(EncryptRequest()));

                string szRequestor = String.Format(CultureInfo.CurrentCulture, "{0} ({1})", pf.UserFullName, pf.Email);

                util.PopulateMessageContentWithTemplate(msg, szTemplate.Replace("<% Requestor %>", szRequestor).Replace("<% Role %>", szRole).Replace("<% ConfirmRoleLink %>", szCallBackUrl).Replace("<% ClubName %>", ClubToJoin == null ? string.Empty : ClubToJoin.Name));
                util.SendMessage(msg);
            }
        }

        /// <summary>
        /// Processes an incoming request parameter on a relationship add request for the specified user
        /// </summary>
        /// <param name="szReq">The encrypted request parameter</param>
        /// <param name="szUser">The target user</param>
        /// <returns>A description of the request, if valid</returns>
        /// <exception cref="MyFlightbookValidationException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public static string ProcessRequestParameter(string szReq, string szUser)
        {
            if (String.IsNullOrEmpty(szReq))
                throw new MyFlightbookValidationException(Resources.LocalizedText.AddRelationshipErrInvalidRequest);
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            CFIStudentMapRequest m_smr = new CFIStudentMapRequest();
            m_smr.DecryptRequest(szReq);
            string szResult = String.Empty;

            Profile pfRequestor = Profile.GetUser(m_smr.RequestingUser);
            if (!pfRequestor.IsValid())
                throw new MyFlightbookValidationException(Resources.LocalizedText.AddRelationshipErrInvalidUser);

            Profile pfCurrent = Profile.GetUser(szUser);
            if (pfCurrent.Email.CompareCurrentCultureIgnoreCase(m_smr.TargetUser) != 0 && !pfCurrent.IsVerifiedEmail(m_smr.TargetUser))
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AddRelationshipErrNotTargetUser, m_smr.TargetUser, pfCurrent.Email));

            CFIStudentMap m_sm = new CFIStudentMap(szUser);

            switch (m_smr.Requestedrole)
            {
                case RoleType.RoleStudent:
                    szResult = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AddRelationshipRequestStudent, Branding.CurrentBrand.AppName, pfRequestor.UserFullName);
                    if (m_sm.IsStudentOf(m_smr.RequestingUser))
                        throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AddRelationshipErrAlreadyStudent, pfRequestor.UserFullName));
                    break;
                case RoleType.RoleCFI:
                    szResult = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AddRelationshipRequestInstructor, Branding.CurrentBrand.AppName, pfRequestor.UserFullName);
                    if (m_sm.IsInstructorOf(m_smr.RequestingUser))
                        throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.AddRelationshipErrAlreadyInstructor, pfRequestor.UserFullName));
                    break;
                case RoleType.RoleInviteJoinClub:
                    if (m_smr.ClubToJoin == null)
                        throw new MyFlightbookValidationException(Resources.Club.errNoClubInRequest);
                    if (m_smr.ClubToJoin.HasMember(pfCurrent.UserName))
                        throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.Club.errAlreadyMember, m_smr.ClubToJoin.Name));
                    szResult = String.Format(CultureInfo.CurrentCulture, Resources.Club.AddMemberFromInvitation, m_smr.ClubToJoin.Name);
                    break;
                case RoleType.RoleRequestJoinClub:
                    if (m_smr.ClubToJoin == null)
                        throw new MyFlightbookValidationException(Resources.Club.errNoClubInRequest);
                    if (m_smr.ClubToJoin.HasMember(pfRequestor.UserName))
                        throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, Resources.Club.errAlreadyAddedMember, pfRequestor.UserFullName, m_smr.ClubToJoin.Name));
                    szResult = String.Format(CultureInfo.CurrentCulture, Resources.Club.AddMemberFromRequest, HttpUtility.HtmlEncode(pfRequestor.UserFullName), m_smr.ClubToJoin.Name);
                    break;
            }
            return szResult;
        }

        /// <summary>
        /// Carries out the request for the specified user
        /// </summary>
        /// <param name="szReq">The encrypted request</param>
        /// <param name="szUser">The target user</param>
        /// <returns>A destination for redirect</returns>
        public static string ExecuteRequestForUser(string szReq, string szUser)
        {
            if (String.IsNullOrEmpty(szReq))
                throw new MyFlightbookValidationException(Resources.LocalizedText.AddRelationshipErrInvalidRequest);
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            CFIStudentMap m_sm = new CFIStudentMap(szUser);
            CFIStudentMapRequest m_smr = new CFIStudentMapRequest();
            m_smr.DecryptRequest(szReq);
            m_sm.ExecuteRequest(m_smr);

            switch (m_smr.Requestedrole)
            {
                case RoleType.RoleCFI:
                case RoleType.RoleStudent:
                    return "~/mvc/training/" + (m_smr.Requestedrole == RoleType.RoleCFI ? "students" : "instructors");
                case RoleType.RoleInviteJoinClub:
                    {
                        // Let the requestor know that the invitation has been accepted.
                        Profile pfTarget = Profile.GetUser(m_smr.TargetUser.Contains("@") ? System.Web.Security.Membership.GetUserNameByEmail(m_smr.TargetUser) : m_smr.TargetUser);
                        string szSubject = String.Format(CultureInfo.CurrentCulture, Resources.Club.AddMemberInvitationAccepted, m_smr.ClubToJoin.Name);
                        string szBody = Branding.ReBrand(Resources.Club.ClubInvitationAccepted).Replace("<% ClubName %>", m_smr.ClubToJoin.Name).Replace("<% ClubInvitee %>", pfTarget.UserFullName);
                        foreach (ClubMember cm in ClubMember.AdminsForClub(m_smr.ClubToJoin.ID))
                            util.NotifyUser(szSubject, szBody.Replace("<% FullName %>", cm.UserFullName), new MailAddress(cm.Email, cm.UserFullName), false, false);
                        return String.Format(CultureInfo.InvariantCulture, "~/mvc/club/details/{0}", m_smr.ClubToJoin.ID);
                    }
                case RoleType.RoleRequestJoinClub:
                    {
                        // Let the requestor know that the request has been approved.
                        Profile pfRequestor = Profile.GetUser(m_smr.RequestingUser);
                        string szSubject = String.Format(CultureInfo.CurrentCulture, Resources.Club.AddMemberRequestAccepted, m_smr.ClubToJoin.Name);
                        string szBody = Branding.ReBrand(Resources.Club.ClubRequestAccepted).Replace("<% ClubName %>", m_smr.ClubToJoin.Name).Replace("<% FullName %>", pfRequestor.UserFullName);
                        util.NotifyUser(szSubject, szBody, new MailAddress(pfRequestor.Email, pfRequestor.UserFullName), false, false);
                        return String.Format(CultureInfo.InvariantCulture, "~/mvc/club/details/{0}", m_smr.ClubToJoin.ID);
                    }
                default:
                    throw new MyFlightbookValidationException("Unknown role requested: " + m_smr.Requestedrole.ToString());
            }
        }
    }

    public class CFIStudentMap
    {
        private const string szQStudentsForUser = "SELECT users.*, NULL AS Role, students.CanViewStudent, CONCAT(COALESCE(LastName, ''), COALESCE(FirstName, ''), username) AS sortName FROM users INNER JOIN students ON users.username=students.StudentName WHERE students.CFIName=?uname ORDER BY sortName ASC";
        private const string szQInstructorsForUser = "SELECT users.*, NULL AS Role, students.CanViewStudent, CONCAT(COALESCE(LastName, ''), COALESCE(FirstName, ''), username) AS sortName FROM users INNER JOIN students ON users.username=students.CFIName WHERE students.StudentName=?uname ORDER BY sortName ASC";

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
        private List<InstructorStudent> GetAssociatedUsers(Boolean fStudents)
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
        public static InstructorStudent GetInstructorStudent(IEnumerable<InstructorStudent> rgpf, string szName)
        {
            if (rgpf == null)
                throw new ArgumentNullException(nameof(rgpf));
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
                throw new ArgumentNullException(nameof(cmr));
            Profile pf = Profile.GetUser(User);
            if (cmr.TargetUser.CompareOrdinalIgnoreCase(pf.Email) != 0 && !pf.IsVerifiedEmail(cmr.TargetUser))
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

        private const string PrefKeyInstructorScribble = "DefaultCFIScribble";

        /// <summary>
        /// Gets the default scribble (if any) for the specified user
        /// </summary>
        /// <param name="pf"></param>
        /// <returns></returns>
        public static byte[] DefaultScribbleForInstructor(Profile pf)
        {
            if (pf == null || !pf.PreferenceExists(PrefKeyInstructorScribble))
                return Array.Empty<byte>();

            return Convert.FromBase64String(pf.GetPreferenceForKey<string>(PrefKeyInstructorScribble));
        }

        /// <summary>
        /// Gets the default scribble (if any) for the specified user
        /// </summary>
        /// <param name="pf"></param>
        /// <returns></returns>
        public static byte[] DefaultScribbleForInstructor(string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));

            return DefaultScribbleForInstructor(Profile.GetUser(szUser));
        }

        /// <summary>
        /// Checks if the given instructor has a default signature.
        /// </summary>
        /// <param name="szUser"></param>
        /// <returns></returns>
        public static bool InstructorHasDefaultScribble(string szUser)
        {
            if (szUser == null)
                throw new ArgumentNullException(nameof(szUser));

            Profile pf = Profile.GetUser(szUser);
            return pf != null && pf.PreferenceExists(PrefKeyInstructorScribble);
        }

        /// <summary>
        /// Sets the default signature for the specified user.  Clears it if null/empty
        /// </summary>
        /// <param name="pf"></param>
        /// <returns></returns>
        public static void SetDefaultScribbleForInstructor(Profile pf, byte[] rgb)
        {
            if (pf == null)
                throw new ArgumentNullException(nameof(pf));

            if (rgb == null || rgb.Length == 0)
                pf.SetPreferenceForKey(PrefKeyInstructorScribble, null, true);
            else
                pf.SetPreferenceForKey(PrefKeyInstructorScribble, Convert.ToBase64String(rgb));
        }
    }
}