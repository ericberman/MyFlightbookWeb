using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2011-2016 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_ImportTable : System.Web.UI.Page
{
    private const int cColumnsOfProps = 4;

    private void AddProp(CustomPropertyType cpt, TableRow tr)
    {
        string szUnit = string.Empty;
        switch (cpt.Type)
        {
            case CFPPropertyType.cfpBoolean:
                szUnit = Resources.LogbookEntry.importUnitBoolean;
                break;
            case CFPPropertyType.cfpCurrency:
            case CFPPropertyType.cfpDecimal:
            case CFPPropertyType.cfpInteger:
                szUnit = Resources.LogbookEntry.importUnitNumber;
                break;
            case CFPPropertyType.cfpDate:
            case CFPPropertyType.cfpDateTime:
                szUnit = Resources.LogbookEntry.importUnitDate;
                break;
            case CFPPropertyType.cfpString:
                szUnit = Resources.LogbookEntry.importUnitText;
                break;
            default:
                break;
        }

        TableCell tc = new TableCell();
        tr.Cells.Add(tc);
        tc.Style["padding"] = "5px";

        Panel pTitle = new Panel();
        tc.Controls.Add(pTitle);
        Label lTitle = new Label();
        pTitle.Controls.Add(lTitle);
        lTitle.Style["font-weight"] = "bold";
        lTitle.Text = cpt.Title;

        Panel pUnit = new Panel();
        tc.Controls.Add(pUnit);
        pUnit.Controls.Add(new LiteralControl(szUnit));

        Panel pDesc = new Panel();
        tc.Controls.Add(pDesc);
        pDesc.Style["font-size"] = "smaller";
        pDesc.Controls.Add(new LiteralControl(cpt.Description));
        
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        TableRow tr = new TableRow();
        tblAdditionalProps.Rows.Add(tr);
        CustomPropertyType[] rgCpt = CustomPropertyType.GetCustomPropertyTypes("");
        int cRows = (rgCpt.Length + cColumnsOfProps - 1) / cColumnsOfProps;
        for (int iRow= 0; iRow < cRows; iRow++)
        {
            for (int iCol = 0; iCol < cColumnsOfProps; iCol++)
            {
                int iProp = (iCol * cRows) + iRow;
                if (iProp < rgCpt.Length)
                    AddProp(rgCpt[iProp], tr);
                else
                {
                    // add blank cells to pad out the row.
                    tr.Cells.Add(new TableCell());
                }
            }
            tr = new TableRow();
            tblAdditionalProps.Rows.Add(tr);
            tr.Style["vertical-align"] = "top";
        }

        if (tr.Cells.Count == 0)
            tblAdditionalProps.Rows.Remove(tr);

        this.Master.SelectedTab = tabID.tabLogbook;
        this.Title = lblImportHeader.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LogbookEntry.importTableHeader, Branding.CurrentBrand.AppName);
        litTableMainFields.Text = Branding.ReBrand(Resources.LogbookEntry.ImportTableDescription);
    }
}