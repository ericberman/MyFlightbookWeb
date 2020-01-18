using MyFlightbook;
using MyFlightbook.Airports;
using MyFlightbook.Mapping;
using System;
using System.Linq;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2008-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

[ParseChildren(ChildrenAsProperties=true)]
public partial class Controls_mfbAirportIDGame : System.Web.UI.UserControl, INamingContainer
{
    AirportQuiz m_AirportQuiz = null;
    private const string keyQuiz = "QuizState";
    private int m_cQuestions = 10;
    private int m_BluffCount = 3;

    private ITemplate m_Intro = null;
    private ITemplate m_Outtro = null;
    private Control m_IntroContainer = null;
    private Control m_OuttroContainer = null;

    [TemplateContainer(typeof(QuizIntroTemplate)), PersistenceMode(PersistenceMode.InnerDefaultProperty), TemplateInstance(TemplateInstance.Single)]
    public ITemplate QuizIntro
    {
        get { return m_Intro; }
        set { m_Intro = value; }
    }

    [TemplateContainer(typeof(QuizSummaryTemplate)), PersistenceMode(PersistenceMode.InnerDefaultProperty), TemplateInstance(TemplateInstance.Single)]
    public ITemplate QuizSummary
    {
        get { return m_Outtro; }
        set { m_Outtro = value; }
    }

    protected override void CreateChildControls()
    {
        if (QuizIntro != null)
        {
            m_IntroContainer = new QuizIntroTemplate();
            QuizIntro.InstantiateIn(m_IntroContainer);
            vwBegin.Controls.Clear();
            vwBegin.Controls.Add(m_IntroContainer);
        }
        if (QuizSummary != null)
        {
            m_OuttroContainer = new QuizSummaryTemplate(this);
            QuizSummary.InstantiateIn(m_OuttroContainer);
            vwResult.Controls.Clear();
            vwResult.Controls.Add(m_OuttroContainer);
        }

        base.CreateChildControls();
    }

    protected override void OnDataBinding(EventArgs e)
    {
        EnsureChildControls();
        base.OnDataBinding(e);
    }

    /// <summary>
    /// Which question # are we on?
    /// </summary>
    public int CurrentQuestionIndex
    {
        get { return m_AirportQuiz.CurrentQuestionIndex; }
    }

    public string ResultsSnark
    {
        get { return lblSnark.Text; }
    }

    /// <summary>
    /// How many questions have been correctly answered?
    /// </summary>
    public int CorrectAnswerCount
    {
        get { return m_AirportQuiz.CorrectAnswerCount; }
    }

    /// <summary>
    /// Gets/Sets the # of questions in the quiz
    /// </summary>
    public int QuestionCount
    {
        get { return m_cQuestions; }
        set 
        {
            if (value > 0)
                m_cQuestions = value;
            else
                throw new MyFlightbookException("There must be at least 1 question in the quiz");
        }
    }

    /// <summary>
    /// The # of bluffs to show besides the correct answer; defaults to 3
    /// </summary>
    public int BluffCount
    {
        get { return m_BluffCount; }
        set
        {
            if (BluffCount > 1)
            {
                m_BluffCount = value;
                if (m_AirportQuiz != null)
                    m_AirportQuiz.BluffCount = value;
            }
            else
                throw new MyFlightbookException("Bluffcount must be greater than 1");
        }
    }

    /// <summary>
    /// Initializes a new quiz object; necessary in case the control has not yet loaded.  This will discared any existing quiz.
    /// </summary>
    public void InitQuiz()
    {
        if (m_AirportQuiz == null)
        {
            m_AirportQuiz = new AirportQuiz();
            m_AirportQuiz.BluffCount = m_BluffCount;

            ViewState[keyQuiz] = m_AirportQuiz;
        }
    }

    /// <summary>
    /// Event raised when the user finishes the quiz.
    /// </summary>
    public event System.EventHandler QuizFinished;

