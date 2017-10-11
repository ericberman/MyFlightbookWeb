using System;
using System.Linq;
using System.Collections;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;
using MyFlightbook.Image;
using MyFlightbook.Payments;
using MyFlightbook.Telemetry;
using MyFlightbook.SocialMedia;

/******************************************************
 * 
 * Copyright (c) 2007-2016 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbEditFlight : System.Web.UI.UserControl
{

    private ArrayList m_rgTailwheelAircraft = new ArrayList();

    const string keyLastEntryDate = "LastEntryDate";
    const string keyTailwheelList = "TailwheelListForUser";
    const string keySessionInProgress = "InProgressFlight";
    const string keyCookieExpandFSLandings = "FSLandingsExpanded";
    const string keyCookieLastEndingHobbs = "LastHobbs";
    const string keyVSFlightUser = "VSFlightUser";
    const string szValGroupEdit = "vgEditFlight";

    #region Properties
    private int FlightID
    {
        get { return Convert.ToInt32(hdnItem.Value, CultureInfo.InvariantCulture); }
        set { hdnItem.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    /// <summary>
    /// Username for the flight.
    /// </summary>
    private string FlightUser
    {
        get { return (string) ViewState[keyVSFlightUser]; }
        set { ViewState[keyVSFlightUser] = value; }
    }

    protected int AltCatClass
    {
        get { return Convert.ToInt32(cmbCatClasses.SelectedValue, CultureInfo.InvariantCulture); }
        set { cmbCatClasses.SelectedValue = value.ToString(CultureInfo.InvariantCulture); }
    }

    public bool CanCancel
    {
        get { return btnCancel.Visible; }
        set { btnCancel.Visible = value; }
    }

    #region internal settings - NOT PERSISTED
    protected bool UseLastTail { get; set; }
    protected bool IsAdmin { get; set; }

    private Profile m_CurrentUser = null;
    protected Profile CurrentUser
    {
        get
        {
            return m_CurrentUser ?? (m_CurrentUser = MyFlightbook.Profile.GetUser(Page.User.Identity.Name));
        }
        set { m_CurrentUser = value; }
    }

    #endregion
    #endregion

    public event System.EventHandler FlightUpdated;
    public event System.EventHandler FlightEditCanceled;

    /// <summary>
    /// Initialize the edit form for a new flight (blank fields) or for editing of an existing flight
    /// </summary>
    /// <param name="idFlight">-1 for a new flight, otherwise the ID of the flight to load</param>
    /// <param name="fForceLoad">True to force load (e.g., an admin mode, or CFI editing a user's flight)</param>
    public void SetUpNewOrEdit(int idFlight, bool fForceLoad = false)
    {
        LogbookEntry le = new LogbookEntry() { User = Page.User.Identity.Name };

        InitBasicControls();

        // Initialize our logbook entry from the db or make it a new entry
        bool fAdminMode = (CurrentUser.CanSupport && (util.GetStringParam(Request, "a").Length > 0));
        IsAdmin = fForceLoad || fAdminMode;

        FlightID = idFlight;

        if (!le.FLoadFromDB(FlightID, Page.User.Identity.Name, LogbookEntry.LoadTelemetryOption.LoadAll, IsAdmin))
        {
            // if this isn't found, try again with a new flight (but tell the user of the error)
            lblError.Text = le.ErrorString;
            FlightID = (le = new LogbookEntry() { User = Page.User.Identity.Name }).FlightID;
        }

        // check for CFI signing mode
        if (fForceLoad && !le.IsNewFlight)
        {
            if (le.User.CompareOrdinal(Page.User.Identity.Name) != 0 && le.CanEditThisFlight(Page.User.Identity.Name))
            {
                pnlTwitter.Visible = pnlPictures.Visible = false;
                FlightUser = le.User;   // save the name of the owner of the flight.
            }
            else
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture,"attempt by {0} to edit non-owned flight (owned by {1}) by non-instructor!", Page.User.Identity.Name, le.User));
        }

        // Enable Admin Signature fix-up
        if (fAdminMode && !le.IsNewFlight && le.CFISignatureState != LogbookEntry.SignatureState.None)
        {
            LogbookEntry.SignatureSanityCheckState sscs = le.AdminSignatureSanityCheckState;
            if (sscs != LogbookEntry.SignatureSanityCheckState.OK)
            {
                pnlAdminFixSignature.Visible = true;
                lblSigSavedState.Text = le.CFISignatureState.ToString();
                lblSigSanityCheck.Text = sscs.ToString();
                lblSigSavedHash.Text = le.DecryptedFlightHash;
                lblSigCurrentHash.Text = le.DecryptedCurrentHash;
            }
        }

        // If the user has entered another flight this session, default to that date rather than today
        if (Session[keyLastEntryDate] != null && FlightID == LogbookEntry.idFlightNew)
            le.Date = (DateTime)Session[keyLastEntryDate];

        // see if we have a pending in-progress flight
        if (FlightID == LogbookEntry.idFlightNew && Session[keySessionInProgress] != null)
            le = (LogbookEntry)Session[keySessionInProgress];
        Session[keySessionInProgress] = null; // clear it out regardless.

        UseLastTail = true;

        // If a repeat or a reverse is requested, then clone and/or reverse it.
        le = CloneOrReverse(le);
        
        // If this is a shared flight, initialize from that.
        le = SharedFlight(le);

        // If we're setting up a new flight and last flight had an ending hobbs, initialize with that
        // clear the cookie, if present, regardless.
        HttpCookie c = Request.Cookies[keyCookieLastEndingHobbs];
        if (c != null)
        {
            if (le.IsNewFlight)
            {
                decimal hobbsEnd;
                if (decimal.TryParse(c.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out hobbsEnd))
                    le.HobbsStart = hobbsEnd;
            }
            Response.Cookies[keyCookieLastEndingHobbs].Expires = DateTime.Now.AddDays(-1);   // clear it.
        }

        SetUpAircraftForFlight(le);

        InitFormFromLogbookEntry(le);

        bool fCanDoVideo = EarnedGrauity.UserQualifies(Page.User.Identity.Name, Gratuity.GratuityTypes.Videos);
        mfbMFUFlightImages.IncludeVideos = fCanDoVideo;
        mfbVideoEntry1.CanAddVideos = fCanDoVideo;
        mfbVideoEntry1.FlightID = le.FlightID;
        lblPixForFlight.Text = fCanDoVideo ? Resources.LogbookEntry.HeaderImagesVideosForFlight : Resources.LogbookEntry.HeaderImagesForFlight;

        FinalizeSetupForFlight(le);

        mfbDate.Focus();
    }

    private void AutoExpandLandings(LogbookEntry le)
    {
        cpeLandingDetails.Collapsed = !(le.NightLandings + le.FullStopLandings > 0 ||
                                       le.Nighttime > 0.0M ||
                                       m_rgTailwheelAircraft.Contains(le.AircraftID) ||
                                       (Request.Cookies[keyCookieExpandFSLandings] != null && Request.Cookies[keyCookieExpandFSLandings].Value.CompareOrdinalIgnoreCase("y") == 0)
                                       );
        cpeLandingDetails.ClientState = cpeLandingDetails.Collapsed ? "true" : "false";
    }

    // Save on some view state by loading this up on each page load.
    protected void Page_Init(object sender, EventArgs e)
    {
        cmbCatClasses.DataSource = CategoryClass.CategoryClasses();
        cmbCatClasses.DataBind();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        mfbDate.DefaultDate = DateTime.Now; // if the field is blank, assume today.

        if (!IsPostBack)
        {
            InitBasicControls();
        }
        else
            ProcessImages(FlightID);
    }

    /// <summary>
    /// Checks if we are repeating a flight or if we are repeating & reversing it.
    /// </summary>
    /// <param name="le">The logbook entry to start</param>
    /// <returns>The updated logbook entry</returns>
    private LogbookEntry CloneOrReverse(LogbookEntry le)
    {
        // if cloning, reset the ID and date
        if (util.GetIntParam(Request, "Clone", -1) != -1)
        {
            le = le.Clone();
            FlightID = le.FlightID; // should be idFlightNew
            UseLastTail = false;    // we need to use the tail from this flight's aircraft
            le.Date = DateTime.Now;
            le.HobbsEnd = le.HobbsStart = 0;
            le.EngineEnd = le.EngineStart = le.FlightStart = le.FlightEnd = DateTime.MinValue;
            le.FlightData = null;

            if (util.GetIntParam(Request, "Reverse", -1) != -1)
            {
                string[] rgRoute = MyFlightbook.Airports.AirportList.NormalizeAirportList(le.Route);
                Array.Reverse(rgRoute);
                le.Route = String.Join(Resources.LocalizedText.LocalizedSpace, rgRoute);
            }
        }
        return le;
    }

    /// <summary>
    /// Checks to see if this is being initialized from a shared flight, returns the LogbookEntry if so
    /// </summary>
    /// <param name="le">The logbook entry to start</param>
    /// <returns>The updated logbook entry</returns>
    private LogbookEntry SharedFlight(LogbookEntry le)
    {
        string szSharedFlightKey = util.GetStringParam(Request, "src");
        if (!String.IsNullOrEmpty(szSharedFlightKey))
        {
            le = new LogbookEntry(szSharedFlightKey) { User = Page.User.Identity.Name };
            UseLastTail = le.AircraftID == Aircraft.idAircraftUnknown;   // use the aircraft from this, if it's not an unknown aircraft (e.g., if the flight actually exists in the db)

            if (le.AircraftID != Aircraft.idAircraftUnknown)
            {
                // Add this aircraft to the user's profile if needed
                UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
                Aircraft ac = new Aircraft(le.AircraftID);
                if (!ua.CheckAircraftForUser(ac))
                    ua.FAddAircraftForUser(ac);
            }
        }

        return le;
    }

    /// <summary>
    /// Sets up the list of aircraft for the specified flight.
    /// If the flight is a new flight, we set up for the current user and optionally use the last known tail.
    /// For editing existing flights, we set up aircraft for the owner's aircraft list.
    /// </summary>
    /// <param name="le">The flight for which we want to set up</param>
    private void SetUpAircraftForFlight(LogbookEntry le)
    {
        if (le.IsNewFlight)
            SetUpAircraftForUser(CurrentUser.UserName, UseLastTail ? Aircraft.idAircraftUnknown : le.AircraftID);
        else
            SetUpAircraftForUser(le.User, le.AircraftID);
    }

    /// <summary>
    /// Populates the aircraft drop-down for the specified username and aircraft.
    /// The list comes from the user's aircraft list, and any hidden aircraft remain hidden 
    /// UNLESS that is the aircraft for this particular flight.
    /// Side effect: if the aircraft is tailwheel, we add it to the list of tailwheel aircraft.
    /// </summary>
    /// <param name="username"></param>
    /// <param name="idFlightAircraft"></param>
    protected void SetUpAircraftForUser(string username, int idFlightAircraft)
    {
        UserAircraft ua = new UserAircraft(username);
        Aircraft[] rgac = ua.GetAircraftForUser();
        cmbAircraft.Items.Clear();
        if (rgac != null)
        {
            foreach (Aircraft ac in rgac)
            {
                if (ac.HideFromSelection && ac.AircraftID != idFlightAircraft)
                    continue;

                cmbAircraft.Items.Add(new ListItem(ac.DisplayTailnumberWithModel, ac.AircraftID.ToString(CultureInfo.InvariantCulture)));
                if (ac.IsTailwheel)
                    m_rgTailwheelAircraft.Add(ac.AircraftID);
            }
        }
        util.SetValidationGroup(this, szValGroupEdit);
        Session[keyTailwheelList] = m_rgTailwheelAircraft;
    }

    /// <summary>
    /// Sets up things like decimal vs. hhMM mode, alternat category classes list, etc.
    /// </summary>
    protected void InitBasicControls()
    {
        if (Session[keyTailwheelList] != null && cmbAircraft.Items.Count > 0) // we've already initialized...
        {
            m_rgTailwheelAircraft = (ArrayList)Session[keyTailwheelList];
            return;
        }

        if (Request.IsMobileDevice())
            cmbAircraft.Width = txtRoute.Width = txtComments.Width = Unit.Pixel(130);

        ckUpdateTwitter.Checked = mfbTwitter.FDefaultTwitterCheckboxState && (FlightID > 0);  // don't default the facebook/twitter checkboxes on if this is editing an existing flight.
        ckFacebook.Checked = mfbFacebook1.FDefaultFacebookCheckboxState && (FlightID > 0);

        // Use the desired editing mode.
        MyFlightbook.Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
        Controls_mfbDecimalEdit.EditMode em = pf.UsesHHMM ? Controls_mfbDecimalEdit.EditMode.HHMMFormat : Controls_mfbDecimalEdit.EditMode.Decimal;
        decCFI.EditingMode = decDual.EditingMode = decGrndSim.EditingMode = decIMC.EditingMode =
            decNight.EditingMode = decPIC.EditingMode = decSIC.EditingMode = decSimulatedIFR.EditingMode =
            decTotal.EditingMode = decXC.EditingMode = em;

        // And enable/disable facebook/twitter:
        ckFacebook.Visible = pf.CanPostFacebook();
        ckUpdateTwitter.Visible = pf.CanTweet();
        mvFacebook.SetActiveView(pf.CanPostFacebook() ? vwFacebookActive : vwFacebookInactive);
        mvTwitter.SetActiveView(pf.CanTweet() ? vwTwitterActive : vwTwitterInactive);

        mfbEditPropSet1.CrossFillSourceClientID = decCFI.CrossFillSourceClientID = decDual.CrossFillSourceClientID = decGrndSim.CrossFillSourceClientID = decIMC.CrossFillSourceClientID = 
            decNight.CrossFillSourceClientID = decPIC.CrossFillSourceClientID = decSIC.CrossFillSourceClientID = decSimulatedIFR.CrossFillSourceClientID = decXC.CrossFillSourceClientID = decTotal.EditBox.ClientID;
    }

    private void FinalizeSetupForFlight(LogbookEntry le)
    {
        // if the specified id was found AND it belongs to the current user, we can edit it; else, just set up for "new"
        if (le.IsNewFlight)
        {
            btnAddFlight.Text = Resources.LocalizedText.EditFlightAddFlight;
            mfbFlightImages.Visible = false;
            mfbVideoEntry1.Videos.Clear();

            // set the aircraft to the ID of the most recently used flight, if known.
            int idLastAircraft = Aircraft.LastTail;
            if (idLastAircraft != 0 && UseLastTail)
            {
                try { cmbAircraft.SelectedValue = idLastAircraft.ToString(CultureInfo.InvariantCulture); }
                catch (ArgumentOutOfRangeException) { }
            }

            try
            {
                le.AircraftID = Convert.ToInt32(cmbAircraft.SelectedValue, CultureInfo.InvariantCulture); // initialize the aircraft so that the landings auto-expands based on tailwheel 
            }
            catch (FormatException)
            {
                le.AircraftID = Aircraft.idAircraftUnknown;
            }
            cpeFlightDetails.Collapsed = !CurrentUser.DisplayTimesByDefault;
        }
        else
        {
            btnAddFlight.Text = Resources.LocalizedText.EditFlightUpdateFlight;
            mfbFlightImages.Visible = true;
            mfbFlightImages.Key = le.FlightID.ToString(CultureInfo.InvariantCulture);
            mfbFlightImages.Refresh();

            // Set up videos too.
            mfbVideoEntry1.Videos.Clear();
            foreach (VideoRef vr in le.Videos)
                mfbVideoEntry1.Videos.Add(vr);

            cpeAltCatClass.Collapsed = (le.CatClassOverride == 0);
        }

        cpeFlightDetails.Collapsed = !CurrentUser.DisplayTimesByDefault && !(le.HobbsEnd > 0.0M ||
            le.HobbsStart > 0.0M ||
            le.EngineStart.HasValue() ||
            le.EngineEnd.HasValue() ||
            le.FlightStart.HasValue() ||
            le.FlightEnd.HasValue() ||
            le.HasFlightData ||
            le.CustomProperties.GetLength(0) > 0);

        AutoExpandLandings(le);
    }

    /// <summary>
    /// Fills in the form to edit a flight based on a LogbookEntry object
    /// </summary>
    /// <param name="le">The logbook entry object</param>
    private void InitFormFromLogbookEntry(LogbookEntry le)
    {
        mfbDate.Date = le.Date;

        if (le.AircraftID != Aircraft.idAircraftUnknown)
        {
            // try to select the aircraft based on the flight aircraft
            try { cmbAircraft.SelectedValue = le.AircraftID.ToString(CultureInfo.InvariantCulture); }
            catch (ArgumentOutOfRangeException) { cmbAircraft.SelectedIndex = 0; }
        }
        else
            cmbAircraft.SelectedIndex = 0;

        intApproaches.IntValue = le.Approaches;
        intLandings.IntValue = le.Landings;
        intFullStopLandings.IntValue = le.FullStopLandings;
        intNightLandings.IntValue = le.NightLandings;
        decNight.Value = le.Nighttime;
        decPIC.Value = le.PIC;
        decSimulatedIFR.Value = le.SimulatedIFR;
        decGrndSim.Value = le.GroundSim;
        decDual.Value = le.Dual;
        decXC.Value = le.CrossCountry;
        decIMC.Value = le.IMC;
        decCFI.Value = le.CFI;
        decSIC.Value = le.SIC;
        decTotal.Value = le.TotalFlightTime;
        ckHold.Checked = le.fHoldingProcedures;
        txtRoute.Text = le.Route;
        txtComments.Text = le.Comment;
        ckPublic.Checked = le.fIsPublic;

        mfbFlightInfo1.FlightID = le.FlightID;
        mfbFlightInfo1.HobbsStart = le.HobbsStart;
        mfbFlightInfo1.HobbsEnd = le.HobbsEnd;
        mfbFlightInfo1.EngineStart = le.EngineStart;
        mfbFlightInfo1.EngineEnd = le.EngineEnd;
        mfbFlightInfo1.FlightStart = le.FlightStart;
        mfbFlightInfo1.FlightEnd = le.FlightEnd;
        mfbFlightInfo1.HasFlightData = le.HasFlightData;
        if (le.HasFlightData)
            mfbFlightInfo1.Telemetry = le.FlightData;
        AltCatClass = le.CatClassOverride;

        if (util.GetIntParam(Request, "oldProps", 0) != 0)
        {
            mvPropEdit.SetActiveView(vwLegacyProps);
            mfbFlightProperties1.Enabled = true;
            mfbFlightProperties1.SetFlightProperties(le.CustomProperties);
        }
        else
            mfbEditPropSet1.SetFlightProperties(le.CustomProperties);

        MyFlightbook.Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
        divCFI.Visible = pf.IsInstructor;
        divSIC.Visible = pf.TracksSecondInCommandTime;
    }

    /// <summary>
    /// Initializes a LogbookEntry object based on what is currently in the form.
    /// </summary>
    /// <returns></returns>
    protected LogbookEntry InitLogbookEntryFromForm()
    {
        LogbookEntry le = new LogbookEntry();
        le.FlightID = FlightID;
        le.User = String.IsNullOrEmpty(FlightUser) ? Page.User.Identity.Name : FlightUser;

        le.Date = mfbDate.Date;
        le.AircraftID = cmbAircraft.SelectedValue.SafeParseInt(Aircraft.idAircraftUnknown);
        le.Approaches = intApproaches.IntValue;
        le.Landings = intLandings.IntValue;
        le.NightLandings = intNightLandings.IntValue;
        le.FullStopLandings = intFullStopLandings.IntValue;
        le.CrossCountry = decXC.Value;
        le.Nighttime = decNight.Value;
        le.IMC = decIMC.Value;
        le.SimulatedIFR = decSimulatedIFR.Value;
        le.GroundSim = decGrndSim.Value;;
        le.Dual = decDual.Value;
        le.PIC = decPIC.Value;
        le.CFI = decCFI.Value;
        le.SIC = decSIC.Value;
        le.TotalFlightTime = decTotal.Value;
        le.fHoldingProcedures = ckHold.Checked;
        le.fIsPublic = ckPublic.Checked;
        le.Route = txtRoute.Text;
        le.Comment = txtComments.Text;

        mfbFlightInfo1.DefaultDate = le.Date;
        le.HobbsStart = mfbFlightInfo1.HobbsStart;
        le.HobbsEnd = mfbFlightInfo1.HobbsEnd;
        le.EngineStart = mfbFlightInfo1.EngineStart;
        le.EngineEnd = mfbFlightInfo1.EngineEnd;
        le.FlightStart = mfbFlightInfo1.FlightStart;
        le.FlightEnd = mfbFlightInfo1.FlightEnd;
        le.CatClassOverride = AltCatClass;

        le.CustomProperties = mfbEditPropSet1.DistilledList.ToArray();

        le.Videos = mfbVideoEntry1.Videos;

        return le;
    }

    protected void ProcessImages(int idFlight)
    {
        if (!LogbookEntry.IsNewFlightID(idFlight))
        {
            mfbFlightImages.Key = mfbMFUFlightImages.ImageKey = idFlight.ToString(CultureInfo.InvariantCulture);
            mfbMFUFlightImages.ProcessUploadedImages();
            mfbFlightImages.Refresh();
        }
    }

    /// <summary>
    /// Commits the edits, returns the id of the resulting row, -1 if it failed.
    /// </summary>
    /// <returns>Non-negative flight ID, -1 for failure</returns>
    protected int CommitChanges()
    {
        int idResult = -1;

        if (!Page.IsValid)
            return idResult;

        LogbookEntry le = InitLogbookEntryFromForm();

        if (le.IsValid())
        {
            // if a new flight and hobbs > 0, save it for the next flight
            if (le.IsNewFlight && le.HobbsEnd > 0)
                Response.Cookies.Add(new HttpCookie(keyCookieLastEndingHobbs, le.HobbsEnd.ToString(CultureInfo.InvariantCulture)));

            le.FlightData = mfbFlightInfo1.Telemetry;

            try
            {
                if (le.FCommit(le.HasFlightData))
                {
                    Aircraft.SaveLastTail(le.AircraftID);

                    ProcessImages(le.FlightID);

                    if (FlightID == LogbookEntry.idFlightNew) // new flight - save the date
                        Session[keyLastEntryDate] = le.Date;

                    idResult = le.FlightID; // this must be >= 0 if the commit succeeded

                    if (ckFacebook.Checked)
                        mfbFacebook1.PostFlight(le);
                    else if (FlightID == LogbookEntry.idFlightNew)
                        mfbFacebook1.FDefaultFacebookCheckboxState = false; // if this was a new flight and the user didn't post on facebook, then don't default next time.

                    if (ckUpdateTwitter.Checked)
                        mfbTwitter.PostFlight(le);
                    else if (FlightID == LogbookEntry.idFlightNew)
                        mfbTwitter.FDefaultTwitterCheckboxState = false; // if this was a new flight and user didn't tweet, then don't default next time; if editing a flight, don't change the default state

                    // Badge computation may be wrong
                    MyFlightbook.Profile.GetUser(le.User).SetAchievementStatus(MyFlightbook.Achievements.Achievement.ComputeStatus.NeedsComputing);
                }
                else
                {
                    lblError.Text = le.ErrorString;
                }
            }
            catch (MyFlightbookException ex)
            {
                lblError.Text = !String.IsNullOrEmpty(le.ErrorString) ? le.ErrorString : (ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }
        else
            lblError.Text = le.ErrorString;

        return idResult;
    }

    protected void btnAddFlight_Click(object sender, EventArgs e)
    {
        // Save the state of the full-stop landings expansion
        Request.Cookies.Add(new HttpCookie(keyCookieExpandFSLandings, Convert.ToBoolean(cpeLandingDetails.ClientState, CultureInfo.InvariantCulture) ? string.Empty : "y"));

        Page.Validate(szValGroupEdit);
        if (CommitChanges() >= 0)
        {
            if (FlightUpdated != null)
                FlightUpdated(sender, e);
        }
    }
    protected void lnkAddAircraft_Click(object sender, EventArgs e)
    {
        LogbookEntry le = InitLogbookEntryFromForm();
        Session[keySessionInProgress] = le;
        Response.Redirect("~/Member/EditAircraft.aspx?id=-1&Ret=" + HttpUtility.UrlEncode(Request.Url.PathAndQuery));
    }

    protected void CheckFullStopCount(object sender, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException("args");
        if (intLandings.IntValue > 0 && intFullStopLandings.IntValue + intNightLandings.IntValue > intLandings.IntValue)
            args.IsValid = false;
    }

    protected void valDate_ServerValidate(object source, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException("args");
        if (DateTime.Compare(mfbDate.Date, DateTime.Now.AddDays(3)) > 0)
            args.IsValid = false;
    }

    protected void lnkSetUpTwitter_Click(object sender, EventArgs e)
    {
        SocialNetworkAuthorization.PushRedirect(Request.Url.PathAndQuery);
        Response.Redirect(mfbTwitter.AuthURL.ToString());
    }

    protected void lnkSetUpFacebook_Click(object sender, EventArgs e)
    {
        SocialNetworkAuthorization.PushRedirect(Request.Url.PathAndQuery);
        MFBFacebook.Authorize();
    }

    protected void AutoFill(object sender, AutofillEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        LogbookEntry le = InitLogbookEntryFromForm();
        le.FlightData = e.Telemetry;
        using (FlightData fd = new FlightData())
        {
            fd.AutoFill(le, e.Options);
        }
        InitFormFromLogbookEntry(le);
        AutoExpandLandings(le);
    }

    protected void btnCancel_Click(object sender, EventArgs e)
    {
        if (FlightEditCanceled != null)
            FlightEditCanceled(this, e);
    }

    #region Admin tools for signatures
    protected void btnAdminForceValid_Click(object sender, EventArgs e)
    {
        LogbookEntry le = new LogbookEntry();
        le.FLoadFromDB(FlightID, FlightUser, LogbookEntry.LoadTelemetryOption.None, true);
        le.AdminSignatureSanityFix(true);
        Response.Redirect(Request.Url.OriginalString);
    }

    protected void btnAdminFixSignature_Click(object sender, EventArgs e)
    {
        LogbookEntry le = new LogbookEntry();
        le.FLoadFromDB(FlightID, FlightUser, LogbookEntry.LoadTelemetryOption.None, true);
        le.AdminSignatureSanityFix(false);
        Response.Redirect(Request.Url.OriginalString);
    }
    #endregion
}

