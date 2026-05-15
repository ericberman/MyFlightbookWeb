using System;
using System.Collections.Generic;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2013-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Payments
{
    /// <summary>
    /// Provides an admin report about the number of users who have made 1 payment, 2 payments, etc.
    /// </summary>
    public class UserTransactionSummary
    {
        public int NumPayments { get; set; }
        public int NumUsers { get; set; }

        public static IEnumerable<UserTransactionSummary> Refresh()
        {
            List<UserTransactionSummary> lst = new List<UserTransactionSummary>();
            DBHelper dbh = new DBHelper(@"SELECT 
    numpayments AS 'Number of payments',
    COUNT(numpayments) AS 'Number of Users'
FROM
    (SELECT 
        COUNT(username) AS numpayments
    FROM
        payments
    WHERE
        TransactionType = 0
    GROUP BY username) p
GROUP BY p.numpayments
ORDER BY p.numpayments");

            dbh.ReadRows((comm) => { },
                (dr) => { lst.Add(new UserTransactionSummary() { NumPayments = Convert.ToInt32(dr["Number of payments"], CultureInfo.InvariantCulture), NumUsers = Convert.ToInt32(dr["Number of Users"], CultureInfo.InvariantCulture) }); });
            return lst;
        }
    }

    /// <summary>
    /// Provides an admin report about the number of users who have donated $10, $15, ...
    /// </summary>
    public class AmountTransactionSummary
    {
        public int NumTransactions { get; set; }
        public double TransactionValue { get; set; }

        public static IEnumerable<AmountTransactionSummary> Refresh()
        {
            List<AmountTransactionSummary> lst = new List<AmountTransactionSummary>();
            DBHelper dbh = new DBHelper(@"SELECT 
    COUNT(Amount) AS 'Number of transactions', Amount
FROM
    payments
WHERE
    TransactionType = 0
GROUP BY amount
ORDER BY Amount ASC");

            dbh.ReadRows((comm) => { },
                (dr) => { lst.Add(new AmountTransactionSummary() { NumTransactions = Convert.ToInt32(dr["Number of transactions"], CultureInfo.InvariantCulture), TransactionValue = Convert.ToDouble(dr["Amount"], CultureInfo.InvariantCulture) }); });
            return lst;
        }
    }


    public class YearlyPayments : IComparable
    {
        public int Year { get; set; }
        public IList<PeriodPaymentStat> MonthlyPayments { get; private set; }
        public PeriodPaymentStat AnnualPayment { get; set; }

        public YearlyPayments(int year)
        {
            Year = year;
            PeriodPaymentStat[] rgPayments = new PeriodPaymentStat[12];
            MonthlyPayments = new List<PeriodPaymentStat>(rgPayments);
            for (int i = 0; i < 12; i++)
                MonthlyPayments[i] = new PeriodPaymentStat();
            AnnualPayment = new PeriodPaymentStat();
        }

        public static IEnumerable<YearlyPayments> PaymentsByYearAndMonth()
        {
            Dictionary<int, YearlyPayments> d = new Dictionary<int, YearlyPayments>();

            DBHelper dbh = new DBHelper(@"SELECT 
    YEAR(Date) AS Year,
    MONTH(Date) AS Month,
    SUM(Amount) AS Gross,
    SUM(Fee) AS Fee,
    SUM(Amount - Fee) AS Net,
    SUM(IF(TransactionID = '', 0, Amount - FEE)) AS NetPaypal
FROM
    Payments
WHERE
    TransactionType IN (0 , 1)
GROUP BY Year , Month
ORDER BY Year ASC , Month ASC;");

            dbh.ReadRows((comm) => { },
                (dr) =>
                {
                    int year = Convert.ToInt32(dr["Year"], CultureInfo.InvariantCulture);
                    int month = Convert.ToInt32(dr["Month"], CultureInfo.InvariantCulture);
                    if (month <= 0 || month > 12)
                        throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "Invalid month in donations: {0}", month));
                    string MonthPeriod = String.Format(CultureInfo.InvariantCulture, "{0:0000}-{1:00}", year, month);
                    if (!d.ContainsKey(year))
                        d[year] = new YearlyPayments(year);
                    d[year].MonthlyPayments[month - 1] = new PeriodPaymentStat()
                    {
                        Net = Convert.ToDouble(dr["NetPaypal"], CultureInfo.InvariantCulture),
                        Gross = Convert.ToDouble(dr["Gross"], CultureInfo.InvariantCulture),
                        Fee = Convert.ToDouble(dr["Fee"], CultureInfo.InvariantCulture)
                    };
                });

            List<YearlyPayments> lst = new List<YearlyPayments>(d.Values);
            lst.Sort();

            PeriodPaymentStat[] highWaters = new PeriodPaymentStat[13];


            // Now go through and compute stats.
            for (int i = 0; i < lst.Count; i++)
            {
                YearlyPayments thisYear = lst[i];

                // First year is special because we can't compare to previous year.
                if (i == 0)
                {

                    foreach (PeriodPaymentStat stat in thisYear.MonthlyPayments)
                    {
                        thisYear.AnnualPayment.Fee += stat.Fee;
                        thisYear.AnnualPayment.Net += stat.Net;
                        thisYear.AnnualPayment.Gross += stat.Gross;
                    }
                    continue;
                }

                if (lst[i - 1].Year != lst[i].Year - 1) // ignore non-contiguous years (shouldn't happen)
                    continue;

                double annual = 0;
                YearlyPayments lastYear = lst[i - 1];

                for (int j = 0; j < 12; j++)
                {
                    PeriodPaymentStat lastYearMonth = lastYear.MonthlyPayments[j];
                    PeriodPaymentStat thisYearMonth = thisYear.MonthlyPayments[j];
                    PeriodPaymentStat highWater = highWaters[j];
                    if (thisYearMonth.Net > (highWater?.Net ?? 0))
                        highWaters[j] = thisYearMonth;

                    annual += thisYearMonth.Net;
                    thisYearMonth.YOYGross = thisYearMonth.Net - lastYearMonth.Net;
                    thisYearMonth.YOYPercent = (lastYearMonth.Net == 0) ? 0 : thisYearMonth.YOYGross / lastYearMonth.Net;
                }

                thisYear.AnnualPayment.Net = annual;
                thisYear.AnnualPayment.YOYGross = thisYear.AnnualPayment.Net - lastYear.AnnualPayment.Net;
                thisYear.AnnualPayment.YOYPercent = (lastYear.AnnualPayment.Net == 0) ? 0 : thisYear.AnnualPayment.YOYGross / lastYear.AnnualPayment.Net;

                if (annual > (highWaters[12]?.Net ?? 0))
                    highWaters[12] = thisYear.AnnualPayment;
            }

            foreach (PeriodPaymentStat pps in highWaters)
                pps.IsHighWater = true;

            return lst;
        }

        #region IComparable
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            YearlyPayments yp = obj as YearlyPayments;
            return Year.CompareTo(yp.Year);
        }
        public override bool Equals(object obj)
        {
            return obj is YearlyPayments payments &&
                   Year == payments.Year &&
                   EqualityComparer<IList<PeriodPaymentStat>>.Default.Equals(MonthlyPayments, payments.MonthlyPayments) &&
                   EqualityComparer<PeriodPaymentStat>.Default.Equals(AnnualPayment, payments.AnnualPayment);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = -296683479;
                hashCode = hashCode * -1521134295 + Year.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<IList<PeriodPaymentStat>>.Default.GetHashCode(MonthlyPayments);
                hashCode = hashCode * -1521134295 + EqualityComparer<PeriodPaymentStat>.Default.GetHashCode(AnnualPayment);
                return hashCode;
            }
        }

        public static bool operator ==(YearlyPayments left, YearlyPayments right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(YearlyPayments left, YearlyPayments right)
        {
            return !(left == right);
        }

        public static bool operator <(YearlyPayments left, YearlyPayments right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(YearlyPayments left, YearlyPayments right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(YearlyPayments left, YearlyPayments right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(YearlyPayments left, YearlyPayments right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion
    }

    public class PeriodPaymentStat
    {
        public double Net { get; set; }
        public double Gross { get; set; }
        public double Fee { get; set; }
        public double YOYPercent { get; set; }
        public double YOYGross { get; set; }
        public bool IsHighWater { get; set; }
    }
}
