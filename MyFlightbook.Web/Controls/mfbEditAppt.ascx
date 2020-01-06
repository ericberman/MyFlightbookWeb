<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbEditAppt" Codebehind="mfbEditAppt.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<%@ Register src="mfbTypeInDate.ascx" tagname="mfbTypeInDate" tagprefix="uc3" %>
<asp:ModalPopupExtender ID="ModalPopupExtender1" BehaviorID="mpeEditAppt" BackgroundCssClass="modalBackground" runat="server" TargetControlID="btnDummy" PopupControlID="pnlEditAppt"></asp:ModalPopupExtender>
<asp:Panel ID="pnlEditAppt" DefaultButton="btnSaveAppt" style="display:none" runat="server" CssClass="modalpopup">
    <div><asp:Localize ID="Localize3" runat="server" Text="<%$ Resources:Schedule, EventTitle %>"></asp:Localize></div>
    <div>
        <asp:TextBox ID="txtApptTitle" runat="server" Width="90%"></asp:TextBox>
        <asp:TextBoxWatermarkExtender BehaviorID="watermarkTitle" ID="TextBoxWatermarkExtender1" TargetControlID="txtApptTitle" WatermarkCssClass="watermark" WatermarkText="<%$ Resources:Schedule, EventTitleWatermark %>" runat="server"></asp:TextBoxWatermarkExtender>
    </div>
    <div><asp:Localize ID="Localize1" runat="server" Text="<%$ Resources:Schedule, EventStart %>"></asp:Localize></div>
    <div>
        <uc3:mfbTypeInDate ID="dateStart" Width="80px" runat="server" ForceAjax="true" />
        <asp:DropDownList ID="cmbHourStart" runat="server">
        </asp:DropDownList>
    </div>
    <div><asp:Localize ID="Localize2" runat="server" Text="<%$ Resources:Schedule, EventEnd %>"></asp:Localize></div>
    <div>
        <uc3:mfbTypeInDate ID="dateEnd" Width="80px" runat="server" ForceAjax="true" />
        <asp:DropDownList ID="cmbHourEnd" runat="server">
        </asp:DropDownList>
    </div>
    <div style="text-align: center">
        <asp:Button runat="server" Text="<%$ Resources:Schedule, EventCancel %>" ID="btnCancel" OnClientClick="javascript:hideEditor();return false;"></asp:Button>
        &nbsp;
        <asp:Button runat="server" Text="<%$ Resources:Schedule, EventDelete %>" ID="btnDeleteAppt"></asp:Button>
        &nbsp; 
        <asp:Button runat="server" Text="<%$ Resources:Schedule, EventSave %>" ID="btnSaveAppt"></asp:Button>
    </div>
    <asp:HiddenField runat="server" ID="hdnApptID"></asp:HiddenField>
    <asp:HiddenField ID="hdnResource" runat="server" />
    <asp:HiddenField ID="hdnDefaultTitle" runat="server" />
</asp:Panel>
<div style="display: none">
    <asp:Button ID="btnDummy" runat="server" Text="" />
    <script>
        function setApptDate(dt, idDate, idHour) {
            var d = new Date(dt.getYear(), dt.getMonth(), dt.getDay(), 0, 0, 0, 0);
            $find(idDate).set_selectedDate(d);
            var h = parseInt(dt.getHours());
            var m = parseInt(dt.getMinutes() / 15) * 15;
            document.getElementById(idHour).value = (h * 60 + m).toString();
        }

        function getApptDate(idDate, idHour) {
            var dLocal = $find(idDate)._selectedDate;
            dLocal.setMinutes(dLocal.getMinutes() - dLocal.getTimezoneOffset());

            var minutesIntoDay = parseInt(document.getElementById(idHour).value);

            var h = parseInt(minutesIntoDay / 60);
            var m = parseInt(minutesIntoDay % 60);
            var d = new Date(dLocal.getUTCFullYear(), dLocal.getUTCMonth(), dLocal.getUTCDate(), h, m, 0, 0);
            dt = new DayPilot.Date(d, true);
            return dt;
        }

        function setStartDate(dt) { setApptDate(dt, '<% =dateStart.CalendarExtenderControl.ClientID %>', '<% =cmbHourStart.ClientID %>'); }
        function getStartDate() { return getApptDate('<% =dateStart.CalendarExtenderControl.ClientID %>', '<% =cmbHourStart.ClientID %>'); }
        function setEndDate(dt) { setApptDate(dt, '<% =dateEnd.CalendarExtenderControl.ClientID %>', '<% =cmbHourEnd.ClientID %>'); }
        function getEndDate() { return getApptDate('<% =dateEnd.CalendarExtenderControl.ClientID %>', '<% =cmbHourEnd.ClientID %>'); }

        function getAppointment() {
            return new DayPilot.Event({
                id: document.getElementById('<% =hdnApptID.ClientID %>').value,
                start: getStartDate(), end: getEndDate(), resource: document.getElementById('<% =hdnResource.ClientID %>').value,
                text: $find('watermarkTitle').get_text()
            });
        }

        function hideEditor() {
            document.getElementById('<% =pnlEditAppt.ClientID %>').style.display = 'none';
            $find("mpeEditAppt").hide();
        }

        /* Handle escape to dismiss */
        function pageLoad(sender, args) {
            if (!args.get_isPartialLoad()) {
                $addHandler(document, "keydown", onKeyDown);
            }
        }

        function onKeyDown(e) {
            if (e && e.keyCode == Sys.UI.Key.esc)
                hideEditor();
        }

        function setAppointment(e) {
            document.getElementById('<% =hdnApptID.ClientID %>').value = e.data.id;
            setStartDate(e.data.start);
            setEndDate(e.data.end);
            document.getElementById('<% =hdnResource.ClientID %>').value = e.data.resource;
            $find('watermarkTitle').set_text(e.data.text);
            document.getElementById('<% =btnSaveAppt.ClientID %>').onclick = function (args) {
                if (typeof args === 'undefined')
                    args = $.event.fix(window.event);
                e.onSave(args, hideEditor);
            }
            document.getElementById('<% =btnDeleteAppt.ClientID %>').onclick = function (args)
            {
                if (typeof args === 'undefined')
                    args = $.event.fix(window.event);
                var r = window.confirm('<% = Resources.Schedule.confirmDelete %>');
                if (r == true) {
                    e.onDelete(args, hideEditor);
                }
                else {
                    args.stopPropagation();
                    args.preventDefault();
                }
            }
        }

        function editAppt(e, isNew) {
            setAppointment(e);
            document.getElementById('<% =btnDeleteAppt.ClientID %>').style.display = (isNew) ? 'none' : 'inline';
            $find("mpeEditAppt").show();
            document.getElementById('<% =txtApptTitle.ClientID %>').focus();
        }

        function newAppt(e, onSave) {
            e.onSave = onSave;
            e.onDelete = function (e) { };
            if (e.data.text === '')
                e.data.text = document.getElementById('<% =hdnDefaultTitle.ClientID %>').value;
            editAppt(e, true);
        }
    </script>
</div>
