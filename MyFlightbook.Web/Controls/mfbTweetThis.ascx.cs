using System;
using System.Web;
using MyFlightbook;
using MyFlightbook.SocialMedia;

/******************************************************
 * 
 * Copyright (c) 2009-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbTweetThis : System.Web.UI.UserControl
{
    private LogbookEntry m_le;

    public LogbookEntry FlightToTweet
    {
        get { return m_le; }
        set { m_le = value; WireUpTweetLink(); }
    }

    protected void WireUpTweetLink()
    {
        if (FlightToTweet != null)
        {
            string szTwitterURL = String.Format(System.Globalization.CultureInfo.InvariantCulture, "https://twitter.com/intent/tweet?text={0}", HttpUtility.UrlEncode(TwitterPoster.TweetContent(FlightToTweet, Request.Url.Host)));

            lnkTweetThis.Attributes["onclick"] = "javascript:window.open('" + szTwitterURL + "', 'TweetFlight', 'height=700,width=800,scrollbars=1,menubar=1,toolbar=1');";
            lnkTweetThis.Visible = FlightToTweet.CanPost;
        }
    }
}
