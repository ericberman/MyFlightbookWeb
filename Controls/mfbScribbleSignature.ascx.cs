using MyFlightbook;
using MyFlightbook.Image;
using System;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2018-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbScribbleSignature : System.Web.UI.UserControl
{
    public bool Enabled
    {
        get { return valSignature.Enabled; }
        set { valSignature.Enabled = value; }
    }

    public byte[] Base64Data()
    {
        byte[] rgbSignature = null;

        try
        {
            rgbSignature = ScribbleImage.FromDataLinkURL(hdnSigData.Value);
        }
        catch (MyFlightbookException ex)
        {
            lblSigErr.Text = ex.Message;
            return null;
        }

        return rgbSignature;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Page.ClientScript.RegisterClientScriptInclude("signatureScript", ResolveClientUrl("~/public/Scripts/signature.js?v=2"));
        Page.ClientScript.RegisterClientScriptInclude("dataURL", ResolveClientUrl("~/public/Scripts/todataurl-png.js"));
    }

    protected void valSignature_ServerValidate(object source, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException("args");

        if (Enabled && !ScribbleImage.IsValidDataURL(hdnSigData.Value))
            args.IsValid = false;
    }
}