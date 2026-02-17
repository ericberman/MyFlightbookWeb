using MyFlightbook.Currency;
using System;
using System.Collections.Generic;

/******************************************************
 * 
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.AircraftSupport
{
    public class UserAircraftChangeUtility : IUserAircraftChanged
    {
        public void AircraftDeleted(string szUser, int idAircraft)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            // Delete any deadlines associated with this aircraft
            foreach (DeadlineCurrency dc in DeadlineCurrency.DeadlinesForUser(szUser, idAircraft))
                dc.FDelete();

            // And delete any custom currencies associated with the aircraft
            foreach (CustomCurrency cc in CustomCurrency.CustomCurrenciesForUser(szUser))
            {
                List<int> ids = new List<int>(cc.AircraftRestriction);

                if (ids.Contains(idAircraft))
                {
                    ids.Remove(idAircraft);
                    cc.AircraftRestriction = ids;
                    cc.FCommit();
                }
            }
        }

        public void AircraftMigrated(string szUser, int idOldAircraft, int idNewAircraft)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            LogbookEntryBase.UpdateFlightAircraftForUser(szUser, idOldAircraft, idNewAircraft);

            // Migrate any custom currencies associated with the aircraft
            foreach (CustomCurrency cc in CustomCurrency.CustomCurrenciesForUser(szUser))
            {
                List<int> lst = new List<int>(cc.AircraftRestriction);

                for (int i = 0; i < lst.Count; i++)
                {
                    if (lst[i] == idOldAircraft)
                    {
                        lst[i] = idNewAircraft;
                        cc.AircraftRestriction = lst;
                        cc.FCommit();
                        break;
                    }
                }
            }

            // And migrate any deadlines associated with the aircraft
            foreach (DeadlineCurrency dc in DeadlineCurrency.DeadlinesForUser(szUser, idOldAircraft))
            {
                dc.AircraftID = idNewAircraft;
                dc.FCommit();
            }

        }
    }
}