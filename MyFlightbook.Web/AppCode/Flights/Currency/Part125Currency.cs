using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2007-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Currency
{
    /// <summary>
    /// 125.287(a) - need a knowledge test every 12 months
    /// </summary>
    public class Part125_287a : FlightCurrency
    {
        public Part125_287a(string szName)
            : base(1, 12, true, szName) { }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            if (cfr.FlightProps.PropertyExistsWithID(CustomPropertyType.KnownProperties.IDProp125287Knowledge))
                AddRecentFlightEvents(cfr.dtFlight, 1);
        }
    }

    /// <summary>
    /// 125.287(b) - need a competency check every 12 months; IPC can count.  Striped by type.  IPC can qualify as well.
    /// </summary>
    public class Part125_287b : FlightCurrency
    {
        public Part125_287b(string szCatClass, string szName)
            : base(1, 12, true, String.Format(CultureInfo.InvariantCulture, "{0} - {1}", szCatClass, szName)) { }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));
            cfr.FlightProps.ForEachEvent((pfe) =>
            {
                if (pfe.PropTypeID == (int)CustomPropertyType.KnownProperties.IDProp125287Competency || pfe.PropTypeID == (int)CustomPropertyType.KnownProperties.IDProp125291IPC)
                    AddRecentFlightEvents(cfr.dtFlight, 1);
            });
        }
    }

    /// <summary>
    /// 125.291(a) - need an IPC every 6 months
    /// </summary>
    public class Part125_291a : FlightCurrency
    {
        public Part125_291a(string szType, string szName)
            : base(1, 6, true, String.Format(CultureInfo.InvariantCulture, "{0} - {1}", szType, szName)) { }

        public override void ExamineFlight(ExaminerFlightRow cfr)
        {
            if (cfr == null)
                throw new ArgumentNullException(nameof(cfr));

            // Only ever applies to airplanes
            if (!CategoryClass.IsAirplane(cfr.idCatClassOverride))
                return;

            cfr.FlightProps.ForEachEvent((pfe) =>
            {
                if (pfe.PropTypeID == (int)CustomPropertyType.KnownProperties.IDProp125291IPC)
                    AddRecentFlightEvents(cfr.dtFlight, 1);
            });
        }
    }
}