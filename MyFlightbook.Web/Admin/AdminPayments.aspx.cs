using MyFlightbook.Payments;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2009-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Admin
{
    public partial class AdminPayments : AdminPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            CheckAdmin(Profile.GetUser(Page.User.Identity.Name).CanManageMoney);
            Master.SelectedTab = tabID.admDonations;

            if (!IsPostBack)
            {
                RefreshDonations();
                RefreshRunning30();

                util.SetValidationGroup(pnlTestTransaction, "valTestTransaction");
                dateTestTransaction.Date = DateTime.Now;
            }
        }

        protected const string szDonationsTemplate = "SELECT *, (Amount - ABS(Fee)) AS Net FROM payments WHERE username LIKE ?user {0} ORDER BY date DESC";

        protected void RefreshRunning30()
        {
            List<Payment> lstPayments = new List<Payment>();
            DateTime dtMin = DateTime.Now.AddYears(-1).Date;
            DateTime dtMax = DateTime.Now.Date;

            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, szDonationsTemplate, " AND date > ?dtMin AND TransactionType IN (0, 2, 3) "));
            dbh.ReadRows((comm) =>
            {
                comm.Parameters.AddWithValue("user", "%");
                comm.Parameters.AddWithValue("dtMin", dtMin);
            }, (dr) =>
            {
                lstPayments.Add(new Payment(Convert.ToDateTime(dr["Date"], CultureInfo.InvariantCulture), string.Empty, Convert.ToDecimal(dr["Amount"], CultureInfo.InvariantCulture), Convert.ToDecimal(dr["Fee"], CultureInfo.InvariantCulture), (Payment.TransactionType)((int)dr["TransactionType"]), string.Empty, string.Empty, string.Empty));
            });
            lstPayments.Reverse();  // go in ascending order

            // Now compute a year's worth of 30-day averages.
            DateTime dtMin30 = DateTime.Now.AddYears(-1).AddDays(30).Date;
            Queue<Payment> queue = new Queue<Payment>();

            int iPayment = 0;
            decimal running30 = 0.0M;
            for (DateTime dt = dtMin; dt.CompareTo(dtMax) <= 0; dt = dt.AddDays(1))
            {
                // while there are payments to add to the rolling 30-day window, add them
                while (iPayment < lstPayments.Count)
                {
                    Payment p = lstPayments[iPayment];
                    if (p.Timestamp.Date.CompareTo(dt) <= 0)
                    {
                        queue.Enqueue(p);   // add it to the queue
                        running30 += (p.Amount - p.Fee);
                        iPayment++;
                    }
                    else
                    {
                        break;
                    }
                }

                // And de-queue anything that's fallen out of the 30 day window
                while (queue.Count > 0 && queue.Peek().Timestamp.Date.CompareTo(dt.AddDays(-30)) < 0)
                {
                    Payment p = queue.Dequeue();
                    running30 -= (p.Amount - p.Fee);
                }

                // OK, we should have our running average.
                if (dt.CompareTo(dtMin30) >= 0)
                {
                    gchRunning30.XVals.Add(dt);
                    gchRunning30.YVals.Add(running30);
                }
            }
        }

        protected void RefreshDonations()
        {
            sqlDSDonations.SelectParameters.Clear();
            sqlDSDonations.SelectParameters.Add("user", String.IsNullOrEmpty(txtDonationUser.Text) ? "%" : txtDonationUser.Text);

            List<string> l = new List<string>();
            foreach (ListItem li in ckTransactionTypes.Items)
                if (li.Selected)
                    l.Add(li.Value);

            sqlDSDonations.SelectCommand = String.Format(CultureInfo.InvariantCulture, szDonationsTemplate, (l.Count == 0) ? string.Empty : String.Format(CultureInfo.InvariantCulture, " AND TransactionType IN ({0}) ", String.Join(",", l.ToArray())));
            gvDonations.DataBind();

            using (IDataReader idr = (IDataReader)sqlDSTotalPayments.Select(DataSourceSelectArguments.Empty))
            {
                IEnumerable<YearlyPayments> rgyp = YearlyPayments.PaymentsByYearAndMonth(idr);
                YearlyPayments.ToTable(plcPayments, rgyp);
            }
        }

        protected void btnFindDonations_Click(object sender, EventArgs e)
        {
            RefreshDonations();
        }

        protected void btnComputeStats_Click(object sender, EventArgs e)
        {
            IEnumerable<Payment> lst = Payment.AllRecords();
            foreach (Payment p in lst)
            {
                if (String.IsNullOrEmpty(p.TransactionID) || String.IsNullOrEmpty(p.TransactionNotes))
                    continue;
                System.Collections.Specialized.NameValueCollection nvc = HttpUtility.ParseQueryString(p.TransactionNotes);
                p.Fee = Math.Abs(Convert.ToDecimal(nvc["mc_fee"], CultureInfo.InvariantCulture));
                if (p.Type == Payment.TransactionType.Payment || p.Type == Payment.TransactionType.Refund)
                    p.Commit();
            }
            RefreshDonations();
            gvDonations.DataBind();
        }

        protected void gvDonations_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e != null && e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView drv = (DataRowView)e.Row.DataItem;
                Label l = (Label)e.Row.FindControl("lblTxNotes");
                Label lTransactionType = (Label)e.Row.FindControl("lblTransactionType");
                lTransactionType.Text = ((Payment.TransactionType)Convert.ToInt32(drv.Row["TransactionType"], CultureInfo.InvariantCulture)).ToString();
                PlaceHolder plc = (PlaceHolder)e.Row.FindControl("plcDecoded");
                l.Text = (string)drv.Row["TransactionData"];

                System.Collections.Specialized.NameValueCollection nvc = HttpUtility.ParseQueryString(l.Text);
                Table t = new Table();
                plc.Controls.Add(t);
                foreach (string szKey in nvc.AllKeys)
                {
                    TableRow tr = new TableRow();
                    t.Rows.Add(tr);

                    TableCell tc = new TableCell();
                    tr.Cells.Add(tc);
                    tc.Text = HttpUtility.HtmlEncode(szKey);
                    tc.Style["font-weight"] = "bold";

                    tc = new TableCell();
                    tr.Cells.Add(tc);
                    tc.Text = HttpUtility.HtmlEncode(nvc[szKey]);
                }
            }
        }

        protected void btnResetGratuities_Click(object sender, EventArgs e)
        {
            EarnedGratuity.UpdateEarnedGratuities(txtDonationUser.Text, ckResetGratuityReminders.Checked);
        }

        protected void btnEnterTestTransaction_Click(object sender, EventArgs e)
        {
            Page.Validate("valTestTransaction");
            if (Page.IsValid)
            {
                Payment.TransactionType tt = (Payment.TransactionType)Convert.ToInt32(cmbTestTransactionType.SelectedValue, CultureInfo.InvariantCulture);
                decimal amount = decTestTransactionAmount.Value;
                switch (tt)
                {
                    case Payment.TransactionType.Payment:
                        amount = Math.Abs(amount);
                        break;
                    case Payment.TransactionType.Refund:
                        amount = -1 * Math.Abs(amount);
                        break;
                    default:
                        break;
                }
                Payment p = new Payment(dateTestTransaction.Date, txtTestTransactionUsername.Text, amount, decTestTransactionFee.Value, tt, txtTestTransactionNotes.Text, "Manual Entry", string.Empty);
                p.Commit();
                EarnedGratuity.UpdateEarnedGratuities(txtTestTransactionUsername.Text, true);
                txtTestTransactionUsername.Text = txtTestTransactionNotes.Text = string.Empty;
                decTestTransactionAmount.Value = decTestTransactionFee.Value = 0;
                RefreshDonations();
            }
        }

        protected void gvDonations_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            if (e != null)
            {
                gvDonations.PageIndex = e.NewPageIndex;
                RefreshDonations();
            }
        }
    }
}