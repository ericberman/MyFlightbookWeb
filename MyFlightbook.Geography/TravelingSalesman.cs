using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2010-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Geography
{
    /// <summary>
    /// Solution to TSP for a collection of airports, code adapted from https://stackoverflow.com/questions/2927469/traveling-salesman-problem-2-opt-algorithm-c-sharp-implementation
    /// </summary>
    public static class TravelingSalesman
    {
        private class Stop
        {
            #region Constructors
            public Stop(IFix f)
            {
                Fix = f;
            }
            #endregion

            #region Properties
            public Stop Next { get; set; }

            public IFix Fix { get; set; }
            #endregion

            public Stop Clone()
            {
                return new Stop(Fix);
            }

            public static double Distance(Stop first, Stop other)
            {
                return first.Fix.DistanceFromFix(other.Fix);
            }

            //list of nodes, including this one, that we can get to
            public IEnumerable<Stop> CanGetTo()
            {
                var current = this;
                while (true)
                {
                    yield return current;
                    current = current.Next;
                    if (current == this) break;
                }
            }

            public override bool Equals(object obj)
            {
                if (obj == null || !(obj is Stop stop))
                    return false;

                return Fix.Code.CompareCurrentCultureIgnoreCase(stop.Fix.Code) == 0;
            }

            public override int GetHashCode()
            {
                return Fix.GetHashCode();
            }

            public override string ToString()
            {
                return Fix.ToString();
            }
        }

        private class Tour
        {
            public Tour(IEnumerable<Stop> stops)
            {
                Anchor = stops.First();
            }

            //the set of tours we can make with 2-opt out of this one
            public IEnumerable<Tour> GenerateMutations()
            {
                for (Stop stop = Anchor; stop.Next != Anchor; stop = stop.Next)
                {
                    //skip the next one, since you can't swap with that
                    Stop current = stop.Next.Next;
                    while (current != Anchor)
                    {
                        yield return CloneWithSwap(stop.Fix, current.Fix);
                        current = current.Next;
                    }
                }
            }

            public Stop Anchor { get; set; }

            public Tour CloneWithSwap(IFix firstFix, IFix secondFix)
            {
                Stop firstFrom = null, secondFrom = null;
                var stops = UnconnectedClones();
                stops.Connect(true);

                foreach (Stop stop in stops)
                {
                    if (stop.Fix == firstFix) firstFrom = stop;

                    if (stop.Fix == secondFix) secondFrom = stop;
                }

                //the swap part
                var firstTo = firstFrom.Next;
                var secondTo = secondFrom.Next;

                //reverse all of the links between the swaps
                firstTo.CanGetTo()
                       .TakeWhile(stop => stop != secondTo)
                       .Reverse()
                       .Connect(false);

                firstTo.Next = secondTo;
                firstFrom.Next = secondFrom;

                var tour = new Tour(stops);
                return tour;
            }


            public IList<Stop> UnconnectedClones()
            {
                return Cycle().Select(stop => stop.Clone()).ToList();
            }


            public double Cost()
            {
                return Cycle().Aggregate(
                    0.0,
                    (sum, stop) =>
                    sum + Stop.Distance(stop, stop.Next));
            }


            private IEnumerable<Stop> Cycle()
            {
                return Anchor.CanGetTo();
            }


            public override string ToString()
            {
                string path = String.Join(
                    "->",
                    Cycle().Select(stop => stop.ToString()).ToArray());
                return String.Format(CultureInfo.CurrentCulture, "Cost: {0}, Path:{1}", Cost(), path);
            }

            public IEnumerable<IFix> Path()
            {
                return Cycle().Select(stop => stop.Fix);
            }
        }

        public static IEnumerable<IFix> ShortestPath(IEnumerable<IFix> rgFixes)
        {
            if (rgFixes == null)
                return Array.Empty<IFix>();
            if (rgFixes.Count() <= 2)
                return rgFixes;

            var lstStops = new List<Stop>();
            foreach (IFix ap in rgFixes)
                lstStops.Add(new Stop(ap));

            lstStops.NearestNeighbors().Connect(true);

            Tour startingTour = new Tour(lstStops);

            //the actual algorithm
            while (true)
            {
                var newTour = startingTour.GenerateMutations()
                                          .MinBy(tour => tour.Cost());
                if (newTour.Cost() < startingTour.Cost()) startingTour = newTour;
                else break;
            }

            // Success?
            return startingTour.Path();
        }

        //take an ordered list of nodes and set their next properties
        private static void Connect(this IEnumerable<Stop> stops, bool loop)
        {
            Stop prev = null, first = null;
            foreach (var stop in stops)
            {
                if (first == null) first = stop;
                if (prev != null) prev.Next = stop;
                prev = stop;
            }

            if (loop)
            {
                prev.Next = first;
            }
        }


        //T with the smallest func(T)
        private static T MinBy<T, TComparable>(
            this IEnumerable<T> xs,
            Func<T, TComparable> func)
            where TComparable : IComparable<TComparable>
        {
            return xs.DefaultIfEmpty().Aggregate(
                (maxSoFar, elem) =>
                func(elem).CompareTo(func(maxSoFar)) > 0 ? maxSoFar : elem);
        }


        //return an ordered nearest neighbor set
        private static IEnumerable<Stop> NearestNeighbors(this IEnumerable<Stop> stops)
        {
            var stopsLeft = stops.ToList();
            for (var stop = stopsLeft.First();
                 stop != null;
                 stop = stopsLeft.MinBy(s => Stop.Distance(stop, s)))
            {
                stopsLeft.Remove(stop);
                yield return stop;
            }
        }
    }
}
