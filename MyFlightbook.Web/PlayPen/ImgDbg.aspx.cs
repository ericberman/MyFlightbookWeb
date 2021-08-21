using ImageMagick;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/******************************************************
 * 
 * Copyright (c) 2010-2021 MyFlightbook LLC
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

        foreach (MyFlightbook.Controls.ImageControls.mfbFileUpload fu in mfbMultiFileUpload1.FileUploadControls)
        {
            if (fu.HasFile)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "<hr /><br />File: {0}<br />", fu.PostedFile.FileName);
                using (IMagickImage image = new MagickImage(fu.PostedFile.InputStream)) { 
                    IExifProfile exif = image.GetExifProfile();

                if (exif == null)
                    continue;

                    // Write all values to the console
                    foreach (IExifValue value in exif.Values)
                    {
                        if (value.IsArray)
                        {
                            List<string> lst = new List<string>();
                            object o = value.GetValue();

                            if (o is byte[] rgbyte)
                            {
                                foreach (byte b in rgbyte)
                                    lst.Add(b.ToString("X", CultureInfo.InvariantCulture));
                            }
                            else if (o is ushort[] rgshort)
                            {
                                foreach (ushort u in rgshort)
                                    lst.Add(u.ToString(CultureInfo.InvariantCulture));
                            }
                            else if (o is Rational[] rgrational)
                            {
                                foreach (Rational r in rgrational)
                                    lst.Add(r.ToString(CultureInfo.InvariantCulture));
                            }
                            else
                                lst.Add(o.ToString());

                            sb.AppendFormat(CultureInfo.CurrentCulture, "{0}({1}): [{2}]<br />", value.Tag, value.DataType, String.Join(", ", lst));

                        }
                        else
                            sb.AppendFormat(CultureInfo.CurrentCulture, "{0}({1}): {2}<br />", value.Tag, value.DataType, value.ToString());
                    }
                }
            }
        }

        lblDiagnose.Text = sb.ToString();
    }
}