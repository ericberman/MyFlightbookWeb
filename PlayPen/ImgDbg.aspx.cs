using System;
using System.Globalization;
using System.Text;
using gma.Drawing.ImageInfo;

/******************************************************
 * 
 * Copyright (c) 2010-2016 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Public_ImgDbg : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void btnTest_Click(object sender, EventArgs e)
    {
        StringBuilder sb = new StringBuilder();

        foreach (Controls_mfbFileUpload fu in mfbMultiFileUpload1.FileUploadControls)
        {
            if (fu.HasFile)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "<hr /><br />File: {0}<br />", fu.PostedFile.FileName);
                System.Drawing.Image image = System.Drawing.Image.FromStream(fu.PostedFile.InputStream, false, false);

                try 
                {
                    System.Drawing.Imaging.PropertyItem p = image.GetPropertyItem((int)PropertyTagId.GpsLatitudeRef);
                    sb.Append("Got latitudeRef; ");
                    sb.AppendFormat(CultureInfo.CurrentCulture, "Latitude Ref: {0}<br />", (string)PropertyTag.getValue(p));
                }
                catch (ArgumentException ex)
                {
                    sb.AppendFormat(CultureInfo.CurrentCulture, "Latitude Ref: Not found! {0}<br />", ex.Message);
                }

                try
                {
                    System.Drawing.Imaging.PropertyItem p = image.GetPropertyItem((int)PropertyTagId.GpsLatitude);
                    sb.Append("Got latitude; ");
                    Fraction[] f = (Fraction[])PropertyTag.getValue(p);
                    sb.AppendFormat(CultureInfo.CurrentCulture, "has {0} elements; ", f.Length);
                    sb.AppendFormat(CultureInfo.CurrentCulture, "Latitude values: {0}, {1}, {2}<br />", f[0], f[1], f[2]);
                }
                catch (ArgumentException ex)
                {
                    sb.AppendFormat(CultureInfo.CurrentCulture, "Latitudes values: Not Found! {0}<br />", ex.Message);
                }

                try
                {
                    System.Drawing.Imaging.PropertyItem p = image.GetPropertyItem((int)PropertyTagId.GpsLongitudeRef);
                    sb.Append("Got longitudeRef; ");
                    sb.AppendFormat(CultureInfo.CurrentCulture, "Longitude Ref: {0}<br />", (string)PropertyTag.getValue(p));
                }
                catch (ArgumentException ex)
                {
                    sb.AppendFormat(CultureInfo.CurrentCulture, "Longitude Ref: Not found! {0}<br />", ex.Message);
                }

                try
                {
                    System.Drawing.Imaging.PropertyItem p = image.GetPropertyItem((int)PropertyTagId.GpsLongitude);
                    sb.Append("Got longitude; ");
                    Fraction[] f = (Fraction[])PropertyTag.getValue(p);
                    sb.AppendFormat(CultureInfo.CurrentCulture, "has {0} elements; ", f.Length);
                    sb.AppendFormat(CultureInfo.CurrentCulture, "Longitude values: {0}, {1}, {2}<br />", f[0], f[1], f[2]);
                }
                catch (ArgumentException ex)
                {
                    sb.AppendFormat(CultureInfo.CurrentCulture, "Longitude values: Not Found! {0}<br />", ex.Message);
                }
            }
        }

        lblDiagnose.Text = sb.ToString();
    }
}