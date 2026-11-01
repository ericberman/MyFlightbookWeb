using MyFlightbook.CloudStorage;
using MyFlightbook.Currency;
using MyFlightbook.Payments;
using MyFlightbook.SharedUtility.EventRecorder;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;

/******************************************************
 *
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Subscriptions
{
    #region Job types and rows
    /// <summary>
    /// The kinds of work done by the nightly run.  Stored by name in the nightlyjobs table, so don't rename these.
    /// </summary>
    public enum NightlyJobType
    {
        /// <summary>Weekly and/or expiring-currency email for one user.  Payload contains "C" (currency candidate) and/or "W" (weekly subscriber)</summary>
        Email,
        /// <summary>Monthly (or year-in-review) email for one user</summary>
        MonthlyEmail,
        /// <summary>Reminder that a user has lots of pending flights.  Payload is the count</summary>
        PendingFlightsReminder,
        /// <summary>Reminders of expiring gratuities (run-wide).  Payload is the user restriction, if any</summary>
        GratuityReminders,
        /// <summary>Monthly club reports (run-wide)</summary>
        ClubReports,
        /// <summary>Cloud backup for one user</summary>
        CloudBackup,
        /// <summary>Sends the report once everything else is done.  Inserted last, so its presence also means planning finished.</summary>
        Finalize
    }

    public enum NightlyJobStatus { pending, running, done, failed }

    /// <summary>
    /// A row in the nightlyjobs table.
    /// </summary>
    public class NightlyJob
    {
        #region Properties
        public int ID { get; private set; }
        public string RunKey { get; private set; }
        public DateTime RunDate { get; private set; }
        public NightlyJobType JobType { get; private set; }
        public string Username { get; private set; }
        public string Payload { get; private set; }
        public NightlyJobStatus Status { get; private set; }
        public int Attempts { get; private set; }
        public string Owner { get; private set; }
        public string Result { get; private set; }
        public string Failures { get; private set; }
        #endregion

        internal NightlyJob(MySqlDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
            ID = Convert.ToInt32(dr["id"], CultureInfo.InvariantCulture);
            RunKey = util.ReadNullableString(dr, "runKey");
            RunDate = Convert.ToDateTime(dr["runDate"], CultureInfo.InvariantCulture);
            JobType = (NightlyJobType)Enum.Parse(typeof(NightlyJobType), util.ReadNullableString(dr, "jobType"));
            Username = util.ReadNullableString(dr, "username");
            Payload = util.ReadNullableString(dr, "payload");
            Status = (NightlyJobStatus)Enum.Parse(typeof(NightlyJobStatus), util.ReadNullableString(dr, "status"));
            Attempts = Convert.ToInt32(dr["attempts"], CultureInfo.InvariantCulture);
            Owner = util.ReadNullableString(dr, "owner");
            Result = util.ReadNullableString(dr, "result");
            Failures = util.ReadNullableString(dr, "failures");
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0} {1} ({2}, attempt {3})", JobType, String.IsNullOrEmpty(Username) ? "(all)" : Username, Status, Attempts);
        }
    }

    /// <summary>
    /// The outcome of running a single job.
    /// </summary>
    public class NightlyJobOutcome
    {
        public bool Succeeded { get; set; } = true;
        public string Result { get; set; } = string.Empty;
        public string Failures { get; set; } = string.Empty;

        public NightlyJobOutcome() { }

        public NightlyJobOutcome(string result, string failures = null)
        {
            Result = result ?? string.Empty;
            Failures = failures ?? string.Empty;
        }
    }

    /// <summary>
    /// A job to be added to the queue at planning time.
    /// </summary>
    public class PlannedNightlyJob
    {
        public NightlyJobType JobType { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;

        public PlannedNightlyJob(NightlyJobType jobType, string username = null, string payload = null)
        {
            JobType = jobType;
            Username = username ?? string.Empty;
            Payload = payload ?? string.Empty;
        }
    }
    #endregion

    /// <summary>
    /// Database operations on the nightlyjobs table.
    ///
    /// A job is claimed by setting it to "running" with a unique owner GUID and a short lease.  While the job runs, a heartbeat keeps extending the lease.
    /// If the process dies, the heartbeat stops, the lease expires, and the next worker reclaims the job.  Every completion update requires a matching owner,
    /// so a straggler that finishes after its job was reclaimed can't overwrite the newer result.
    /// </summary>
    public static class NightlyJobQueue
    {
        /// <summary>A job that has been claimed this many times without finishing is marked failed (e.g., a user whose backup crashes the process).</summary>
        public const int MaxAttempts = 3;

        /// <summary>How long a claim lasts without a heartbeat</summary>
        public const int LeaseSeconds = 300;

        /// <summary>How often a running job extends its lease</summary>
        public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(60);

        /// <summary>Stop extending the lease after this long, so a hung job can be reclaimed</summary>
        public static readonly TimeSpan MaxJobDuration = TimeSpan.FromHours(2);

        /// <summary>Rows older than this are deleted when a new run is planned</summary>
        public const int RetentionDays = 30;

        private const int InsertBatchSize = 200;

        private static string FinalizeName { get { return NightlyJobType.Finalize.ToString(); } }

        public static int PriorityFor(NightlyJobType jt)
        {
            switch (jt)
            {
                case NightlyJobType.Email:
                    return 10;
                case NightlyJobType.MonthlyEmail:
                    return 20;
                case NightlyJobType.PendingFlightsReminder:
                    return 30;
                case NightlyJobType.GratuityReminders:
                    return 40;
                case NightlyJobType.ClubReports:
                    return 50;
                case NightlyJobType.CloudBackup:
                    return 100;
                case NightlyJobType.Finalize:
                default:
                    return 1000;
            }
        }

        private static void ThrowOnError(DBHelper dbh, string szContext)
        {
            if (!String.IsNullOrEmpty(dbh.LastError))
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Nightly job queue error ({0}): {1}", szContext, dbh.LastError));
        }

        private static int Count(string szQ, Action<MySqlCommand> init)
        {
            int c = 0;
            DBHelper dbh = new DBHelper(szQ);
            dbh.ReadRow((comm) => { init(comm); }, (dr) => { c = Convert.ToInt32(dr["c"], CultureInfo.InvariantCulture); });
            ThrowOnError(dbh, "count");
            return c;
        }

        /// <summary>
        /// Adds jobs to the queue.  Uses INSERT IGNORE, so adding a job that already exists for the run is a no-op; that makes planning safe to repeat.
        /// </summary>
        public static void AddJobs(string runKey, DateTime runDate, IEnumerable<PlannedNightlyJob> jobs)
        {
            if (jobs == null)
                throw new ArgumentNullException(nameof(jobs));
            List<PlannedNightlyJob> lst = new List<PlannedNightlyJob>(jobs);

            for (int i = 0; i < lst.Count; i += InsertBatchSize)
            {
                List<PlannedNightlyJob> batch = lst.GetRange(i, Math.Min(InsertBatchSize, lst.Count - i));
                StringBuilder sb = new StringBuilder("INSERT IGNORE INTO nightlyjobs (runKey, runDate, jobType, username, priority, payload) VALUES ");
                for (int j = 0; j < batch.Count; j++)
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0}(?rk, ?rd, ?jt{1}, ?u{1}, ?pri{1}, ?pl{1})", j == 0 ? string.Empty : ", ", j);

                DBHelper dbh = new DBHelper(sb.ToString());
                dbh.DoNonQuery((comm) =>
                {
                    comm.Parameters.AddWithValue("rk", runKey);
                    comm.Parameters.AddWithValue("rd", runDate.Date);
                    for (int j = 0; j < batch.Count; j++)
                    {
                        comm.Parameters.AddWithValue("jt" + j.ToString(CultureInfo.InvariantCulture), batch[j].JobType.ToString());
                        comm.Parameters.AddWithValue("u" + j.ToString(CultureInfo.InvariantCulture), batch[j].Username);
                        comm.Parameters.AddWithValue("pri" + j.ToString(CultureInfo.InvariantCulture), PriorityFor(batch[j].JobType));
                        comm.Parameters.AddWithValue("pl" + j.ToString(CultureInfo.InvariantCulture), batch[j].Payload);
                    }
                });
                ThrowOnError(dbh, "add jobs");
            }
        }

        /// <summary>
        /// True if planning for the run finished (the Finalize row is inserted last).
        /// </summary>
        public static bool IsPlanned(string runKey)
        {
            return Count("SELECT COUNT(*) AS c FROM nightlyjobs WHERE runKey=?rk AND jobType=?fin", (comm) =>
            {
                comm.Parameters.AddWithValue("rk", runKey);
                comm.Parameters.AddWithValue("fin", FinalizeName);
            }) > 0;
        }

        /// <summary>
        /// True if the run is planned and every job, including Finalize, is done or failed.
        /// </summary>
        public static bool IsComplete(string runKey)
        {
            return IsPlanned(runKey) && Count("SELECT COUNT(*) AS c FROM nightlyjobs WHERE runKey=?rk AND status IN ('pending', 'running')", (comm) => { comm.Parameters.AddWithValue("rk", runKey); }) == 0;
        }

        /// <summary>
        /// Number of non-Finalize jobs that are still pending or running.
        /// </summary>
        public static int OutstandingCount(string runKey)
        {
            return Count("SELECT COUNT(*) AS c FROM nightlyjobs WHERE runKey=?rk AND jobType<>?fin AND status IN ('pending', 'running')", (comm) =>
            {
                comm.Parameters.AddWithValue("rk", runKey);
                comm.Parameters.AddWithValue("fin", FinalizeName);
            });
        }

        private static NightlyJob JobForOwner(string owner)
        {
            NightlyJob job = null;
            DBHelper dbh = new DBHelper("SELECT * FROM nightlyjobs WHERE owner=?owner AND status='running'");
            dbh.ReadRow((comm) => { comm.Parameters.AddWithValue("owner", owner); }, (dr) => { job = new NightlyJob(dr); });
            ThrowOnError(dbh, "read claimed job");
            return job;
        }

        private static NightlyJob Claim(string runKey, bool fFinalize)
        {
            string owner = Guid.NewGuid().ToString();
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, @"UPDATE nightlyjobs
SET status='running', owner=?owner, attempts=attempts+1, leaseExpires=DATE_ADD(NOW(), INTERVAL ?lease SECOND)
WHERE runKey=?rk AND jobType{0}?fin AND attempts < ?maxAttempts AND (status='pending' OR (status='running' AND leaseExpires < NOW()))
ORDER BY priority ASC, id ASC
LIMIT 1", fFinalize ? "=" : "<>"));
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("owner", owner);
                comm.Parameters.AddWithValue("lease", LeaseSeconds);
                comm.Parameters.AddWithValue("rk", runKey);
                comm.Parameters.AddWithValue("fin", FinalizeName);
                comm.Parameters.AddWithValue("maxAttempts", MaxAttempts);
            });
            ThrowOnError(dbh, "claim");
            return JobForOwner(owner);
        }

        /// <summary>
        /// Atomically claims the next pending job (or one whose lease expired), in priority order.  Never returns the Finalize job.
        /// </summary>
        /// <returns>The claimed job, or null if there's nothing to claim</returns>
        public static NightlyJob ClaimNext(string runKey)
        {
            return Claim(runKey, false);
        }

        /// <summary>
        /// Claims the Finalize job, but only if nothing else in the run is outstanding.
        /// </summary>
        public static NightlyJob ClaimFinalize(string runKey)
        {
            return OutstandingCount(runKey) > 0 ? null : Claim(runKey, true);
        }

        /// <summary>
        /// Marks as failed any job that has used up its attempts and isn't actively running, so that the run can complete.
        /// </summary>
        public static void FailExhaustedJobs(string runKey)
        {
            DBHelper dbh = new DBHelper(@"UPDATE nightlyjobs
SET status='failed', leaseExpires=NULL, dateCompleted=NOW(), failures=CONCAT(COALESCE(failures, ''), ?msg)
WHERE runKey=?rk AND attempts >= ?maxAttempts AND (status='pending' OR (status='running' AND leaseExpires < NOW()))");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("msg", String.Format(CultureInfo.InvariantCulture, "Job did not finish after {0} attempts; the process probably died or hung while running it.", MaxAttempts));
                comm.Parameters.AddWithValue("rk", runKey);
                comm.Parameters.AddWithValue("maxAttempts", MaxAttempts);
            });
            ThrowOnError(dbh, "fail exhausted jobs");
        }

        /// <summary>
        /// Extends the lease on a running job.
        /// </summary>
        public static void Heartbeat(NightlyJob job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));
            DBHelper dbh = new DBHelper("UPDATE nightlyjobs SET leaseExpires=DATE_ADD(NOW(), INTERVAL ?lease SECOND) WHERE id=?id AND owner=?owner AND status='running'");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("lease", LeaseSeconds);
                comm.Parameters.AddWithValue("id", job.ID);
                comm.Parameters.AddWithValue("owner", job.Owner);
            });
        }

        /// <summary>
        /// Records the outcome of a job.  No-op if the job has since been reclaimed by someone else.
        /// </summary>
        public static void Complete(NightlyJob job, NightlyJobOutcome outcome)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));
            if (outcome == null)
                throw new ArgumentNullException(nameof(outcome));
            DBHelper dbh = new DBHelper("UPDATE nightlyjobs SET status=?st, leaseExpires=NULL, dateCompleted=NOW(), result=?res, failures=?fail WHERE id=?id AND owner=?owner AND status='running'");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("st", (outcome.Succeeded ? NightlyJobStatus.done : NightlyJobStatus.failed).ToString());
                comm.Parameters.AddWithValue("res", outcome.Result);
                comm.Parameters.AddWithValue("fail", outcome.Failures);
                comm.Parameters.AddWithValue("id", job.ID);
                comm.Parameters.AddWithValue("owner", job.Owner);
            });
            ThrowOnError(dbh, "complete job");
        }

        /// <summary>
        /// All of the jobs in a run.
        /// </summary>
        public static IEnumerable<NightlyJob> JobsForRun(string runKey)
        {
            List<NightlyJob> lst = new List<NightlyJob>();
            DBHelper dbh = new DBHelper("SELECT * FROM nightlyjobs WHERE runKey=?rk ORDER BY priority ASC, id ASC");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("rk", runKey); }, (dr) => { lst.Add(new NightlyJob(dr)); });
            ThrowOnError(dbh, "read jobs");
            return lst;
        }

        /// <summary>
        /// Deletes all of the jobs in a run, so that it can be planned from scratch.
        /// </summary>
        public static void DeleteRun(string runKey)
        {
            DBHelper dbh = new DBHelper("DELETE FROM nightlyjobs WHERE runKey=?rk");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("rk", runKey); });
            ThrowOnError(dbh, "delete run");
        }

        /// <summary>
        /// Deletes old runs.
        /// </summary>
        public static void PurgeOldRuns()
        {
            DBHelper dbh = new DBHelper("DELETE FROM nightlyjobs WHERE runDate < DATE_SUB(CURDATE(), INTERVAL ?days DAY)");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("days", RetentionDays); });
            ThrowOnError(dbh, "purge");
        }

        /// <summary>
        /// Human-readable summary of a run: counts by job type and status.
        /// </summary>
        public static string StatusSummary(string runKey)
        {
            SortedDictionary<string, Dictionary<string, int>> d = new SortedDictionary<string, Dictionary<string, int>>();
            DBHelper dbh = new DBHelper("SELECT jobType, status, COUNT(*) AS c FROM nightlyjobs WHERE runKey=?rk GROUP BY jobType, status");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("rk", runKey); }, (dr) =>
            {
                string jt = Convert.ToString(dr["jobType"], CultureInfo.InvariantCulture);
                if (!d.ContainsKey(jt))
                    d[jt] = new Dictionary<string, int>();
                d[jt][Convert.ToString(dr["status"], CultureInfo.InvariantCulture)] = Convert.ToInt32(dr["c"], CultureInfo.InvariantCulture);
            });
            ThrowOnError(dbh, "status");

            if (d.Count == 0)
                return String.Format(CultureInfo.InvariantCulture, "Run {0}: not planned yet.", runKey);

            StringBuilder sb = new StringBuilder(String.Format(CultureInfo.InvariantCulture, "Run {0}:\r\n", runKey));
            foreach (string jt in d.Keys)
            {
                Dictionary<string, int> counts = d[jt];
                counts.TryGetValue("pending", out int pending);
                counts.TryGetValue("running", out int running);
                counts.TryGetValue("done", out int done);
                counts.TryGetValue("failed", out int failed);
                sb.AppendFormat(CultureInfo.InvariantCulture, "  {0}: {1} done, {2} failed, {3} running, {4} pending\r\n", jt, done, failed, running, pending);
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// The nightly run, as a resumable queue of jobs.
    ///
    /// StartOrResumeNightlyRun is idempotent: call it as often as you like (e.g., every 15 minutes from Task Scheduler).
    ///  - If the run hasn't been planned, the first worker plans it (one row per user per task), then works through it.
    ///  - If the run is in progress, it starts workers if none are running in this process; expired leases get picked back up.
    ///  - If the run is complete, it does nothing.
    /// </summary>
    public partial class EmailSubscriptionManager
    {
        #region Worker tracking
        private static readonly object workerLock = new object();
        private static readonly Dictionary<string, int> activeWorkers = new Dictionary<string, int>();
        private static readonly SemaphoreSlim planLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Number of workers to run in parallel within a process.  Claims are atomic, so more than one is safe; 1 matches the old (serial) behavior.
        /// </summary>
        public static int WorkerCount { get; set; } = 2;
        #endregion

        /// <summary>
        /// The date the run is for.  Defaults to today.
        /// </summary>
        public DateTime RunDate { get; set; } = DateTime.Now.Date;

        /// <summary>
        /// Identifies the run.  A full run for a day is just the date; partial or single-user (debug) runs get their own key so they never collide with (or get blocked by) the real run.
        /// </summary>
        public string RunKey
        {
            get
            {
                StringBuilder sb = new StringBuilder(RunDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                if (TasksToRun != SelectedTasks.All)
                    sb.Append("-" + TasksToRun.ToString());
                if (!String.IsNullOrEmpty(UserRestriction))
                    sb.Append("-dbg-" + UserRestriction);
                return sb.ToString();
            }
        }

        private bool IsIncluded(string szUser)
        {
            return String.IsNullOrEmpty(UserRestriction) || String.Compare(szUser, UserRestriction, StringComparison.OrdinalIgnoreCase) == 0;
        }

        #region Starting
        /// <summary>
        /// Starts, resumes, or (if fRestart) restarts the nightly run.  Returns immediately; the work happens on background workers.
        /// </summary>
        /// <param name="fRestart">If true, throws away any existing state for this run and plans it again from scratch.</param>
        /// <returns>A status message</returns>
        public string StartOrResumeNightlyRun(bool fRestart = false)
        {
            if (ActiveBrand == null)
                ActiveBrand = Branding.CurrentBrand;

            string runKey = RunKey;

            if (fRestart)
            {
                EventRecorder.LogCall("Nightly run {runKey}: restarting from scratch", runKey);
                NightlyJobQueue.DeleteRun(runKey);
            }
            else if (NightlyJobQueue.IsComplete(runKey))
                return "Nothing to do - run is complete.\r\n" + NightlyJobQueue.StatusSummary(runKey);

            int cStarted = StartWorkers(runKey);
            return String.Format(CultureInfo.InvariantCulture, "Started {0} worker(s).\r\n{1}", cStarted, NightlyJobQueue.StatusSummary(runKey));
        }

        /// <summary>
        /// Status of the run, without starting anything.
        /// </summary>
        public string NightlyRunStatus()
        {
            string runKey = RunKey;
            int cActive;
            lock (workerLock)
                activeWorkers.TryGetValue(runKey, out cActive);
            return String.Format(CultureInfo.InvariantCulture, "{0} worker(s) active in this process.\r\n{1}", cActive, NightlyJobQueue.StatusSummary(runKey));
        }

        private int StartWorkers(string runKey)
        {
            int cToStart;
            lock (workerLock)
            {
                activeWorkers.TryGetValue(runKey, out int cActive);
                cToStart = Math.Max(0, WorkerCount - cActive);
                activeWorkers[runKey] = cActive + cToStart;
            }

            for (int i = 0; i < cToStart; i++)
            {
                try
                {
                    // Unlike a raw thread, ASP.NET knows about these: on shutdown it signals the token and waits briefly before tearing down the AppDomain.
                    HostingEnvironment.QueueBackgroundWorkItem(ct => RunWorkerAsync(runKey, ct));
                }
                catch (InvalidOperationException ex)
                {
                    WorkerFinished(runKey);
                    EventRecorder.LogCall("Nightly run {runKey}: unable to queue worker: {msg}", runKey, ex.Message);
                }
            }
            return cToStart;
        }

        private static void WorkerFinished(string runKey)
        {
            lock (workerLock)
            {
                if (activeWorkers.TryGetValue(runKey, out int c))
                {
                    if (c <= 1)
                        activeWorkers.Remove(runKey);
                    else
                        activeWorkers[runKey] = c - 1;
                }
            }
        }

        private async Task RunWorkerAsync(string runKey, CancellationToken ct)
        {
            try
            {
                await planLock.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    if (!NightlyJobQueue.IsPlanned(runKey))
                        PlanRun(runKey);
                }
                finally
                {
                    planLock.Release();
                }

                await DrainAsync(runKey, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                EventRecorder.LogCall("Nightly run {runKey}: worker cancelled (app shutting down)", runKey);
            }
            catch (Exception ex) when (ex.IsNonFatal())
            {
                EventRecorder.LogCall("Nightly run {runKey}: worker had exception: {msg}, {stacktrace}", runKey, ex.Message, ex.StackTrace);
            }
            finally
            {
                WorkerFinished(runKey);
            }
        }
        #endregion

        #region Planning
        /// <summary>
        /// Builds the job list for the run.  Has no side effects other than inserting rows (and purging old runs), so it's safe to repeat if it gets interrupted.
        /// </summary>
        private void PlanRun(string runKey)
        {
            EventRecorder.LogCall("Starting nightly run {runKey}: planning", runKey);
            NightlyJobQueue.PurgeOldRuns();

            List<PlannedNightlyJob> lst = new List<PlannedNightlyJob>();
            bool fAll = TasksToRun == SelectedTasks.All;

            if (fAll || TasksToRun == SelectedTasks.EmailOnly)
                lst.AddRange(PlanEmailJobs());

            if (fAll || TasksToRun == SelectedTasks.CloudStorageOnly)
                lst.AddRange(PlanCloudBackupJobs());

            // Run-wide tasks only happen on a full run, so that re-running just email or just backups doesn't re-send these.
            if (fAll)
            {
                lst.Add(new PlannedNightlyJob(NightlyJobType.GratuityReminders, string.Empty, UserRestriction));

                // Club reports go to every club, so skip them on a single-user (debug) run.
                if (RunDate.Day == 1 && String.IsNullOrEmpty(UserRestriction))
                    lst.Add(new PlannedNightlyJob(NightlyJobType.ClubReports));
            }

            NightlyJobQueue.AddJobs(runKey, RunDate, lst);

            // Finalize goes in last; its presence means planning is complete.
            NightlyJobQueue.AddJobs(runKey, RunDate, new PlannedNightlyJob[] { new PlannedNightlyJob(NightlyJobType.Finalize) });

            EventRecorder.LogCall("Nightly run {runKey}: planned {count} jobs", runKey, lst.Count);
        }

        private IEnumerable<PlannedNightlyJob> PlanEmailJobs()
        {
            List<PlannedNightlyJob> lst = new List<PlannedNightlyJob>();
            DateTime dtMin = RunDate.AddDays(-7);

            // Users who qualify for expiring currency notifications (which pre-empt the weekly email) - must have the gratuity AND be subscribed for expiration notices.
            HashSet<string> subscribedForExpiration = new HashSet<string>(Profile.UsersWithSubscriptions(EmailSubscription.FlagForType(SubscriptionType.Expiration), dtMin).Select(pf => pf.UserName), StringComparer.OrdinalIgnoreCase);
            HashSet<string> currencyCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (EarnedGratuity eg in EarnedGratuity.GratuitiesForUser(null, Gratuity.GratuityTypes.CurrencyAlerts))
            {
                if (IsIncluded(eg.Username) && subscribedForExpiration.Contains(eg.Username))
                    currencyCandidates.Add(eg.Username);
            }

            // Users with a subscription OTHER than simple monthly or expiration
            HashSet<string> weekly = new HashSet<string>(Profile.UsersWithSubscriptions(~(EmailSubscription.FlagForType(SubscriptionType.MonthlyTotals) | EmailSubscription.FlagForType(SubscriptionType.Expiration)), dtMin).Select(pf => pf.UserName).Where(IsIncluded), StringComparer.OrdinalIgnoreCase);

            HashSet<string> allEmail = new HashSet<string>(currencyCandidates, StringComparer.OrdinalIgnoreCase);
            allEmail.UnionWith(weekly);
            foreach (string szUser in allEmail)
                lst.Add(new PlannedNightlyJob(NightlyJobType.Email, szUser, (currencyCandidates.Contains(szUser) ? "C" : string.Empty) + (weekly.Contains(szUser) ? "W" : string.Empty)));

            // Monthly/annual emails
            if (RunDate.Day == 1)
            {
                foreach (Profile pf in Profile.UsersWithSubscriptions(EmailSubscription.FlagForType(SubscriptionType.MonthlyTotals), RunDate.AddDays(1)))
                    if (IsIncluded(pf.UserName))
                        lst.Add(new PlannedNightlyJob(NightlyJobType.MonthlyEmail, pf.UserName));
            }

            // Pending flights reminder - every week or so
            if (RunDate.DayOfYear % 7 == 6)
            {
                IDictionary<string, int> usersWithPendingFlights = PendingFlight.UsersWithLotsOfPendingFlights(30);
                foreach (string szUser in usersWithPendingFlights.Keys)
                    if (IsIncluded(szUser))
                        lst.Add(new PlannedNightlyJob(NightlyJobType.PendingFlightsReminder, szUser, usersWithPendingFlights[szUser].ToString(CultureInfo.InvariantCulture)));
            }

            return lst;
        }

        private IEnumerable<PlannedNightlyJob> PlanCloudBackupJobs()
        {
            List<PlannedNightlyJob> lst = new List<PlannedNightlyJob>();
            foreach (EarnedGratuity eg in EarnedGratuity.GratuitiesForUser(string.Empty, Gratuity.GratuityTypes.CloudBackup))
            {
                if (IsIncluded(eg.Username) && eg.UserProfile != null && eg.UserProfile.BestCloudStorage != StorageID.None && eg.CurrentStatus != EarnedGratuity.EarnedGratuityStatus.Expired)
                    lst.Add(new PlannedNightlyJob(NightlyJobType.CloudBackup, eg.Username));
            }
            return lst;
        }
        #endregion

        #region Draining
        private async Task DrainAsync(string runKey, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                NightlyJob job = NightlyJobQueue.ClaimNext(runKey);
                if (job == null)
                {
                    NightlyJobQueue.FailExhaustedJobs(runKey);

                    // Anything still outstanding is being worked on by another worker (or is in a dead process, waiting for its lease to expire - the next kick-off will pick it up).
                    job = NightlyJobQueue.ClaimFinalize(runKey);
                    if (job == null)
                        return;
                }

                await RunOneJobAsync(job).ConfigureAwait(false);
            }
            EventRecorder.LogCall("Nightly run {runKey}: worker stopping (app shutting down)", runKey);
        }

        private async Task RunOneJobAsync(NightlyJob job)
        {
            DateTime dtStarted = DateTime.UtcNow;
            using (Timer heartbeat = new Timer((o) =>
            {
                // Exceptions on a timer thread would take down the process, so eat everything non-fatal here.
                try
                {
                    if (DateTime.UtcNow - dtStarted < NightlyJobQueue.MaxJobDuration)
                        NightlyJobQueue.Heartbeat(job);
                }
                catch (Exception ex) when (ex.IsNonFatal()) { }
            }, null, NightlyJobQueue.HeartbeatInterval, NightlyJobQueue.HeartbeatInterval))
            {
                NightlyJobOutcome outcome;
                try
                {
                    outcome = await ExecuteJobAsync(job).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex.IsNonFatal())
                {
                    outcome = new NightlyJobOutcome(string.Empty, String.Format(CultureInfo.InvariantCulture, "{0} FAILED for user {1}: {2} {3}\r\n{4}\r\n\r\n", job.JobType, job.Username, ex.GetType().ToString(), ex.Message, ex.StackTrace)) { Succeeded = false };
                }

                NightlyJobQueue.Complete(job, outcome);
            }
        }

        private async Task<NightlyJobOutcome> ExecuteJobAsync(NightlyJob job)
        {
            switch (job.JobType)
            {
                case NightlyJobType.Email:
                    return await RunEmailJobAsync(job).ConfigureAwait(false);
                case NightlyJobType.MonthlyEmail:
                    return await RunMonthlyEmailJobAsync(job).ConfigureAwait(false);
                case NightlyJobType.PendingFlightsReminder:
                    return RunPendingFlightsJob(job);
                case NightlyJobType.GratuityReminders:
                    EarnedGratuity.SendRemindersOfExpiringGratuities(String.IsNullOrEmpty(job.Payload) ? null : job.Payload);
                    return new NightlyJobOutcome("Gratuity reminders sent");
                case NightlyJobType.ClubReports:
                    Clubs.Club.SendMonthlyClubReports();
                    return new NightlyJobOutcome("Club reports sent");
                case NightlyJobType.CloudBackup:
                    return await RunCloudBackupJobAsync(job).ConfigureAwait(false);
                case NightlyJobType.Finalize:
                    return RunFinalizeJob(job);
                default:
                    throw new InvalidOperationException("Unknown nightly job type: " + job.JobType.ToString());
            }
        }
        #endregion

        #region Individual jobs
        private static Profile FreshProfile(string szUser)
        {
            Profile pf = Profile.GetUser(szUser, true);
            return (pf == null || String.IsNullOrEmpty(pf.UserName) || String.IsNullOrEmpty(pf.Email)) ? null : pf;
        }

        /// <summary>
        /// Weekly email, possibly pre-empted by an expiring-currency email.
        /// </summary>
        private async Task<NightlyJobOutcome> RunEmailJobAsync(NightlyJob job)
        {
            Profile pf = FreshProfile(job.Username);
            if (pf == null)
                return new NightlyJobOutcome("User not found; skipped");

            // If we already sent this user mail since planning (e.g., we're retrying after a crash that happened just after sending), don't send again.
            DateTime dtMin = job.RunDate.AddDays(-7).Date;
            if (pf.LastEmailDate > dtMin)
                return new NightlyJobOutcome(String.Format(CultureInfo.InvariantCulture, "Already emailed {0:s}; skipped", pf.LastEmailDate));

            string szPayload = job.Payload ?? string.Empty;

            if (szPayload.Contains("C"))
            {
                EarnedGratuity eg = EarnedGratuity.GratuitiesForUser(pf.UserName, Gratuity.GratuityTypes.CurrencyAlerts).FirstOrDefault();
                if (eg != null)
                {
                    IEnumerable<CurrencyStatusItem> expiringCurrencies = CurrencyStatusItem.CheckForExpiringCurrencies(eg.State, eg.Username, out string newState);
                    bool fSent = expiringCurrencies.Any() && await SendMailForUser(pf, Resources.Profile.EmailCurrencyExpiringMailSubject, string.Empty).ConfigureAwait(false);

                    // Save the state after sending, so that if we die mid-send we'll detect (and send) it again on retry.
                    if (newState.CompareOrdinal(eg.State) != 0)
                    {
                        eg.State = newState;
                        eg.Commit();
                    }

                    if (fSent)
                    {
                        // Don't send the weekly mail, since we just pre-empted it.
                        pf.LastEmailDate = DateTime.Now;
                        pf.FCommit();
                        return new NightlyJobOutcome("Expiring currency email sent");
                    }
                }
            }

            if (szPayload.Contains("W"))
            {
                if (await SendMailForUser(pf, Resources.Profile.EmailWeeklyMailSubject, string.Empty).ConfigureAwait(false))
                {
                    pf.LastEmailDate = DateTime.Now;
                    pf.FCommit();
                    return new NightlyJobOutcome("Weekly email sent");
                }
                return new NightlyJobOutcome(string.Empty, String.Format(CultureInfo.InvariantCulture, "Weekly email NOT sent for {0}\r\n", pf.UserName)) { Succeeded = false };
            }

            return new NightlyJobOutcome("No email needed");
        }

        private async Task<NightlyJobOutcome> RunMonthlyEmailJobAsync(NightlyJob job)
        {
            Profile pf = FreshProfile(job.Username);
            if (pf == null)
                return new NightlyJobOutcome("User not found; skipped");

            // Issue #1168 - if Jan 1 mail, be clear that this includes year in review!
            DateTime dtLastMonth = job.RunDate.AddMonths(-1);
            string szSubject = (job.RunDate.Month == 1) ?
                String.Format(CultureInfo.CurrentCulture, Resources.Profile.EmailMonthlySubjectYearInReview, dtLastMonth.ToString("MMMM", CultureInfo.InvariantCulture), dtLastMonth.Year) :
                String.Format(CultureInfo.CurrentCulture, Resources.Profile.EmailMonthlySubject, dtLastMonth.ToString("MMMM", CultureInfo.InvariantCulture));

            // We don't update the last-email sent on this because this email is asynchronous - i.e., not dependent on any other mail that was sent.
            return await SendMailForUser(pf, szSubject, "monthly").ConfigureAwait(false) ?
                new NightlyJobOutcome("Monthly email sent") :
                new NightlyJobOutcome(string.Empty, String.Format(CultureInfo.InvariantCulture, "Monthly email NOT sent for {0}\r\n", pf.UserName)) { Succeeded = false };
        }

        private static NightlyJobOutcome RunPendingFlightsJob(NightlyJob job)
        {
            Profile pf = FreshProfile(job.Username);
            if (pf == null)
                return new NightlyJobOutcome("User not found; skipped");

            int cPending = int.TryParse(job.Payload, NumberStyles.Integer, CultureInfo.InvariantCulture, out int c) ? c : 0;
            util.NotifyUser(Branding.ReBrand(Resources.Profile.PendingFlightsReminderSubject),
                Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.Profile.PendingFlightsReminder, pf.UserFullName, cPending)),
                new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, true);
            return new NightlyJobOutcome("Pending flights reminder sent");
        }

        private async Task<NightlyJobOutcome> RunCloudBackupJobAsync(NightlyJob job)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder sbFailures = new StringBuilder();

            EarnedGratuity eg = EarnedGratuity.GratuitiesForUser(job.Username, Gratuity.GratuityTypes.CloudBackup).FirstOrDefault();
            Profile pf = eg?.UserProfile;
            StorageID sid = StorageID.None;
            if (pf == null || eg.CurrentStatus == EarnedGratuity.EarnedGratuityStatus.Expired || (sid = pf.BestCloudStorage) == StorageID.None)
                return new NightlyJobOutcome("No longer eligible for cloud backup; skipped");

            try
            {
                LogbookBackup lb = new LogbookBackup(pf);

                EventRecorder.LogCall("Nightly run: backing up to {storage} for user {user}", sid.ToString(), job.Username);

                switch (sid)
                {
                    case StorageID.Dropbox:
                        await BackupDropbox(lb, pf, sb, sbFailures).ConfigureAwait(false);
                        break;
                    case StorageID.OneDrive:
                        await BackupOneDrive(lb, pf, sb, sbFailures).ConfigureAwait(false);
                        break;
                    case StorageID.GoogleDrive:
                        await BackupGoogleDrive(lb, pf, sb, sbFailures).ConfigureAwait(false);
                        break;
                    case StorageID.Box:
                        await BackupBox(lb, pf, sb, sbFailures).ConfigureAwait(false);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex) when (ex.IsNonFatal())
            {
                string szError = String.Format(CultureInfo.CurrentCulture, "eg user={0}\r\n\r\n{1}\r\n\r\n{2}", job.Username, sbFailures.ToString(), sb.ToString());
                util.NotifyAdminException("ERROR running nightly backup", new MyFlightbookException(szError, ex));
                sbFailures.AppendFormat(CultureInfo.CurrentCulture, "Backup FAILED for user {0}: {1} {2}\r\n\r\n", job.Username, ex.GetType().ToString(), ex.Message);
            }

            // The individual backup methods report their own failures (and notify the user), so the job itself is "done" either way; the text goes in the report.
            return new NightlyJobOutcome(sb.ToString().Trim(), sbFailures.ToString().Trim());
        }

        /// <summary>
        /// Sends the report.  Built from the database rather than temp files, so it covers the whole run even if it was interrupted and resumed.
        /// </summary>
        private static NightlyJobOutcome RunFinalizeJob(NightlyJob job)
        {
            List<NightlyJob> jobs = NightlyJobQueue.JobsForRun(job.RunKey).Where(j => j.JobType != NightlyJobType.Finalize).ToList();
            List<NightlyJob> backups = jobs.Where(j => j.JobType == NightlyJobType.CloudBackup).ToList();
            List<NightlyJob> failed = jobs.Where(j => j.Status == NightlyJobStatus.failed).ToList();

            StringBuilder sbSummary = new StringBuilder();
            AppendSummary(sbSummary, job, jobs, failed);

            StringBuilder sbBody = new StringBuilder(sbSummary.ToString());
            foreach (NightlyJob j in backups.Where(j => !String.IsNullOrWhiteSpace(j.Failures)))
                sbBody.AppendLine(j.Failures.Trim());
            foreach (NightlyJob j in failed.Where(j => j.JobType != NightlyJobType.CloudBackup && !String.IsNullOrWhiteSpace(j.Failures)))
                sbBody.AppendLine(j.Failures.Trim());
            sbBody.AppendLine();
            foreach (NightlyJob j in backups.Where(j => !String.IsNullOrWhiteSpace(j.Result)))
                sbBody.AppendLine(j.Result.Trim());

            if (backups.Count > 0)
                util.NotifyAdminEvent("Cloud Backup report", sbBody.ToString(), ProfileRoles.maskCanReport);
            else if (failed.Count > 0)
                util.NotifyAdminEvent("Nightly run report", sbBody.ToString(), ProfileRoles.maskCanReport);

            EventRecorder.LogCall("Ended nightly run {runKey}", job.RunKey);
            return new NightlyJobOutcome(sbSummary.ToString());
        }

        private static void AppendSummary(StringBuilder sb, NightlyJob job, List<NightlyJob> jobs, List<NightlyJob> failed)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "Nightly run {0}: {1} jobs, {2} failed.\r\n", job.RunKey, jobs.Count, failed.Count);
            foreach (NightlyJob j in failed)
                sb.AppendFormat(CultureInfo.InvariantCulture, "  FAILED: {0}\r\n", j.ToString());
            sb.AppendLine();
        }
        #endregion
    }
}
