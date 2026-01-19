using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2015-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Geography
{

    public interface IPolyRegion
    {
        /// <summary>
        /// Tests if the point is in the region.
        /// </summary>
        /// <param name="ll"></param>
        /// <returns></returns>
        bool ContainsLocation(LatLong ll);

        /// <summary>
        /// The name of the region
        /// </summary>
        string Name { get; }
    }

    public abstract class GeoRegionBase : IPolyRegion
    {
        #region Properties
        private List<LatLong> m_lst;

        /// <summary>
        /// The points that define the boundary of the polygon
        /// </summary>
        public IEnumerable<LatLong> BoundingPolygon
        {
            get { return m_lst; }
            set { m_lst = value == null ? null : new List<LatLong>(value); }
        }

        public virtual string Name { get { return string.Empty; } }
        #endregion

        #region Object Creation
        protected GeoRegionBase()
        {
            BoundingPolygon = null;
        }

        protected GeoRegionBase(IEnumerable<LatLong> Poly) : this()
        {
            BoundingPolygon = Poly;
        }
        #endregion

        /// <summary>
        /// Tests to see if the specified point is contained within this region
        /// Code adapted from https://stackoverflow.com/questions/924171/geo-fencing-point-inside-outside-polygon/7739297#7739297
        /// </summary>
        /// <param name="ll">The lat/lon to test</param>
        /// <returns>True if the point is contained.</returns>
        public virtual bool ContainsLocation(LatLong ll)
        {
            if (ll == null || BoundingPolygon == null || !BoundingPolygon.Any())
                return false;

            int i, j;
            bool fContained = false;
            for (i = 0, j = m_lst.Count - 1; i < m_lst.Count; j = i++)
            {
                if ((((m_lst[i].Latitude <= ll.Latitude) && (ll.Latitude < m_lst[j].Latitude))
                        || ((m_lst[j].Latitude <= ll.Latitude) && (ll.Latitude < m_lst[i].Latitude)))
                        && (ll.Longitude < (m_lst[j].Longitude - m_lst[i].Longitude) * (ll.Latitude - m_lst[i].Latitude)
                            / (m_lst[j].Latitude - m_lst[i].Latitude) + m_lst[i].Longitude))

                    fContained = !fContained;
            }

            return fContained;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder((Name ?? "(unnamed)") + ": ");
            if (BoundingPolygon == null)
                sb.Append("(no polygon)");
            else
                foreach (LatLong ll in BoundingPolygon)
                    sb.AppendFormat(CultureInfo.CurrentCulture, "{0} ", ll.ToAdhocFixString());

            return sb.ToString();
        }
    }

    #region Concrete and Known GeoRegions
    public class GeoRegion : GeoRegionBase
    {
        private readonly string m_name;

        public override string Name { get { return m_name; } }

        public GeoRegion(string name, IEnumerable<LatLong> poly) : base(poly)
        {
            m_name = name;
        }
    }

    public class GeoRegionAfrica : GeoRegionBase
    {
        public override string Name { get { return Properties.Geography.regionAfrica; } }

        public GeoRegionAfrica() : base(new LatLong[] {
                new LatLong(35.9639589732242, -5.51296000479861),
                new LatLong(35.8862723352267, -6.06508590729603),
                new LatLong(27.9650246910836, -13.396328703638),
                new LatLong(20.4185511398316, -19.0220934429556),
                new LatLong(10.2067563913974, -18.1746276284209),
                new LatLong(-35.7974468653211, 18.0727332053251),
                new LatLong(-37.800423975311, 45.5756350423789),
                new LatLong(-15.5812502513301, 64.3241461405685),
                new LatLong(13.839554162838, 55.4034779375179),
                new LatLong(12.0001874490197, 43.9744547803343),
                new LatLong(12.8215166774877, 43.1793499776247),
                new LatLong(14.6748538652459, 41.9514734769258),
                new LatLong(26.4097961350399, 35.0444587248198),
                new LatLong(27.7101288125027, 33.9360619033247),
                new LatLong(28.706074147917, 32.9735482949426),
                new LatLong(29.8946920096056, 32.521020534956),
                new LatLong(30.2264448230464, 32.580571137488),
                new LatLong(30.8410092699496, 32.3510582691335),
                new LatLong(31.1126376615812, 32.6174830928286),
                new LatLong(32.759257166309, 32.3129919377845),
                new LatLong(34.8296570592962, 12.3428920775784),
                new LatLong(37.5316071447726, 11.3845983138082),
                new LatLong(37.376662630504, 2.83389562925519),
                new LatLong(35.9639589732242, -5.51296000479861) })
        { }
    }

    public class GeoRegionAsia : GeoRegionBase
    {
        public override string Name { get { return Properties.Geography.regionAsia; } }

        public GeoRegionAsia() : base(new LatLong[] {
        new LatLong(41.2509219004154, 29.1327664900908),
                new LatLong(40.9734141742538, 28.9607800270263),
                new LatLong(40.7139062416896, 27.5095406385034),
                new LatLong(40.42723455786, 26.7220902256957),
                new LatLong(39.964641808291, 26.0892326658118),
                new LatLong(39.033213604809, 25.6738236896689),
                new LatLong(37.8919707829971, 25.7954007357716),
                new LatLong(35.9039485927882, 27.6153442151746),
                new LatLong(31.3042674355417, 32.3541120404423),
                new LatLong(30.5492386422801, 32.3082736215819),
                new LatLong(30.2355781570682, 32.4964367721185),
                new LatLong(29.9166389861311, 32.5755407217273),
                new LatLong(28.5938049291395, 33.0323536878531),
                new LatLong(27.6123886365306, 34.2353541632286),
                new LatLong(13.7388068586605, 42.5055435095544),
                new LatLong(12.6139998625938, 43.3696062227207),
                new LatLong(11.8309154195224, 46.7217748151204),
                new LatLong(12.1090910402245, 51.3426021907803),
                new LatLong(0.0591396777793757, 83.4779441979723),
                new LatLong(-13.0296954507373, 124.711137774139),
                new LatLong(-9.59108097354274, 133.793246439443),
                new LatLong(-9.80501498722165, 142.210469339179),
                new LatLong(-13.7299006546679, 163.709103861709),
                new LatLong(53.3520024026076, 172.161152065312),
                new LatLong(64.4717875927483, 188.593522),  // avoid wrapping issues - go more than 180 degrees east.
                new LatLong(65.7054468886283, 190.9696328),
                new LatLong(65.8174720832036, 191.0716377),
                new LatLong(75.3985684444915, 190.6717555),
                new LatLong(81.6089771036393, 84.8429070695296),
                new LatLong(70.5369188387754, 63.6472730500082),
                new LatLong(66, 69),
                new LatLong(58.5, 59.3),
                new LatLong(46.5, 50),
                new LatLong(46.6, 48.3),
                new LatLong(45.67, 46.1),
                new LatLong(45.6, 42),
                new LatLong(43.2173863336323, 42.4728025502204),
                new LatLong(43.5930962200247, 40.2995769590982),
                new LatLong(44.8732752454212, 38.1208700502164),
                new LatLong(41.2509219004154, 29.1327664900908)})
        { }
    }

    public class GeoRegionAustralia : GeoRegionBase
    {
        public override string Name { get { return Properties.Geography.regionAustralia; } }

        public GeoRegionAustralia() : base(new LatLong[] {
             new LatLong(-9.84747798837819, 142.164903744794),
                new LatLong(-10.7634822999224, 128.242927897764),
                new LatLong(-21.3883493589849, 108.9805830486),
                new LatLong(-38.7356886909232, 109.98120359961),
                new LatLong(-48.9146246440036, 154.581951859103),
                new LatLong(-24.358190937035, 154.968822697766),
                new LatLong(-9.84747798837819, 142.164903744794)})
        { }
    }

    public class GeoRegionEurope : GeoRegionBase
    {
        public override string Name { get { return Properties.Geography.regionEurope; } }

        public GeoRegionEurope() : base(new LatLong[] {
        new LatLong(80.8558924725111, -104.623029917689),
                new LatLong(74.8912746930429, -70.4480130659312),
                new LatLong(66.2374587178815, -56.7443463303166),
                new LatLong(58.3120543795464, -43.7864892029896),
                new LatLong(35.7666986983938, -9.9144334810344),
                new LatLong(35.9206725280298, -5.76605448262612),
                new LatLong(35.987725826605, -5.05260546746426),
                new LatLong(37.5945403737721, 11.4752868911118),
                new LatLong(34.1019556187922, 15.6149082883608),
                new LatLong(34.1142583862664, 23.5656801553353),
                new LatLong(35.1096880928465, 27.6335921301456),
                new LatLong(36.3328033369107, 27.1146010222214),
                new LatLong(37.8454869034392, 25.6093154686312),
                new LatLong(39.3405977850254, 25.6114546037733),
                new LatLong(40.0120089426856, 26.1663812337314),
                new LatLong(40.1603459024305, 26.41090699303),
                new LatLong(40.4489888069553, 26.8294078045092),
                new LatLong(40.9186911803264, 28.9170672550352),
                new LatLong(41.3139457691132, 29.1593413484323),
                new LatLong(43.3673702736188, 39.9994006379031),
                new LatLong(43.593075710196, 40.1067940778491),
                new LatLong(43.2537312885191, 42.5610739750239),
                new LatLong(42.7980685552358, 43.749946837025),
                new LatLong(42.704228077724, 44.4806056061958),
                new LatLong(42.3928227226911, 45.9496621358904),
                new LatLong(42.3070794920955, 45.5542477719412),
                new LatLong(41.8179734725377, 46.8006432792265),
                new LatLong(41.3396199556304, 47.2374245910874),
                new LatLong(41.2046925307547, 47.8216411217678),
                new LatLong(41.7607787135203, 48.5935325616133),
                new LatLong(46.3629939794767, 49.1797477747411),
                new LatLong(46.7265063862009, 48.5500843440706),
                new LatLong(47.8248608945324, 48.2980765207448),
                new LatLong(48.4538889039536, 46.54370124941),
                new LatLong(50.4912668686513, 47.5135479310345),
                new LatLong(49.8827419539125, 48.3332220648206),
                new LatLong(50.0741453268273, 48.8794781443255),
                new LatLong(50.5877839839358, 48.6899694551153),
                new LatLong(51.8185402882364, 50.8209198978647),
                new LatLong(68.3827888074154, 68.435652453113),
                new LatLong(69.9014945798418, 64.210230386991),
                new LatLong(77.3159309370392, 71.0856193510771),
                new LatLong(87.0110973494483, 78.3257990090781),
                new LatLong(80.8558924725111, -104.623029917689)})
        { }
    }

    public class GeoRegionNorthAmerica : GeoRegionBase
    {
        public override string Name { get { return Properties.Geography.regionNorthAmerica; } }

        public GeoRegionNorthAmerica() : base(new LatLong[] {
            new LatLong(16.338092279951, -157.646716565888),
                new LatLong(4.82956650806859, -80.282609015191),
                new LatLong(7.22181017365141, -77.880570089996),
                new LatLong(7.44776581427614, -77.786236266851),
                new LatLong(7.48638485079662, -77.703585326028),
                new LatLong(7.63529055470985, -77.703811265211),
                new LatLong(7.512716626303, -77.582810769788),
                new LatLong(7.76563668378025, -77.38333859188),
                new LatLong(7.8552333427672, -77.342562198241),
                new LatLong(7.95384883044459, -77.166922109416),
                new LatLong(8.51291616597859, -77.446398321394),
                new LatLong(8.68357371710449, -77.365255574793),
                new LatLong(11, -77.317269403893),
                new LatLong(17, -63),
                new LatLong(50.858620027233, -47),
                new LatLong(76.6833454236019, -75.722968754264),
                new LatLong(84.9536812968452, -54.132134583663),
                new LatLong(70.7433570556606, -167.95609566936),
                new LatLong(67.176093128249, -168.177707220915),
                new LatLong(65.8241496563039, -168.899825316639),
                new LatLong(63.7569457862177, -172.043426121483),
                /* new LatLong(52.9504446970992, 171.711362533652), */
                new LatLong(52.9504446970992, -188.288637466348),   // capture the aleutians, but use < -180 to avoid wrap-around issues.
                new LatLong(16.338092279951, -157.646716565888)
            })
        { }
    }

    public class GeoRegionSouthAmerica : GeoRegionBase
    {
        public override string Name { get { return Properties.Geography.regionSouthAmerica; } }

        public GeoRegionSouthAmerica() : base(new LatLong[] {
         new LatLong(8.6864747989427, -77.3578705512968),
                new LatLong(8.50861348396742, -77.4529541443921),
                new LatLong(8.26572875703845, -77.298368698503),
                new LatLong(7.92259705503796, -77.1645994759198),
                new LatLong(7.22322226621087, -77.8979236540419),
                new LatLong(1.71661207255935, -93.6043845257161),
                new LatLong(-55.352876127547, -98.716136930023),
                new LatLong(-58.6325605659904, -53.0064080186952),
                new LatLong(-4.11197623825627, -29.3523164812539),
                new LatLong(11.8941952294329, -59.764648533519),
                new LatLong(13.3535447099878, -71.3131201675003),
                new LatLong(11.5647049699963, -76.551881316024),
                new LatLong(8.6864747989427, -77.3578705512968)})
        { }
    }

    public class GeoRegionAntarctica : IPolyRegion
    {
        public string Name { get { return Properties.Geography.regionAntarctica; } }

        public bool ContainsLocation(LatLong ll) { return ll != null && ll.Latitude < -60; }
    }

    public static class KnownGeoRegions
    {
        private static IEnumerable<IPolyRegion> _AllContinents;

        public static IEnumerable<IPolyRegion> AllContinents
        {
            get
            {
                if (_AllContinents == null)
                    _AllContinents = new IPolyRegion[]
                        {
                            new GeoRegionAntarctica(),
                            new GeoRegionAfrica(),
                            new GeoRegionAsia(),
                            new GeoRegionAustralia(),
                            new GeoRegionEurope(),
                            new GeoRegionNorthAmerica(),
                            new GeoRegionSouthAmerica()
                        };
                return _AllContinents;
            }
        }
    }
    #endregion
}
