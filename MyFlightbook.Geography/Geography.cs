/******************************************************
 * 
 * Copyright (c) 2015-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Geography
{
    /// <summary>
    /// Represents a named point in space.
    /// </summary>
    public interface IFix
    {
        string Code { get; }
        LatLong LatLong { get; }

        double DistanceFromFix(IFix f);
    }

    /// <summary>
    /// Stub class for broadly useful geography.  Right now, it's just doing initialization for CoordinateSharp.
    /// </summary>
    public static class Geography
    {
        public static void Init()
        {
            // CoordinateSharp can be very slow - pegging CPU - due to EagerLoading, which matters for celestial computations that we generally don't care about, so just set the default to NOT do eager load.
            CoordinateSharp.GlobalSettings.Default_EagerLoad = new CoordinateSharp.EagerLoad(false);
        }
    }
}