    protected void Page_Load(object sender, EventArgs e)
    {
        // set up a timeout function, in case the timer goes.
        Page.ClientScript.RegisterClientScriptBlock(GetType(), "TimeOut", "function TimeOutExpired() {" + Page.ClientScript.GetPostBackEventReference(new PostBackOptions(btnSkip)) + ";}", true);

        if (!IsPostBack)
            InitQuiz();
        else
        {
            m_AirportQuiz = (AirportQuiz) ViewState[keyQuiz];

            // We've been getting this a few times; Session could have timed out, so reset things.
            if (m_AirportQuiz == null)
                InitQuiz();

            if (mvQuiz.ActiveViewIndex == 1)  // taking the quiz
            {
                Boolean fCorrectAnswer = (rbGuesses.SelectedIndex == m_AirportQuiz.CurrentQuestion.CorrectAnswerIndex);

                if (m_AirportQuiz.CurrentQuestion.CorrectAnswerIndex >= 0)
                {
                    pRunningScore.Visible = true;
                    lblCorrect.Visible = fCorrectAnswer;
                    lblIncorrect.Visible = !fCorrectAnswer;

                    lblPreviousAnswer.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.AirportGameCorrectAnswer, m_AirportQuiz.CurrentQuestion.Answers[m_AirportQuiz.CurrentQuestion.CorrectAnswerIndex].FullName);
                }

                if (fCorrectAnswer)
                    m_AirportQuiz.CorrectAnswerCount = m_AirportQuiz.CorrectAnswerCount + 1;

                if (m_AirportQuiz.CorrectAnswerCount > 0)
                    lblRunningScore.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.AirportGameAnswerStatus, m_AirportQuiz.CorrectAnswerCount);
            }
        }
    }

    protected void ShowMap(string szTLA)
    {
        // Create an airportlist object and initialize it with this airport string
        airport ap = new AirportList(szTLA).GetAirportList()[0];
        MfbGoogleMap1.Map.MapCenter = ap.LatLong;
        MfbGoogleMap1.Map.ZoomFactor = GMap_ZoomLevels.Airport;
        MfbGoogleMap1.Map.MapType = GMap_MapType.G_SATELLITE_MAP;
        MfbGoogleMap1.Map.StaticMapAdditionalParams = "style=feature:all|element:labels|visibility:off";
    }


    protected airport[] GetUserAirports(string szDefault)
    {
        if (!Page.User.Identity.IsAuthenticated)
        {
            lblDebug.Text = Resources.LocalizedText.AirportGameUnauthenticated;
            return (new AirportList(szDefault)).GetAirportList();
        }

        VisitedAirport[] rgva = VisitedAirport.VisitedAirportsForUser(Page.User.Identity.Name);

        if (rgva.Length < 15)
        {
            lblDebug.Text = Resources.LocalizedText.AirportGameTooFewAirports;
            return airport.AirportsWithExactMatch(szDefault).ToArray();
        }

        airport[] rgap = new airport[rgva.Length];

        int i = 0;
        foreach (VisitedAirport va in rgva)
            rgap[i++] = va.Airport;

        return rgap;
    }

    protected void lnkYourAirports_Click(object sender, EventArgs e)
    {
        // Bounce off of an SSL authenticated page, which will force auth and bounce back here.
        string szURL = Request.RawUrl;
        Response.Redirect("~/Member/Game.aspx?Url=" + HttpUtility.UrlEncode(szURL));
    }

    protected void lnkBusyUS_Click(object sender, EventArgs e)
    {
        StartQuiz(true);
    }

    /// <summary>
    /// Start the quiz.
    /// </summary>
    /// <param name="szDefaultAirportList">The list of airports to use</param>
    protected void BeginQuiz(airport[] rgAirports)
    {
        mvQuiz.SetActiveView(vwQuestions);

        // Strip any navaids from the list; not an issue for user airports, but an issue for US airports
        m_AirportQuiz.Init(AirportList.RemoveNavaids(rgAirports));
        NextQuestion();
    }

    /// <summary>
    /// Public method to begin a quiz, using authenticated credentials if possible.
    /// </summary>
    /// <param name="fAnonymous">True if the user is anonymous or has selected US airports only</param>
    public void StartQuiz(Boolean fAnonymous)
    {
        BeginQuiz(fAnonymous ? (airport.AirportsWithExactMatch(AirportQuiz.szBusyUSAirports).ToArray()) :  GetUserAirports(AirportQuiz.szBusyUSAirports));
    }

    protected void NextQuestion()
    {
        m_AirportQuiz.GenerateQuestion();

        rbGuesses.Items.Clear();
        for (int i = 0; i < m_AirportQuiz.CurrentQuestion.Answers.Count; i++)
            rbGuesses.Items.Add(m_AirportQuiz.CurrentQuestion.Answers[i].FullName);

        ShowMap(m_AirportQuiz.CurrentQuestion.Answers[m_AirportQuiz.CurrentQuestion.CorrectAnswerIndex].Code);

        lblQuestionProgress.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.AirportGameProgress, m_AirportQuiz.CurrentQuestionIndex, QuestionCount);
    }

    protected void btnSkip_Click(object sender, EventArgs e)
    {
        if (m_AirportQuiz.CurrentQuestionIndex < QuestionCount)
            NextQuestion();
        else
            FinishQuiz();
    }

    protected void FinishQuiz()
    {
        int cCorrectAnswers = m_AirportQuiz.CorrectAnswerCount;
        int score = Convert.ToInt32((cCorrectAnswers * 100.0) / QuestionCount);
        lblResults.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.AirportGameCompletionStatus, cCorrectAnswers, QuestionCount, score);

        if (this.QuizFinished != null)
            this.QuizFinished(this, new EventArgs());

        if (score == 100)
            lblSnark.Text = Resources.LocalizedText.AirportGameSnarkPerfect;
        else if (score >= 70)
            lblSnark.Text = Resources.LocalizedText.AirportGameSnark75;
        else if (score >= 50)
            lblSnark.Text = Resources.LocalizedText.AirportGameSnark50;
        else
            lblSnark.Text = Resources.LocalizedText.AirportGameSnarkPoor;

        DataBind(); // needed to make results available to a quiz summary template.

        mvQuiz.SetActiveView(vwResult);
    }

    protected void lnkPlayAgain_Click(object sender, EventArgs e)
    {
        pRunningScore.Visible = false;
        BeginQuiz(m_AirportQuiz.DefaultAirportList);    // reuse the same list of airports
    }

    protected void rbGuesses_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (m_AirportQuiz.CurrentQuestionIndex < QuestionCount)
            NextQuestion();
        else
        {
            FinishQuiz();
        }
    }
}

    public class QuizIntroTemplate : Control, INamingContainer
    {
        public QuizIntroTemplate()
        {
        }
    }

    public class QuizSummaryTemplate : Control, INamingContainer
    {
        private Controls_mfbAirportIDGame parent;

        public int QuestionCount
        {
            get { return parent.QuestionCount; }
        }

        public int CorrectAnswerCount
        {
            get { return parent.CorrectAnswerCount; }
        }

        public string Snark
        {
            get { return parent.ResultsSnark; }
        }

        public QuizSummaryTemplate(Controls_mfbAirportIDGame parent)
        {
            this.parent = parent;
        }
    }

