using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using MySql.Data.MySqlClient;

/******************************************************
 * 
 * Copyright (c) 2013-2016 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Image
{
    /// <summary>
    /// Summary description for Video
    /// </summary>
    [Serializable]
    public class VideoRef
    {
        public const int idVideoUnknown = -1;

        public const int defWidth = 640;
        public const int defHeight = 360;

        public enum VideoSource { Unknown, YouTube, Vimeo };

        private string m_vidURL;

        public void InitFromSerializedString(int idFlight, string szSerialized)
        {
            if (szSerialized == null)
                throw new ArgumentNullException("szSerialized");
            string[] rgParams = szSerialized.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (rgParams.Length < 2 || rgParams.Length > 3)
                throw new MyFlightbookException(String.Format(CultureInfo.InvariantCulture, "Invalid serialization for video: {0}", szSerialized));

            FlightID = idFlight;
            ID = Convert.ToInt32(rgParams[0], CultureInfo.InvariantCulture);
            VideoReference = rgParams[1];
            Comment = (rgParams.Length == 3) ? rgParams[2] : string.Empty;
            if (!IsValid)
                throw new MyFlightbookException(ErrorString);
        }

        #region Constructors
        public VideoRef()
        {
            ID = idVideoUnknown;
            FlightID = LogbookEntry.idFlightNone;
            VideoReference = Comment = string.Empty;
            Source = VideoSource.Unknown;

            ErrorString = string.Empty;
        }

        protected VideoRef(MySqlDataReader dr) : this()
        {
            InitFromDataReader(dr);
        }

        public VideoRef(int idFlight, string szVidRef, string szComment) : this()
        {
            FlightID = idFlight;
            VideoReference = szVidRef;
            Comment = szComment;
        }
        #endregion

        #region Properties
        /// <summary>
        /// The ID for this video
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The flight with which this video is associated
        /// </summary>
        public int FlightID { get; set; }

        /// <summary>
        /// The data (URL or HTML) for the video
        /// </summary>
        public string VideoReference
        {
            get { return m_vidURL; }
            set { m_vidURL = value; Source = GuessSource(); }
        }

        public string DisplayString
        {
            get { return String.IsNullOrEmpty(Comment) ? VideoReference : Comment; }
        }

        /// <summary>
        /// Who is the host (source) of the video?  Currently only Youtube is supported.
        /// </summary>
        public VideoSource Source { get; set; }

        /// <summary>
        /// Comment for the video
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Last error
        /// </summary>
        public string ErrorString { get; set; }
        #endregion

        public bool IsValid
        {
            get
            {
                ErrorString = string.Empty;
                try
                {
                    if (String.IsNullOrEmpty(VideoReference))
                        throw new InvalidOperationException(Resources.LocalizedText.videoErrNoURL);

                    switch (Source)
                    {
                        case VideoSource.YouTube:
                            if (!IsValidYoutubeURL())
                                throw new InvalidOperationException(Resources.LocalizedText.videoErrURLNotParseable);
                            break;
                        case VideoSource.Vimeo:
                            break;
                        case VideoSource.Unknown:
                        default:
                            throw new InvalidOperationException(Resources.LocalizedText.videoErrUnsupportedFormat);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    ErrorString = ex.Message;
                }
                return ErrorString.Length == 0;
            }
        }

        #region DB
        private VideoRef InitFromDataReader(MySqlDataReader dr)
        {
            ID = Convert.ToInt32(dr["idFlightVideos"], CultureInfo.InvariantCulture);
            FlightID = Convert.ToInt32(dr["idFlight"], CultureInfo.InvariantCulture);
            VideoReference = dr["vidRef"].ToString();
            Comment = dr["Comment"].ToString();
            return this;
        }

        public void Commit()
        {
            // never update - only insert.
            if (ID != idVideoUnknown)
                return;

            if (FlightID == LogbookEntry.idFlightNone)
                throw new MyFlightbookException("Video: Can't commit - No flight specified");

            if (IsValid)
            {
                DBHelper dbh = new DBHelper("INSERT INTO flightvideos SET idFlight=?flightid, vidRef=?vr, Comment=?c");
                dbh.DoNonQuery((comm) =>
                    {
                        comm.Parameters.AddWithValue("flightid", FlightID);
                        comm.Parameters.AddWithValue("vr", VideoReference);
                        comm.Parameters.AddWithValue("c", Comment.LimitTo(100));
                    });
                this.ID = dbh.LastInsertedRowId;
            }
            else
                throw new MyFlightbookException(ErrorString);
        }

        public void Delete()
        {
            DBHelper dbh = new DBHelper("DELETE FROM flightvideos WHERE idFlightVideos=?idvid");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("idvid", ID); });
        }
        #endregion

        #region Parsing/display

        private const string vidTemplateVimeo = "<iframe src=\"https://player.vimeo.com/video/{0}?title=0&amp;byline=0&amp;portrait=0&amp;badge=0\" width=\"{1}\" height=\"{2}\" frameborder=\"0\" webkitallowfullscreen mozallowfullscreen allowfullscreen></iframe>";
        private const string vidTemplateYouTube = "<iframe src=\"https://www.youtube.com/embed/{0}\" width=\"{1}\" height=\"{2}\" frameborder=\"0\" webkitallowfullscreen mozallowfullscreen allowfullscreen></iframe>";


        // Adapted from http://linuxpanda.wordpress.com/2013/07/24/ultimate-best-regex-pattern-to-get-grab-parse-youtube-video-id-from-any-youtube-link-url/
        private const string szRegExpMatchYouTube = "^(?:http|https)?(?:://)?(?:www\\.)?(?:youtu\\.be/|youtube\\.com(?:/embed/|/v/|/watch?v=|/ytscreeningroom?v=|/feeds/api/videos/|/user\\S*[^\\w\\-\\s]|\\S*[^\\w\\-\\s]))([\\w\\-]{11})[a-z0-9;:@?&%=+/\\$_.-]*";

        // Adapted from http://stackoverflow.com/questions/10488943/easy-way-to-get-vimeo-id-from-a-vimeo-url
        private const string szRegExpMatchVimeo = "^(?:http|https)(?:://)?(?:www\\.|player\\.)?vimeo.com/(.*)";

        /*
         these two youtube URLs don't work:
            "http://www.youtube.com/watch?v=yVpbFMhOAwE&feature=player_embedded", ** doesn't work
            "http://www.youtube.com/watch?v=6zUVS4kJtrA&feature=c4-overview-vl&list=PLbzoR-pLrL6qucl8-lOnzvhFc2UM1tcZA" ** doesn't work,
         */

        private Regex rYouTube = new Regex(szRegExpMatchYouTube, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private Regex rVimeo = new Regex(szRegExpMatchVimeo, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private bool IsValidYoutubeURL()
        {
            return !String.IsNullOrEmpty(YouTubeID());
        }

        private bool IsValidVimeoURL()
        {
            return !String.IsNullOrEmpty(VimeoID());
        }

        private string YouTubeID()
        {
            return IDFromRegexp(rYouTube);
        }

        private string VimeoID()
        {
            return IDFromRegexp(rVimeo);
        }

        private string IDFromRegexp(Regex r)
        {
            Match m = r.Match(VideoReference);
            if (m.Captures.Count == 1 && m.Groups.Count > 1)
                return m.Groups[1].Value;
            return string.Empty;
        }

        public VideoSource GuessSource()
        {
            if (IsValidYoutubeURL())
                return VideoSource.YouTube;
            else if (IsValidVimeoURL())
                return VideoSource.Vimeo;
            return VideoSource.Unknown;
        }

        /// <summary>
        /// Returns the HTML IFrame to embed the video
        /// </summary>
        /// <returns>A string that is the IFRAME to embed.</returns>
        public string EmbedHTML()
        {
            switch (Source)
            {
                case VideoSource.YouTube:
                    return String.Format(CultureInfo.InvariantCulture, vidTemplateYouTube, YouTubeID(), defWidth, defHeight);
                case VideoSource.Vimeo:
                    {
                        WebRequest request = WebRequest.Create(new Uri(String.Format(CultureInfo.InvariantCulture, "http://vimeo.com/api/oembed.json?url={0}", HttpUtility.UrlEncode(VideoReference))));
                        try
                        {
                            WebResponse response = request.GetResponse();

                            string szJSON = string.Empty;
                            using (StreamReader reader = new StreamReader(response.GetResponseStream(), System.Text.Encoding.Default))
                                szJSON = reader.ReadToEnd();

                            VimeoEmbedResponse ver = szJSON.DeserialiseFromJSON<VimeoEmbedResponse>();
                            return ver.HTML;
                        }
                        catch (WebException)
                        {
                            return string.Empty;
                        }
                    }
                default:
                    return string.Empty;
            }
        }
        #endregion
    }

    [DataContract]
    public class VimeoEmbedResponse
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "html")]
        public string HTML { get; set; }

        [DataMember(Name = "width")]
        public int Width { get; set; }

        [DataMember(Name = "height")]
        public string Height { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "thumnbnail_url")]
        public string ThumbnailURL { get; set; }

        [DataMember(Name = "thumbnail_width")]
        public int ThumbWidth { get; set; }

        [DataMember(Name = "thumbnail_height")]
        public int ThumbHeight { get; set; }

        [DataMember(Name = "video_id")]
        public string VideoID { get; set; }
    }
}