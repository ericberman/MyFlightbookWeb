using System;
using System.Globalization;
using System.Text;
using System.Collections.Generic;
using ImageMagick;

/******************************************************
 * 
 * Copyright (c) 2010-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
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
                IMagickImage image = new MagickImage(fu.PostedFile.InputStream);
                ExifProfile exif = image.GetExifProfile();
                // Write all values to the console
                foreach (IExifValue value in exif.Values)
                {
                    if (value.IsArray && value.Value is Rational[])
                    {
                        List<string> lst = new List<string>();
                        foreach (Rational r in (Rational[]) value.Value)
                            lst.Add(r.ToString());
                        sb.AppendFormat(CultureInfo.CurrentCulture, "{0}({1}): [{2}]<br />", value.Tag, value.DataType, String.Join(", ", lst));

                    }
                    else
                    sb.AppendFormat(CultureInfo.CurrentCulture, "{0}({1}): {2}<br />", value.Tag, value.DataType, value.ToString());
                }
            }
        }

        lblDiagnose.Text = sb.ToString();
    }
}