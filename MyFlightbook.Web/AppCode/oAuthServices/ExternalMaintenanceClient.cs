using DotNetOpenAuth.OAuth2;
using MyFlightbook.AircraftSupport.Maintenance;
using MyFlightbook.OAuth.MyTailLog;
using MyFlightbook.OAuth.TachTime;
using System;

/******************************************************
 * 
 * Copyright (c) 2019-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.OAuth.Maintenance
{
    public static class ExternalMaintenanceSource
    {
        public static IPushHighWater PushHighWaterForUser(this ExternalMaintenanceSourceID id, string userName)
        {
            if (String.IsNullOrEmpty(userName))
                throw new ArgumentNullException(nameof(userName));

            Profile pf = Profile.GetUser(userName);
            string tokenKey = id.TokenPreferenceKey();
            if (!pf.PreferenceExists(tokenKey))
                throw new UnauthorizedAccessException($"User {userName} is not configured for service ID {id.SourceName()}");
            AuthorizationState authorizationState = pf.GetPreferenceForKey<AuthorizationState>(tokenKey);
            switch (id)
            {
                case ExternalMaintenanceSourceID.Unknown:
                    throw new InvalidOperationException("Unknown is not a valid target for pushing an ID");
                case ExternalMaintenanceSourceID.TachTime:
                    return new TachTimeClient(authorizationState, Branding.CurrentBrand.HostName);
                case ExternalMaintenanceSourceID.MyTailLog:
                    return new MyTailLogClient(authorizationState);
                default:
                    throw new InvalidOperationException($"No client known for service {id.SourceName()}");
            }
        }
    }
}