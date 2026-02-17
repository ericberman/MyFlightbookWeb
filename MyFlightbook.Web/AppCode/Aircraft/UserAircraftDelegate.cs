using Google.Protobuf.WellKnownTypes;
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
    public class UserAircraftDelegate : IUserAircraftDelegate
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

        private const string szCacheKey = "userAircraftKey";
        private const string szCachedDictionary = "userAircraftDictionaryKey";

        public IList<Aircraft> GetCachedAircraftForUser(string szUser)
        {
            return String.IsNullOrEmpty(szUser) ? null : (List<Aircraft>)Profile.GetUser(szUser).CachedObject(szCacheKey);
        }

        public void CacheAircraftForUser(string szUser, IList<Aircraft> lst)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));
            Profile.GetUser(szUser).AssociatedData[szCacheKey] = lst;
        }

        public void Invalidate(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));
            Profile pf = Profile.GetUser(szUser);
            pf.AssociatedData.Remove(szCacheKey);
            pf.AssociatedData.Remove(szCachedDictionary);
        }

        public IDictionary<string, Aircraft> GetCachedDictionaryForUser(string szUser)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));

            Profile pf = Profile.GetUser(szUser);
            return pf.AssociatedData.TryGetValue(szCachedDictionary, out object value) ? (IDictionary<string, Aircraft>) value : null;
        }

        public void CacheDictionaryForUser(string szUser, IDictionary<string, Aircraft> dict)
        {
            if (String.IsNullOrEmpty(szUser))
                throw new ArgumentNullException(nameof(szUser));
            Profile pf = Profile.GetUser(szUser);
            pf.AssociatedData[szCachedDictionary] = dict;
        }

        public string CoalescedDeadlinesForAircraft(string szUser, int aircraftID)
        {
            return DeadlineCurrency.CoalescedDeadlinesForAircraft(szUser, aircraftID);
        }
    }
}