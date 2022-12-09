using System;

/******************************************************
 * 
 * Copyright (c) 2015-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Image
{
    public partial class ViewPic : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string szClass = util.GetStringParam(Request, "r");
            string szKey = util.GetStringParam(Request, "k");
            string szThumb = util.GetStringParam(Request, "t");
            Title = szThumb;

            try
            {
                if (Enum.TryParse(szClass, out MFBImageInfoBase.ImageClass ic) && !String.IsNullOrEmpty(szKey) && !String.IsNullOrEmpty(szThumb) && !szThumb.Contains("?"))  // Googlebot seems to be adding "?resize=300,300
                {
                    MFBImageInfo mfbii = new MFBImageInfo(ic, szKey, szThumb);
                    if (mfbii == null)
                        throw new InvalidOperationException("mfbii null in ViewPic");

                    if (Request == null)
                        throw new MyFlightbookException("Null request in ViewPic");

                    Response.Redirect(mfbii.ResolveFullImage(), false);
                }
                else
                    throw new InvalidOperationException("Invalid image class, key, or thumb");
            }
            catch (InvalidOperationException)
            {
                Response.Clear();
                Response.StatusCode = 404;
                Response.End();
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                Response.Clear();
                Response.StatusCode = 400;
                Response.End();
            }
        }
    }
}