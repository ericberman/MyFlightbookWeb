using MyFlightbook.Image.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2013-2026 MyFlightbook LLC
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

        private static readonly char[] paramSeparator = new char[] { '\t' };

        private string m_vidURL;

        public void InitFromSerializedString(int idFlight, string szSerialized)
        {
            if (szSerialized == null)
                throw new ArgumentNullException(nameof(szSerialized));
            string[] rgParams = szSerialized.Split(paramSeparator, StringSplitOptions.RemoveEmptyEntries);
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
            FlightID = -1;
            VideoReference = Comment = string.Empty;
            Source = VideoSource.Unknown;

            ErrorString = string.Empty;
        }

        protected VideoRef(IDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));
            ID = Convert.ToInt32(dr["idFlightVideos"], CultureInfo.InvariantCulture);
            FlightID = Convert.ToInt32(dr["idFlight"], CultureInfo.InvariantCulture);
            VideoReference = dr["vidRef"].ToString();
            Comment = dr["Comment"].ToString();
        }

        public VideoRef(int idFlight, string szVidRef, string szComment) : this()
        {
            FlightID = idFlight;
            VideoReference = szVidRef;
            Comment = szComment;
        }

        public static IEnumerable<VideoRef> FromJSON(string sz)
        {
            if (string.IsNullOrEmpty(sz))
                throw new ArgumentNullException(nameof(sz));
            return JsonConvert.DeserializeObject<VideoRef[]>(sz);
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
                        throw new InvalidOperationException(ImageResources.videoErrNoURL);

                    switch (Source)
                    {
                        case VideoSource.YouTube:
                            if (!IsValidYoutubeURL())
                                throw new InvalidOperationException(ImageResources.videoErrURLNotParseable);
                            break;
                        case VideoSource.Vimeo:
                            break;
                        case VideoSource.Unknown:
                        default:
                            throw new InvalidOperationException(ImageResources.videoErrUnsupportedFormat);
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
        public void Commit()
        {
            // never update - only insert.
            if (ID != idVideoUnknown)
                return;

            if (FlightID <= 0)
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
        private const string vidTemplateYouTube = "<iframe src=\"https://www.youtube.com/embed/{0}\" width=\"{1}\" height=\"{2}\" frameborder=\"0\" webkitallowfullscreen mozallowfullscreen allowfullscreen></iframe>";

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
            return IDFromRegexp(RegexUtility.YouTubeReference);
        }

        private string VimeoID()
        {
            return IDFromRegexp(RegexUtility.VimeoReference);
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
                        WebRequest request = WebRequest.Create(new Uri(String.Format(CultureInfo.InvariantCulture, "https://vimeo.com/api/oembed.json?url={0}", HttpUtility.UrlEncode(VideoReference))));
                        try
                        {
                            using (WebResponse response = request.GetResponse())
                            {
                                string szJSON = string.Empty;
                                using (StreamReader reader = new StreamReader(response.GetResponseStream(), System.Text.Encoding.Default))
                                    szJSON = reader.ReadToEnd();

                                VimeoEmbedResponse ver = szJSON.DeserialiseFromJSON<VimeoEmbedResponse>();
                                return ver.HTML;
                            }
                        }
                        catch (Exception ex) when (ex is WebException)
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
        public string ThumbnailLink { get; set; }

        [DataMember(Name = "thumbnail_width")]
        public int ThumbWidth { get; set; }

        [DataMember(Name = "thumbnail_height")]
        public int ThumbHeight { get; set; }

        [DataMember(Name = "video_id")]
        public string VideoID { get; set; }
    }
}