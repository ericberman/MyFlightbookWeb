using System;
using System.Globalization;
using MyFlightbook;
using MyFlightbook.Image;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_ViewPic : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        string szClass = util.GetStringParam(Request, "r");
        string szKey = util.GetStringParam(Request, "k");
        string szThumb = util.GetStringParam(Request, "t");

        MFBImageInfo.ImageClass ic = MFBImageInfo.ImageClass.Unknown;
        try { ic = (MFBImageInfo.ImageClass)Enum.Parse(typeof(MFBImageInfo.ImageClass), szClass); }
        catch (InvalidCastException) { }
        catch (OverflowException) { }
        catch (ArgumentException) { }

        this.Title = szThumb;
        if (ic != MFBImageInfo.ImageClass.Unknown && !String.IsNullOrEmpty(szKey) && !String.IsNullOrEmpty(szThumb) && !szThumb.Contains("?"))  // Googlebot seems to be adding "?resize=300,300
        {
            MFBImageInfo mfbii = new MFBImageInfo(ic, szKey, szThumb);
            if (mfbii == null)
                throw new MyFlightbookException("mfbii null in ViewPic");

            if (Request == null)
                throw new MyFlightbookException("Null request in ViewPic");

            string ua = String.IsNullOrEmpty(Request.UserAgent) ? string.Empty : Request.UserAgent.ToUpperInvariant();
            if (mfbii.ImageType == MFBImageInfo.ImageFileType.S3VideoMP4 && !ua.Contains("IPHONE") && !ua.Contains("IPAD"))
            {
                // For android devices, need to put it into actual HTML for it to play in-line in the browser.
                mfbEditableImage1.MFBImageInfo = mfbii;
            }
            else
            {
                try
                {
                    Response.Redirect(mfbii.ResolveFullImage());
                }
                catch (ArgumentException ex)
                {
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Argument Exception in ViewPic key={0}\r\nthumb={1}\r\n", szKey, szThumb), ex);
                }
                catch (System.Web.HttpUnhandledException ex)
                {
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Argument Exception in ViewPic key={0}\r\nthumb={1}\r\n", szKey, szThumb), ex);
                }
                catch (NullReferenceException ex)
                {
                    throw new MyFlightbookException("Null reference on resolve full image (but where?) ", ex);
                }
            }
        }
        else
        {
            Response.Clear();
            Response.StatusCode = 404;
            Response.End();
        }
    }
}