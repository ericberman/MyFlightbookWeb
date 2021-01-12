using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Controls.Prefs
{
    public partial class mfbFlightColoring : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Profile pf = Profile.GetUser(Page.User.Identity.Name);
                IEnumerable<FlightColor> keywordColors = pf.KeywordColors;
                if (keywordColors == null)
                    return;

                int i = 0;
                foreach (FlightColor fc in keywordColors)
                {
                    if (String.IsNullOrWhiteSpace(fc.KeyWord))
                        continue;

                    switch (i++)
                    {
                        case 0:
                            txtKey1.Text = fc.KeyWord;
                            txtCol1.Text = fc.ColorString;
                            lblSamp1.BackColor = fc.Color;
                            break;
                        case 1:
                            txtKey2.Text = fc.KeyWord;
                            txtCol2.Text = fc.ColorString;
                            lblSamp2.BackColor = fc.Color;
                            break;
                        case 2:
                            txtKey3.Text = fc.KeyWord;
                            txtCol3.Text = fc.ColorString;
                            lblSamp3.BackColor = fc.Color;
                            break;
                        case 3:
                            txtKey4.Text = fc.KeyWord;
                            txtCol4.Text = fc.ColorString;
                            lblSamp4.BackColor = fc.Color;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        protected void btnUpdateColors_Click(object sender, EventArgs e)
        {
            List<FlightColor> lst = new List<FlightColor>()
            {
                new FlightColor(txtKey1.Text, txtCol1.Text),
                new FlightColor(txtKey2.Text, txtCol2.Text),
                new FlightColor(txtKey3.Text, txtCol3.Text),
                new FlightColor(txtKey4.Text, txtCol4.Text)
            };

            lst.RemoveAll(fc => String.IsNullOrWhiteSpace(fc.KeyWord));
            Profile pf = Profile.GetUser(Page.User.Identity.Name);
            pf.KeywordColors = lst;
            lblColorsUpdated.Visible = true;
        }
    }
}