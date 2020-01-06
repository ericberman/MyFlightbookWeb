<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Public_ClubsManual" Codebehind="ClubsManual.aspx.cs" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" runat="Server">
    Overview of Clubs on MyFlightbook
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" runat="Server">
    <div>
    <p>
        Flying clubs on MyFlightbook provide a convenient way to
manage aircraft that are shared among multiple pilots, whether in a club/FBO
environment or even just a single aircraft shared among a few co-owner pilots. 
(The term “club” here refers to any of these scenarios.)
    </p>

    <p>
        The club functionality provides the following high-level
features:
    </p>

    <ul>
        <li>Associate aircraft with your club and manage shared schedules for
those aircraft</li>
        <li>Associate members with your club</li>
        <li>Generate reports of which members flew which club aircraft</li>
        <li>Optionally advertise your club to potential new members</li>
    </ul>

    <p>
        Anybody can create a club on MyFlightbook, and it will
remain active for 30 days.  We ask for a one-time contribution to MyFlightbook
to extend the club’s active status indefinitely.   There is NO recurring
payment required, nor any charges associated with adding members or aircraft.
    </p>

    <h2>Creating a club</h2>

    <p>
        To create a club, go to the website (<a
            href="http://myflightbook.com">http://myflightbook.com</a>) and go to “Flying
Clubs” under the “Aircraft” tab.  (If you are already a member of a club, then
you will go to that club by default; click on the link that says “See All
Available Clubs”)
    </p>

    <p>
        You will see a screen that lets you search for clubs and
create one as well:
    </p>

    <p style="text-align:center">
        <img border="0" alt="Find and Create Clubs Screen" 
            src="ClubsManual_files/image017.jpg" />
    </p>

    <p>
        Click where it says “Create a new club/FBO” to expand the
club-creation tool.  This is where you can enter some basic information about
the club to get it set up:
    </p>

    <p style="text-align:center">
        <img border="0" id="Picture 1" alt="Create club screen"
            src="ClubsManual_files/image018.jpg" />
    </p>

    <p>
        Most of the fields here should be pretty self-explanatory,
but a few are worth calling out, particularly with regard to policy.  Don’t
worry if you don’t have all the information you want right now; you can edit
this all later.
    </p>

        <ul>
            <li>The <b>Description</b> field is what people will see if they
search for your club on MyFlightbook.  If you are interested in potentially
attracting new members, it is worth putting your best foot forward in a few
sentences.  (See below if you are not interested in attracting new members). 
This is not a good place to put lots of policy statements, member rules, etc. 
Keep it relatively brief. 
    </li>
            <li>If your club has a <b>website</b>, provide it.  MyFlightbook will
provide links to this address.
    </li>
            <li>The <b>timezone</b> is important for scheduling so that there is
no ambiguity.  All schedules are in UTC under-the-covers and are converted
based on your timezone for display or data entry.  MyFlightbook will handle
daylight saving time for you.
    </li>
            <li>“<b>Restrict editing of schedule to admins and the owner/creator
of the scheduled item</b>” prevents pilots from editing each other’s items. 
Some clubs operate on an honor system where anybody can edit anything, other
clubs need a little more control.  If this is checked, then if a pilot puts an
item on the schedule, only that pilot or a club admin/owner can edit it or
delete it.  If it is unchecked, then one pilot can modify or delete another
pilot’s entries.
    </li>
            <li>“<b>Prefix all scheduled items with the schedule’s name</b>” – if
checked, each item will include the name of the pilot so that everybody looking
at that schedule will know to whom the entry belongs
    </li>
            <li>“<b>Hide club from search results</b>” makes your club invisible
to search results.  For instance, if you and another pilot are co-owners of an
airplane, then you probably interested in having people contact you to join the
club.  The only way to find out about the club will be by invitation in that
case.
    </li>
            <li>You can specify policy for notifications <b>when an item is
deleted, added or modified</b>.  This can be useful if there might be somebody else who would want
to fly the airplane, optimizing usage of the plane.  The options are to send no
notification, notification just to the admins, or notification to everybody in
the club.
    </li>
            <li>You can also specify <b>double booking</b> policy.  Double-booking
simply means that two or more pilots can simultaneously book an aircraft.
    </li>
        </ul>

    <p>
        Click the “Save” button and your club will be live!  You can
then get to your club by going to “Flying Clubs” under the Aircraft tab.
    </p>

    <h2>Managing your club</h2>

    <p>
        Now that you have created a club, you need to add some
aircraft and members to it.
    </p>

    <p>
        You can always get to your club by going to “Flying Clubs”
under the “Aircraft” tab.  As the creator of the club, you should see a
management bar:
    </p>

    <p style="text-align:center">
        <img border="0" id="Picture 2" alt="Management bar"
            src="ClubsManual_files/image019.png" />
    </p>

    <p>Click on &quot;Manage Club&quot; that to view the management tools:</p>

    <p style="text-align:center">
        <img border="0" id="Picture 3" alt="Expanded management bar"
            src="ClubsManual_files/image020.jpg" />
    </p>

    <p>
        You’ll see 4 tabs: Members, Aircraft, Club Details, and
Reports.  Each of these is discussed below
    </p>

    <h3>Members and Roles</h3>

    <p>
        As the creator of the club, you’ll see your name and the
date that you joined.  Note that your role is “Owner”, since you created the
club.  Each club must have exactly one owner.
    </p>

    <p>
        To add a new member, you must type their email address and
click Invite Member.  They will receive an email with a link to accept
membership, and will be prompted to create a MyFlightbook account if they do
not already have one.  Note that each invitation is specific to the email
address, so if the recipient already has a MyFlightbook account, you need to
send to the address that they use on MyFlightbook.
    </p>

    <p>
        When the pilot accepts your request, they will appear in the
list of members.
    </p>

    <p>
        You can also delete a pilot by clicking the red “x” next to
the pilot’s row.
    </p>

    <p>Club members can have one of three roles:</p>

        <ul>
            <li><strong>Member</strong> – a member of the club, who can see and modify aircraft
schedules. </li>
            <li><strong>Manager</strong> – a member of the club who also has the ability to
administer the club (manage members, aircraft, club details, and reports);
i.e., these are the people that see the management bar pictured above. </li>
            <li><strong>Owner</strong> – a special manager who is the owner of the club.  The
owner has the same capabilities as any manager, but is the point of contact if
MyFlightbook needs to reach you about the club. </li>
        </ul>

    <p>
        You can change a pilot’s role by clicking “Edit” to the left
of their name, selecting the role, and then clicking “Update”.
    </p>

    <h3>Aircraft</h3>

    <p>
        The Aircraft tab lets you add/remove club aircraft, and
provide additional descriptions about them.
    </p>

    <p>The aircraft must already exist in your aircraft list.  </p>

    <p>
        To add an aircraft, select it from the drop-down, and add
any description you like.  This can be a good place to describe things like
rental rates, or provide links to the POH or weight-and-balance, and so forth. 
Click Add to add it.
    </p>
    <p style="text-align:center">
        <img border="0" id="Picture 5" alt="Add Aircraft"
            src="ClubsManual_files/image021.jpg" />
    </p>
    <p style="text-align:left">
        Once an aircraft is added, you can edit it by clicking the &quot;Edit&quot; button.&nbsp; There is also a place to enter high-watermark hours (hobbs or tach, as you desire), which can be useful for aircraft maintenance (described below).
        If the aircraft has been used in club flights and tach or hobbs have been recorded, there is an option to cross-fill those values directly into this field.
    </p>
    <p style="text-align:center">
        <img border="0" id="updatehrs" alt="Update Hours"
            src="ClubsManual_files/updatehours.png" />
    </p>
        <p style="text-align:left">
            The reason that this isn&#39;t done automatically for you is to avoid erroneous values (e.g., somebody putting hobbs in the tach field or vice versa) polluting the high-water value.</p>
    <h3>Club Details</h3>

    <p>
        This tab is simply a repeat of the form you used to create
the club, and is described above.
    </p>

    <h3>Reports</h3>

    <p>
        The Reports tab can be useful for shared expenses or billing. 
While a billing/accounting system is well beyond the scope of what MyFlightbook
can provide (there are many good solutions for that out there today), this can
help inform who flew an airplane without logging it in a timesheet, or flying
hours for insurance purposes.
    </p>

    <p>
        For a flying report, put in the starting and Ending dates and click “Refresh” to
see a report and, optionally, download it to a spreadsheet.
    </p>
    <p style="text-align:center">
        <img border="0" id="Picture 6" alt="Reporting"
            src="ClubsManual_files/image022.jpg" />
    </p>

        <p>
        For privacy reasons, the report only includes flights by
club members in club aircraft during the time window that they are members of
the club.  The data returned includes the date of the flight, the aircraft used
for the flight, the total time logged for the flight, and any hobbs or tach
time recorded for the flight.
    </p>
        <p>
            Just for fun, there is also an option to download all of the routes flown in a KML (Google Earth) file.</p>
        <p>
            The maintenance report allows you to track scheduled maintenance for an aircraft, and lets you view upcoming maintenance across all club aircraft.&nbsp; This includes the standard &quot;AV1ATES&quot; inspections (Annual, VOR, 100 hour, Altimeter, Transponder, ELT, and pitot-Static), as well as oil changes, engine replacement, and registration renewal dates.&nbsp; If you&#39;ve filled in the high-water mark for an aircraft (described above), you can quickly see how close you are getting to non-calendar-driven items like oil changes or engine replacement.</p>

    <h2>Using the club to schedule an aircraft</h2>

    <p>
        To schedule an aircraft, go to “Flying Clubs”
under the “Aircraft” tab:
    </p>
    <p style="text-align:center">
        <img border="0" id="Picture 7" alt="Schedule Aircraft Screen"
            src="ClubsManual_files/image023.jpg" />
    </p>

    <p>
        Each aircraft has its own tab for its schedule, and a
calendar control lets you specify the date(s) to view.  You can choose between
week-view and daily view.
    </p>

    <p>
        Click on the calendar area to create an item on the
schedule:
    </p>
    <p style="text-align:center">
        <img border="0" alt="Create item on schedule"
            width="338" id="Picture 10" src="ClubsManual_files/image015.png" />
    </p>

    <p>Or, click an existing item to edit it or delete it:</p>
    <p style="text-align:center">
        <img border="0" alt="Edit or delete item on schedule"
            width="338" id="Picture 9" src="ClubsManual_files/image016.png" />
    </p>

    <p>
        If you
have upcoming reservations on the schedule, these will also be listed, along
with a link to download it to your personal calendar.
    </p>

    <p>
        You
can access the schedule for an aircraft that is associated with your club in
two other ways:
    </p>

        <ul>
            <li>On the website, if you click on an aircraft to view/edit its
details, a section will show for that aircraft’s schedule.
    </li>
            <li>If you use one of the mobile apps, tap on the aircraft in your
aircraft list and there will be an option to view/edit its schedule.
    </li>
        </ul>

</div>
</asp:Content>

