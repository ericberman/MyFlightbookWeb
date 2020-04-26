using MyFlightbook;
using MyFlightbook.Image;
using System;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2018-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbScribbleSignature : System.Web.UI.UserControl
{
    public event EventHandler SaveClicked;
    public event EventHandler CancelClicked;

    public bool Enabled
    {
        get { return valSignature.Enabled; }
        set { valSignature.Enabled = value; }
    }

    public bool ShowSave
    {
        get { return btnSave.Visible; }
        set { btnSave.Visible = value; }
    }

    public bool ShowCancel
    {
        get { return btnCancel.Visible; }
        set { btnCancel.Visible = value; }
    }

    public string WatermarkRef
    {
        get { return hdnWM.Value; }
        set { hdnWM.Value = value ?? string.Empty; }
    }

    public string ColorRef
    {
        get { return hdnColor.Value; }
        set { hdnColor.Value = value ?? "#0000ff"; }
    }

    public byte[] Base64Data()
    {
        byte[] rgbSignature;
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
        Page.ClientScript.RegisterClientScriptInclude("signatureScript", ResolveClientUrl("~/public/Scripts/signature.js?v=3"));
        Page.ClientScript.RegisterClientScriptInclude("dataURL", ResolveClientUrl("~/public/Scripts/todataurl-png.js"));
    }

    protected void valSignature_ServerValidate(object source, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException(nameof(args));

        if (Enabled && !ScribbleImage.IsValidDataURL(hdnSigData.Value))
            args.IsValid = false;
    }

    protected void btnSave_Click(object sender, EventArgs e)
    {
        SaveClicked?.Invoke(sender, e);
    }

    protected void btnCancel_Click(object sender, EventArgs e)
    {
        CancelClicked?.Invoke(sender, e);
    }
}