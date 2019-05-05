using MyFlightbook;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2009-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_AircraftControls_SelectMake : System.Web.UI.UserControl, INamingContainer
{
    protected class SelectMakeHeaderTemplate : Control, INamingContainer
    {
        public SelectMakeHeaderTemplate()
        {
        }
    }

    public event EventHandler<MakeSelectedEventArgs> ModelChanged;
    public event EventHandler MajorChangeRequested;

    public enum MakeEditMode { Edit, EditWithConfirm, Locked }

    #region Properties
    [TemplateContainer(typeof(SelectMakeHeaderTemplate)), PersistenceMode(PersistenceMode.InnerDefaultProperty), TemplateInstance(TemplateInstance.Single)]
    public ITemplate Prompt { get; set; }

    public PlaceHolder HeaderContainer
    {
        get { return plcPrompt; }
    }

    private MakeEditMode _editMode = MakeEditMode.Edit;

    public MakeEditMode EditMode
    {
        get { return _editMode; }
        set
        {
            _editMode = value;
            switch (_editMode)
            {
                case MakeEditMode.Edit:
                    mvModel.SetActiveView(vwEdit);
                    break;
                case MakeEditMode.EditWithConfirm:
                    mvModel.SetActiveView(vwReadOnly);
                    imgEditAircraftModel.Visible = true;
                    break;
                case MakeEditMode.Locked:
                    mvModel.SetActiveView(vwReadOnly);
                    imgEditAircraftModel.Visible = false;
                    break;
            }
        }
    }

    public MakeModel SelectedModel
    {
        get
        {
            return MakeModel.GetModel(SelectedModelID);
        }
    }

    private const string szKeyVSAircraftAttributes = "szVSAircraftAttrib";
    public IEnumerable<LinkedString> AircraftAttributes
    {
        get { return (IEnumerable<LinkedString>) ViewState[szKeyVSAircraftAttributes]; }
        set { ViewState[szKeyVSAircraftAttributes] = value; }
    }

    public int SelectedModelID
    {
        get
        {
            int modelID = MakeModel.UnknownModel;
            if (int.TryParse(cmbMakeModel.SelectedValue, out modelID))
                return modelID;
            return MakeModel.UnknownModel;
        }
        set
        {
            if (value == MakeModel.UnknownModel)
            {
                LastSelectedManufacturer = Manufacturer.UnsavedID;
                cmbManufacturers.SelectedIndex = 0;
                UpdateModelList(Manufacturer.UnsavedID);
                UpdateAttributesForModel(null);
            }
            else
            {
                MakeModel mm = MakeModel.GetModel(value);
                cmbManufacturers.SelectedValue = mm.ManufacturerID.ToString(CultureInfo.InvariantCulture);
                LastSelectedManufacturer = mm.ManufacturerID;
                UpdateModelList(mm.ManufacturerID);
                cmbMakeModel.SelectedValue = mm.MakeModelID.ToString(CultureInfo.InvariantCulture);
                mvModelDisplay.SetActiveView(util.GetIntParam(Request, "a", 0) != 0 && MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanManageData ? vwLinkedModel : vwStaticModel);
                lblMakeModel.Text = mm.ManufacturerDisplay + Resources.LocalizedText.LocalizedSpace + mm.ModelDisplayName;
                lnkMakeModel.Text = mm.ManufacturerDisplay + Resources.LocalizedText.LocalizedSpace + mm.ModelDisplayName;
                lnkMakeModel.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/EditMake.aspx?id={0}", mm.MakeModelID);
                UpdateAttributesForModel(mm);
            }
        }
    }

    protected void UpdateAttributesForModel(MakeModel mm)
    {
        List<LinkedString> lst = new List<LinkedString>();
        lst.AddRange(AircraftAttributes);
        if (mm != null)
        {
            if (!String.IsNullOrEmpty(mm.FamilyName))
                lst.Add(new LinkedString(ModelQuery.ICAOPrefix + mm.FamilyName));
            foreach (string sz in mm.AttributeList())
                lst.Add(new LinkedString(sz));
        }
        rptAttributes.DataSource = lst;
        rptAttributes.DataBind();
    }

    protected int LastSelectedManufacturer
    {
        get { return String.IsNullOrEmpty(hdnLastMan.Value) ? Manufacturer.UnsavedID : Convert.ToInt32(hdnLastMan.Value, CultureInfo.InvariantCulture); }
        set { hdnLastMan.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    #endregion

    #region Page setup/loading
    protected void Page_Init(object sender, EventArgs e)
    {
        // For efficiency of viewstate, we repopulate manufactures on each postback.
        cmbManufacturers.DataSource = Manufacturer.CachedManufacturers();
        cmbManufacturers.DataBind();
    }

    protected override void OnInit(EventArgs e)
    {
        if (Prompt != null)
            Prompt.InstantiateIn(plcPrompt);
        base.OnInit(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
    #endregion

    #region Managing the combos
    protected void cmbMakeModel_SelectedIndexChanged(object sender, EventArgs e)
    {
        int modelID = SelectedModelID;
        UpdateAttributesForModel(modelID == MakeModel.UnknownModel ? null : MakeModel.GetModel(modelID));
        if (ModelChanged != null)
            ModelChanged(this, new MakeSelectedEventArgs(modelID));
    }

    protected void UpdateModelList(int idManufacturer)
    {
        ListItem liSelect = cmbMakeModel.Items[0];  // hold onto the "please select a model" item.
        cmbMakeModel.Items.Clear();
        cmbMakeModel.Items.Add(liSelect);
        cmbMakeModel.DataSource = (idManufacturer == MakeModel.UnknownModel) ? new System.Collections.ObjectModel.Collection<MakeModel>() : MakeModel.MatchingMakes(idManufacturer);
        cmbMakeModel.DataBind();
    }

    protected void cmbManufacturers_SelectedIndexChanged(object sender, EventArgs e)
    {
        int newManId = Convert.ToInt32(cmbManufacturers.SelectedValue, CultureInfo.InvariantCulture);
        if (newManId != LastSelectedManufacturer)
        {
            UpdateAttributesForModel(null);
            UpdateModelList(newManId);
            LastSelectedManufacturer = newManId;
            if (ModelChanged != null)
                ModelChanged(this, new MakeSelectedEventArgs(SelectedModelID));
        }
    }

    protected void btnChangeModelTweak_Click(object sender, EventArgs e)
    {
        EditMode = MakeEditMode.Edit;
        SelectedModelID = SelectedModelID; // not quite sure why I need to do this, but otherwise the manufacturer dropdown reverts.
    }

    protected void btnChangeModelClone_Click(object sender, EventArgs e)
    {
        if (MajorChangeRequested != null)
            MajorChangeRequested(this, e);        
    }
    #endregion

    protected void lnkPopulateModel_Click(object sender, EventArgs e)
    {
        int mmID = MakeModel.UnknownModel;
        if (int.TryParse(hdnSelectedModel.Value, out mmID))
        {
            MakeModel mm = MakeModel.GetModel(mmID);
            cmbManufacturers.SelectedValue = mm.ManufacturerID.ToString(CultureInfo.InvariantCulture);
            LastSelectedManufacturer = mm.ManufacturerID;
            UpdateModelList(mm.ManufacturerID);
            cmbMakeModel.SelectedValue = mmID.ToString(CultureInfo.InvariantCulture);
            UpdateAttributesForModel(mm);
            if (ModelChanged != null)
                ModelChanged(this, new MakeSelectedEventArgs(SelectedModelID));

            hdnSelectedModel.Value = txtFilter.Text = string.Empty;
        }
    }
}