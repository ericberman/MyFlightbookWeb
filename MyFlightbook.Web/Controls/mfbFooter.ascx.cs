using MyFlightbook;
using System;
using System.Web.UI;
using System.Collections.Generic;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2007-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbFooter : UserControl
{   
    protected IDictionary<Brand.FooterLinkKey, BrandLink> Links { get; set; }

    private void BindLink(Brand.FooterLinkKey k, HyperLink l)
    {
        if (!Links.TryGetValue(k, out BrandLink bl) || !bl.IsVisible)
        {
            l.Visible = false;
        }
        else
        {
            l.NavigateUrl = bl.LinkRef;
            if (bl.OpenInNewPage)
                l.Target = "_blank";

            if (!String.IsNullOrEmpty(bl.ImageRef))
            {
                Image img = new Image() { ImageUrl = bl.ImageRef, AlternateText = bl.Name, ToolTip = bl.Name };
                l.Controls.Add(img);
                img.Style["vertical-align"] = "middle";
                img.Style["margin-right"] = ".5em";
            }
            l.Controls.Add(new Label() { Text = bl.Name });
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            mvClassicMobile.SetActiveView(Request.IsMobileSession() ? vwMobile : vwClassic);

            Links = Branding.CurrentBrand.FooterLinks();

            BindLink(Brand.FooterLinkKey.About, lnkAbout);
            BindLink(Brand.FooterLinkKey.Privacy, lnkPrivacy);
            BindLink(Brand.FooterLinkKey.Terms, lnkTerms);
            BindLink(Brand.FooterLinkKey.Developers, lnkDevelopers);

            BindLink(Brand.FooterLinkKey.Contact, lnkContact);
            BindLink(Brand.FooterLinkKey.FAQ, lnkFAQ);
            BindLink(Brand.FooterLinkKey.Videos, lnkVideos);
            BindLink(Brand.FooterLinkKey.Blog, lnkBlog);

            BindLink(Brand.FooterLinkKey.Mobile, lnkPDA);
            BindLink(Brand.FooterLinkKey.Classic, lnkViewClassic);

            BindLink(Brand.FooterLinkKey.Facebook, lnkFacebook);
            BindLink(Brand.FooterLinkKey.Twitter, lnkTwitter);
            BindLink(Brand.FooterLinkKey.RSS, lnkRSS);
        }
    }
}
