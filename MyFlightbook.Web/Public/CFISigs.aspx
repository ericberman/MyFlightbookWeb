<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Public_CFISigs" Codebehind="CFISigs.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" runat="Server">1 Endorsements and Signed Flights</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" runat="Server">
    <div>
    <!-- Copyright (c) 2015 MyFlightbook LLC Contact myflightbook-at-gmail.com for more information -->
        <p>A method is needed by which an instructor can sign a student’s logbook, or by which an instructor can endorse a student.   Reference is made to <a href="https://www.faa.gov/regulations_policies/advisory_circulars/index.cfm/go/document.information/documentID/1029747" target="_blank">FAA circular AC No: 120-78A</a>, which outlines the criteria to be used to ensure that such a signature is acceptable to the FAA.</p>
        <p>This document describes the implementation used by MyFlightbook.   It has NOT been vetted by the FAA (indeed, the FAA does not appear to certify compliance); it is a description of how I have chosen to implement my interpretation of the requirements.</p>
        <h1>2 
 Definitions</h1>
        <p>
            <b>CFI</b> = The instructor (&quot;Certified Flight Instructor).  
 The CFI must have a CFI certificate with an expiration date in the future.
        </p>
        <p><b>Student</b> = The pilot wishing to have an entry signed.   A student need not be a not-yet-licensed pilot; the student is ANY pilot who receives instruction from a CFI. </p>
        <p>
            <b>Signed Flight</b> = A flight entry in a student&#39;s logbook which includes CFI comments and a CFI attestation that the training described has taken place during that flight.&nbsp; I.e., this is what the CFI fills out in a paper logbook at the end of the flight.
        </p>
        <p>
            <b>Endorsement</b> = An indication by a CFI that the student has met some required level of accomplishment, fulfilled a periodic requirement, or is otherwise authorized to perform various flying activities.&nbsp; I.e., this is not tied to a particular flight, but rather are the items typically found at the back of a paper logbook, such as a tailwheel endorsement, student-pilot solo authorization, etc.
        </p>
        <p><b>Ad-Hoc </b>= A signature provided when the student and the instructor do not have accounts on MyFlightbook that are linked in a student/instructor relationship.   The signing of a flight is a one-off event, and it is not necessary for the instructor to ever even have an account on MyFlightbook.</p>
        <p><b>Authenticated</b> = A signature where the student and the instructor each have an account on MyFlightbook, and where their accounts are linked in a student/instructor relationship, and where both must authenticate themselves to the system.</p>
        <p><b>Mobile </b>= A signature that is provided face-to-face on the student’s mobile device; e.g., as might happen in the hangar immediately following a flight. Note that it is on the student’s device because it is the student’s logbook that is being signed.</p>
        <p><b>Remote</b> = A signature that is provided asynchronously via the web.   It does not require that the student and CFI be together, nor the presence of a mobile device.   This is ALWAYS authenticated.</p>
        <p><b>Hash</b> = A deterministic unique output of an operation performed on an object which changes if the underlying object changes.   In most applications of a hash, the hash is small relative to the size of the object being hashed, but is highly sensitive to changes in the underlying object.   The key attribute of a hash is that you can compare the hash of an original object and the hash of the new object, and if the new object differs even slightly from the old, the hashes will be different with an exceedingly high probability (or, stated another way, the odds of the underlying objects being different but the hashes being the same is vanishingly small), and hashes are much quicker to compare than entire objects.</p>
        <p><strong>Encryption</strong> = The transformation of data into an encoded form that obscures the underlying data.&nbsp; Encrypted data requires a key to decrypt.</p>
        <p><b>Electronic Signature</b> = <b>CFI Signature</b> = proof that the specified CFI has signed the specified object, and that the object has not been modified since that time.</p>
        <p><b>Digitized Signature </b>= an image of a handwritten signature. Not to be confused with an Electronic Signature.</p>
        <h1>3 Signed Flights</h1>
        <p>A signed flight is an attestation by a CFI that they gave instruction to another individual on a given day.</p>
        <h2>3.1 
 User Flow</h2>
        <p>There are 4 basic scenarios to support.   In descending order of importance, these are:</p>
        <ol>
            <li><b>Student Initiated – Mobile Device, Ad-Hoc:</b>   In this scenario, the student has an account on MyFlightbook and the instructor may or may not have one, but we bypass the establishment of an account/relationship in order to quickly perform an on-the-spot signing.   This is called “Ad-hoc” because no relationship is established.   This might be a typical scenario after a check-out flight at an FBO.   Because we cannot authenticate the CFI in any other way, but the student has a mobile device, a digitized handwritten signature is captured.   This is the only scenario we allow where a digitized handwritten signature is used, because it is the only one where we can assume that the CFI has physical access to the mobile device that is signed in to the student’s account.   </li>
            <li><b>Student Initiated – Mobile Device, Authenticated</b>: In this scenario, the CFI has both a MyFlightbook account AND the student and CFI have established a student/CFI relationship.   This might be a typical scenario for right after an instruction flight during primary training.   Because the instructor is known to MyFlightbook, a digitized handwritten signature is not necessary.   However, the CFI must still authenticate themselves to the device by providing their password.</li>
            <li><b>Student Initiated – Remote, Authenticated. </b>Again, the CFI and Student are both on MyFlightbook and have a relationship, but this addresses the scenario where they are not together but a signature is required.   For example, if they took a training flight and then went home and then the student realizes that the CFI forgot to sign the logbook.   The Student sends a request by email to the CFI to sign the entry; the CFI then authenticates to MyFlightbook and signs the record.   This can also be used to “fix” a signature that is invalidated by an edit to the underlying flight, but for which the CFI agrees that the edit was OK.</li>
            <li><b>CFI Initiated – Remote, Authenticated.</b>The CFI and Student are both on MyFlightbook and have a relationship, and the student has given the CFI permission to view their logbook.   In viewing the logbook, the CFI may choose to sign entries that do not already have valid signatures, or to re-sign ones that have been broken (as above).<b></b></li>
        </ol>
        <p>These scenarios are discussed in more detail below</p>
        <h3>3.1.1 
 Student Initiated – Mobile Device, Ad-Hoc</h3>
        <ol>
            <li>Student OR Instructor fills out and submits flight.</li>
            <li>View flight on mobile device.</li>
            <li>Tap link to sign flight.</li>
            <li>Student positively affirms that the signature that will be captured belongs to the CFI that claims to be signing.</li>
            <li>Flight is displayed for verification, and fields are present for CFI to provide their identifying information, including CFI Certificate, Email, name, and comments.</li>
            <li>CFI signs with a fingertip/stylus.   </li>
            <li>Digitized signature is submitted as part of the signature. The CFIUsername data field is blank, but the DigitizedSignature field is populated with the digitized signature.</li>
        </ol>
        <p>In the current implementation, this is done via web page served (securely) from the MyFlightbook server, and requires authentication from the STUDENT’s account.</p>
        <p>The digitized signature is stored as a <a href="http://en.wikipedia.org/wiki/Portable_Network_Graphics" target="_blank">PNG</a> file with the flight record.</p>
        <h3>3.1.2 Student Initiated – Mobile Device, Authenticated</h3>
        <ol>
            <li>Student OR Instructor fills out and submits flight.</li>
            <li>View flight on mobile device.</li>
            <li>Tap link to sign flight.</li>
            <li>Select the Instructor’s name from a list of linked instructors.</li>
            <li>Flight is displayed for verification, and fields are present for CFI to provide their comments. (Since we know the CFI information, only comments field needs to be filled out; we can provide the rest).</li>
            <li>To verify that it is in fact the CFI who is signing, we require the CFI to enter their password as well.</li>
            <li>Signature is submitted. CFIUsername is captured, but DigitizedSignature is empty.</li>
        </ol>
        <h3>3.1.3 Student Initiated – Remote, Authenticated</h3>
        <ol>
            <li>The student fills out their own entry for their logbook. </li>
            <li>From the web site, the student then requests that the instructor sign the entry
 <ol style="list-style-type: lower-alpha">
     <li>If the instructor is already set up as having an instructor/student relationship with the student, then the system already knows the instructor’s email address.
     </li>
     <li>If instructor is not already set up as an instructor for the student, then the student provides the email address of the instructor.
     </li>
 </ol>
            </li>
            <li>Email is sent to instructor with a link to the entry to be signed.</li>
            <li>The instructor clicks the link, bringing them to the MyFlightbook site
 <ol style="list-style-type: lower-alpha">
     <li>If the instructor does not have an account, they are invited to create one.
     </li>
     <li>If the instructor does not have a student/instructor relationship with the student, they are invited to confirm this relationship.
     </li>
 </ol>
            </li>
            <li>Once the instructor is signed in and has a valid instructor/student relationship with the student, they can see a list of entries for which signatures are pending.</li>
            <li>Instructor clicks on a pending: goes directly to page to sign entry</li>
            <li>After signing, instructor goes to page showing any further signature requests. </li>
        </ol>
        <h3>3.1.4 
   Instructor Initiated, Remote, Authenticated</h3>
        <ol>
            <li>Instructor views student logbook. This only happens if already set up as CFI already, so this avoids many of the above complications.</li>
            <li>Entries that do not have valid signatures include a link to sign</li>
            <li>Instructor clicks on entry and signs it.</li>
        </ol>
        <h2>3.2 
 Data Model</h2>
        <h3>3.2.1 
 Hash of Entry</h3>
        <p>It is important to detect if an entry has been tampered with.   For this reason, we capture a hash that identifies the flight object’s state at the point of the CFI’s signing.   This should be fairly small, so the hash can actually be a clear-text concatenation (which has the advantage that it truly will deterministically catch any change), and there is really no need for encryption, as long as we can assume that the CFI record cannot be modified (which is a business rule that we enforce).</p>
        <p>The hash of the flight object consists of a string that is a concatenation of:</p>
        <ol style="list-style-type: lower-alpha">
            <li>The ID of the flight in the database (this is unique and unchanging).</li>
            <li>The ID of the flight aircraft (id in database, not tail-number)</li>
            <li>The date of the flight ( yyyy -MM- dd form)</li>
            <li>Each of the counts from the core object (landings, night landings, approaches, hold)</li>
            <li>Each of the times from the core object (Cross country, night, simulated instrument, instrument, ground-simulator, dual, CFI, SIC, PIC, and total flight time) </li>
            <li>Each of the properties, consisting of a concatenation of: PropertyID + value (specific value can vary based on type of property).  
PropertyID is used because this will be invariant if name of property changes.   Date must be stored in invariant format (UTC) and numbers in US Culture (period as decimal).   Properties must be sorted by propertyID so that the signature is not invalidated by the order of the properties.</li>
            <li>Category/class override</li>
            <li>Comments from the core object</li>
        </ol>
        <p>For database efficiency, we truncate the hash at 255 characters, which as a practical matter cuts off comments occasionally but rarely.</p>
        <p>Excluded from the signature are values which the student should be allowed to change without invalidating the underlying signature:</p>
        <ul>
            <li>Hobbs values, Engine/Flight times</li>
            <li>Telemetry</li>
            <li>Route of flight (Don’t want to invalidate for changing KJFK to JFK, for example)</li>
            <li>Pictures</li>
            <li>Sharing flags (e.g., are flight details exposed to others) </li>
        </ul>
        <h3>3.2.2 
 Record of CFI Signing</h3>
        <p>When a flight is signed, the following state information is stored with the flight record: </p>
        <ol style="list-style-type: lower-alpha">
            <li>Hash of the flight being signed</li>
            <li>Additional instructor comments.</li>
            <li>Date that the signature was created.   THIS MAY NOT BE THE DATE OF THE FLIGHT BEING SIGNED</li>
            <li>CFI Certificate</li>
            <li>CFI Certificate Expiration Date</li>
            <li>CFI Username (system username) – this links to the account of the CFI, if they have an account.</li>
            <li>CFI Email as of the date that the flight was signed</li>
            <li>CFI Name as of the date that the flight was signed</li>
            <li>Digitized Signature from the CFI, if they don’t have an account (ad hoc scenario)</li>
        </ol>
        <p>The CFI’s name and email can be extrapolated from the account identified by the username.   However, we will keep a cache of the email and name used at the time of signing in case either changes, or in case the account is subsequently deleted, since a valid signature does not stop being valid when the CFI’s account goes away. </p>
        <p>The Student’s name and email can be extrapolated from the linked flight.</p>
        <p>A flight can only be signed once and by one instructor.   Any new signature will overwrite the old one.</p>
        <h3>3.2.3 
 Signature State</h3>
        <p>A flight signature can be in one of 3 states:</p>
        <ul>
            <li><b>None</b>: The flight is not signed.   A flight must have a flight hash, a CFI certificate, a CFI Expiration date, and (a CFI username OR a digitized signature) to be considered signed; if it is missing any of these, it is considered unsigned.</li>
            <li><b>Valid</b>: The flight is signed and the stored flight hash matches the real-time computed hash of the flight.</li>
            <li><b>Invalid</b>: The flight is signed, but the stored hash does NOT match the real-time computed hash of the flight.</li>
        </ul>
        <p>Note that in computing validity, the presence of a certificate expiration date is all that is required, the actual date of the expiration is not relevant for the validity of the signature.   See the discussion below of enforcement for how we ensure that only unexpired certificates are allowed for signing.</p>
        <h2>3.3 
 Enforcement</h2>
        <p>A CFI is only allowed to sign student flights if they have provided in their profile:</p>
        <ul>
            <li>A certificate number (which is currently an unverified string of text)</li>
            <li>An expiration date that is in the future.</li>
        </ul>
        <p>Due to potential time-zone issues between the time zone of the MyFlightbook server (currently Pacific time zone) and where the CFI may be, we allow a 24-hour grace period before deeming an expiration date to have passed.</p>
        <p>A CFI is only allowed to sign a specific flight if:</p>
        <ul>
            <li>The student hands their signed-in logbook (on a mobile device) to the instructor.&nbsp; I.e., this is the Ad-hoc scenario, and would presumably require the student and instructor to be in each other&#39;s presence.</li>
            <li>OR The flight belongs to a student with whom they have an instructor/student relationship established and the student has EITHER granted permission for the instructor to view their entire logbook (likely more common for a primary student than for an experienced pilot getting a checkout or flight review) OR has specifically requested that that flight be signed.</li>
        </ul>
        <p>Note that the student/instructor relationship only need live for the duration of the signing process.   Once an entry is signed, the relationship can be severed without affecting the validity of the existing signature.</p>
        <h2>3.4 
 Requirements per FAA Advisory AC120-78A</h2>
        <p>AC120-78A has the following attributes for electronic signatures, paraphrased here:</p>
        <table border="1" style="border-collapse: collapse" cellspacing="0" cellpadding="3">
            <tr valign="top" style="color: #FFFFFF; font-weight: bold; background-color: #666666">
                <td >Attribute</td>
                <td>Paraphrase of Requirement</td>
                <td>How this design meets this</td>
            </tr>
            <tr>
                <td valign="top" style="font-weight: bold;">a) Uniqueness</td>
                <td valign="top">Only the person to whom the signature is attributed can be the source.   I.e., can’t
spoof/impersonate
 
                </td>
                <td valign="top">The only way to create a signature is to sign in to
the CFI’s account.   Since that’s
already a strong form of authentication for everything else in the system, it
should work here too.   This is actually
harder to spoof than a physical signature is to forge.   One could theoretically create a 2<sup>nd</sup>
                    account on MyFlightbook and use that to sign with, but that’s really no
different than simply signing your own logbook and using a made-up (or
stolen) CFI # and certificate.
 
                </td>
            </tr>
            <tr>
                <td valign="top" style="font-weight: bold;">b) Control</td>
                <td valign="top" >The signer must be the only person who controls the signature</td>
                <td valign="top" >An electronic signature requires the signer to authenticate with a username/password.&nbsp; A digitized handwritten signature is something only the signer can produce.</td>
            </tr>
            <tr>
                <td valign="top" style="font-weight: bold;">c) Notification</td>
                <td valign="top" >The system should notify the signatory that the signature has been affixed.</td>
                <td valign="top" >The success or failure of the signature is reported immediately.</td>
            </tr>
            <tr>
                <td valign="top" style="font-weight: bold;">d) Intent to sign</td>
                <td valign="top" >The signatory should be prompted to ensure that they realize they are signing using one of several key phrases, such as &quot;Signed by&quot;.</td>
                <td valign="top" >The user interface uses the phrase &quot;Signed by&quot;</td>
            </tr>
            <tr>
                <td valign="top" style="font-weight: bold;">e) Deliberate</td>
                <td valign="top" >It must be a proactive step to sign it; it cannot be implied, or a
side-effect of another action.</td>
                <td valign="top" >The signature must be requested by the student, or optionally
explicitly provided by the instructor.  
 There is no way it is a side effect of some other action.
 
                </td>
            </tr>
            <tr>
                <td valign="top"  style="font-weight: bold;">f) Signature association</td>
                <td valign="top" >It must be clear what the scope/significance of the
signature is
 
                    and the signature must be attached to the record.</td>
                <td valign="top" >Scope is always a single flight, nothing more. (Same
applies to existing endorsements as well), and the signature is stored as an integral part of the flight record.</td>
            </tr>
            <tr>
                <td valign="top"  style="font-weight: bold;">g) Retrievable and traceable</td>
                <td valign="top">There should be a way to trace back to the individual who signed, and provide a way to identify records that they have signed.</td>
                <td valign="top">The account name (for electronic signatures), full name, CFI Certificate/expiration, and email (as of the time of signing) are all captured, providing multiple paths to the signer.&nbsp; The signer can also view any electronic endorsements that they have issued.&nbsp; As of this writing, the CFI cannot see all flights that they have signed, though; but this is no different from a paper logbook, where it is impossible to access signed flights without getting the logbook from the student.</td>
            </tr>
            <tr>
                <td valign="top" style="font-weight: bold;">h) Undeniable
 
                

                </td>
                <td valign="top">If your signature is shown, you can’t say “it wasn’t
me.”
 
                </td>
                <td valign="top">Only the authenticated user described above can
sign, and the hash comparison will only work for that CFI.   Specifically, we can prove that the
signature came from someone with access to a specific second account.   The hash is also encrypted using the
username of the CFI, so any attempt to change this back-trace will invalidate
the signature.
 
 
 In the ad-hoc scenario, the signature provides
non-repudiation at least as good as a handwritten signature (similar to what
grocery store credit-card check-out tablets capture)
 
                </td>
            </tr>
            <tr>
                <td valign="top"  style="font-weight: bold;">i) 
 Security 


                </td>
                <td valign="top">Tampering must be evident and the signature voided when tampering
occurs, impersonation (again) must not be possible, it should not be possible
to do after authority lapses (e.g., student/CFI relationship is broken or CFI
certificate expires)
 
                </td>
                <td valign="top">No mechanism is provided for modifying a signature; the only option
is to re-sign.   The stored hash of the
flight in the state when it was signed ensures that the signature is treated
as invalid if the underlying flight is modified.
 
                </td>
            </tr>
            <tr>
                <td valign="top"  style="font-weight: bold;">j) Permanent and unalterable</td>
                <td valign="top">The signature must be a permanent part of the record, and altering the data should require a new signature</td>
                <td valign="top">The signature is stored with the flight record and any edit to the underlying object invalidates the signature.&nbsp; There are some aspects of the underlying flight which are considered &quot;incidental&quot; and excluded from the set of invalidating edits, such as images or flight telemetry (as described above); this is a deliberate judgment that the signature should apply to the &quot;core&quot; attributes of the flight.</td>
            </tr>
            <tr>
                <td valign="top" style="font-weight: bold;">k) Identification and Authentication
 
                

                </td>
                <td valign="top">Need to be able to uniquely identify the signer.</td>
                <td valign="top">The signature includes the CFI # and expiration along with other
identifying details, such as email address.&nbsp; Uniqueness is determined either by the signer providing their username/password, or by providing their handwritten signature</td>
            </tr>
            <tr>
                <td valign="top"  style="font-weight: bold;">l) Correctable</td>
                <td valign="top">There should be a means for the pilot to edit the underlying flight and to get a new signature.
                </td>
                <td valign="top">Any update to the core (signed) fields of a flight immediately invalidates the signature, and this is displayed to the user.&nbsp; A new signature can be subsequently requested.&nbsp; As a bonus, if the core data is restored to the state at which it was signed, the signature will be re-validated (since the hash will match again).</td>
            </tr>
            <tr>
                <td valign="top"  style="font-weight: bold;">m) Archivable</td>
                <td valign="top">There must be a means of archiving the signed flight</td>
                <td valign="top">Since the signature is part of the flight object, it is automatically backed up as part of the nightly archiving process.</td>
            </tr>
            <tr>
                <td valign="top"  style="font-weight: bold;">n) Control of private keys and access codes</td>
                <td valign="top">Basic security - don&#39;t expose passwords or private keys.</td>
                <td valign="top">The keys for the encrypted signature are not exposed anywhere, and in fact, neither are the signature or the flight hash themselves.</td>
            </tr>
        </table>
        <h3>3.4.1 
 Potential holes</h3>
        <p>There are a few potential holes here, the risk of each of which is acceptable:</p>
        <table border="1" style="border-collapse: collapse" cellspacing="0" cellpadding="3">
            <tr valign="top" style="color: #FFFFFF; font-weight: bold; background-color: #666666">
                <td valign="top">Risk 
                </td>
                <td valign="top">Mitigation 
                </td>
            </tr>
            <tr>
                <td valign="top">We are
authenticating accounts, not people.  
 If one account is used to sign on behalf of another, we can know that
they are distinct accounts with distinct email addresses, but we cannot
actually know anything about the people who created the accounts beyond
that.   It is possible for a student to
create two accounts and use one as a “CFI” account.   
 
                </td>
                <td valign="top">The issue here is not a hole in the system so much
as it is fraud on the part of the user.  
 Also, the only way to truly get around this is to have some additional
cryptographic means of ensuring that the two account holders are in fact
distinct people (e.g., distinct authentications from a trusted 3<sup>rd</sup>
                    party, such as validating certificates against FAA and other
national/international databases), and that is beyond the scope of what is
practical.
 
                </td>
            </tr>
            <tr>
                <td valign="top">If someone
has access to another person’s account (e.g., walk up to their laptop which
is signed in), they can use it
 
                </td>
                <td valign="top">Again, this isn’t so much a hole in the system as it is poor security
on the part of the user: if you leave your bank web page up and logged in, a
passerby could embezzle money from their account.   This level of risk is acceptable
 
                </td>
            </tr>
            <tr>
                <td valign="top">If the user
can gain direct access to the database, one can spoof another CFI providing
the signature.
 
                </td>
                <td valign="top">We treat database security very seriously.   If it is compromised, lots of mischief can
occur.   However, we encrypt the flight
hash so that it is (essentially) impossible to avoid detection of a
modification of a flight, and we create an encryption of the signature
details (CFI username, expiration date, certificate, etc.) so that we can
detect modification of those as well.&nbsp; The encryption happens outside of the database, so even if someone were to gain access to the database, they could not produce a properly encrypted signature.</td>
            </tr>
        </table>
        <h3>3.4.2 
 Verification of identity</h3>
        <p>AC120-78A defines Digital Signatures as employing cryptographically generated data and using public/private key technologies to avoid spoofing, etc. </p>
        <p>The purpose of a cryptographic signature is really twofold: (a) it authenticates the fact that the signer actually signed it, and (b) it proves that the thing being signed has not been modified.   The purpose to using cryptography and public/private keys is to ensure that the signature itself is not tampered with.   </p>
        <p>For example, in a digitally signed email message, the signature enables proof that the message has not been tampered with, and the signature can also only be produced by the sender, which validates that they produced it.   (The message itself, in this scenario, is kept in the clear.)</p>
        <p>The only way to have cryptographically secure identification of a person’s identity is to have an out-of-band certifying authority that validates the identity (e.g., a passport agency, or the FAA (or one of its international equivalent), or driver’s license, etc.) and then issues secure credentials that they vouch for. This is out of scope for the MyFlightbook system.</p>
        <p>While we cannot validate that an account holder is who they say we are , we can verify a few things:</p>
        <ol style="list-style-type: lower-alpha">
            <li>The actions taken on behalf of the account holder were authorized by that account holder (by virtue of the fact that only that account can sign things)</li>
            <li>The CFI# and Expiration form a (weak) sort of username/password.</li>
            <li>We can ensure with a reasonable degree of confidence that the account holder is in fact the owner of the email address by sending email that requires a response to that account.   Since email is not secure, this is not perfect, but it works pretty well.</li>
            <li>We can also store a digitization of a handwritten signature.   This is not MyFlightbook vouching for the identity of the signer, but it enables others to evaluate it and judge for themselves (see below).   Ironically, this does not seem to meet the requirements of AC120-78 (due to the lack of non-repudiation and cryptography), yet it provides no less authentication than a paper logbook. </li>
        </ol>
        <h3>3.4.3 
 Verification of integrity. </h3>
        <p>MyFlightbook performs two integrity checks to detect tampering with or forging of the signature:</p>
        <ol style="list-style-type: lower-alpha">
            <li>The hash of the salient flight details (as described above) is stored in an encrypted form, and validity checks are made by comparing an encrypted hash to this stored encrypted hash. &nbsp; This makes tampering with the flight hash hard to do and easy to detect. &nbsp; We also never need to bother decrypting it, since we can compare a newly computed/encrypted hash with the encrypted hash stored in the database.</li>
            <li>A hash is also provided of the salient signature details (CFI username, CFI Certificate, CFI Expiration date, and date of signature) so that we can detect modifications there; this provides the integrity that the system itself did in fact sign the user’s entry.&nbsp; Like the hash of the flight details, this is also stored in encrypted form, and like its flight counterpart, we never need to decrypt it.</li>
        </ol>
        <p>
            So, if a malicious user were to gain access to the database, they could not forge a signature because they could not store a propertly compute and encrypted hash that would match subsequent comparisons.&nbsp; Nor could they modify an existing signature for the same reason.</p>
        <h2>3.5 
 Handwritten signatures</h2>
        <p>With the proliferation of touch-sensitive smartphones and tablets (particularly among pilots), and the fact that the student and pilot are together at the end of a lesson, the convenience of capturing a digitized signature right then in there is certainly much greater than forcing the CFI to both create a MyFlightbook account and to set up a relationship with the student.   This is why we have the “Ad-hoc” scenario described above.</p>
        <p>AC120-78A does allow for digitized handwritten signatures, and MyFlightbook supports this functionality.</p>
        <p>To do that, however, requires addressing the non-repudiation and traceability issues above.   An image of a signature is easily reproduced, and also easily forged.   Stated another way, the FAA circular requires some way to authenticate that the signer is the signer.   (Ironically, this is a tougher requirement than for paper signatures.)   </p>
        <p>
            The authenticated methods above provide this: the signing MUST come from a SPECIFIC, SEPARATE, AUTHENTICATED account.   Capturing a digitized handwritten signature provides no way to determine that it wasn’t from the student himself.   However, since it is done via the student’s account and in their presence (since it is via their device and signed in to their account), it CAN prove that the student accepted the signature.   For this reason, we require the student to positively affirm that it is the instructor who is signing.  
(Sort of the student-signing-the-instructor-signing-the-student.)
        </p>
        <p>Nevertheless, a digitized signature is a less traceable mechanism and is easier to repudiate than the non-signature model, but it does provide a lightweight alternative.</p>
        <h2>3.6 
 Additional issues/scenarios:</h2>
        <h3>Instructor is not on MyFlightbook at all</h3>
        <p>
            The ad-hoc scenario is designed for this.&nbsp; It does require the use of the mobile app (for touch-screen access) and physical presence.</p>
        <h3>Instructor is on MyFlightbook but not set up as student’s CFI</h3>
        <p>
            A simple email exchange can establish the relationship.&nbsp; Either party may initiate, using the website.&nbsp; An email is sent to the non-initiating party, which includes a link; following the link goes back to the website, where the account is authenticated (and created, if needed) and the relationship can be accepted.</p>
        <h3>Flight is deleted before signature happens.</h3>
        <p>
            This would be a very rare race condition because signing can only happen to an extant flight.&nbsp; E.g., you&#39;d have to start the signing process in one place and then delete the underlying flight after the signing process has started but before it has finished.&nbsp; But even then, this would be caught: because the signature is a part of the flight record (as opposed to a separate record that is tied to the flight), the update of the flight would fail since the original flight would no longer be in the database.</p>
        <h3>Instructor doesn’t have CFI certificate # or expiration, or has expired</h3>
        <p>
            This instructor cannot sign.&nbsp; By design.</p>
        <h1>4 
 Instructor endorsements for students</h1>
        <h2>4.1 
 Overview</h2>
        <p>An endorsement is a certification by the instructor that the student is competent and (as appropriate) authorized for certain flights.   Typical examples of endorsements include solo operations, tailwheel sign-off, readiness for a check-ride, etc.</p>
        <p>Endorsements are not tied to an individual flight (which is why they are in the back of the book for paper logbooks), and as such are much simpler than signed flights discussed above.   (For this reason, MyFlightbook implemented them much earlier than signed flights).</p>
        <h2>4.2 
 User Flow</h2>
        <p>Endorsements largely follow the same model as for signing flights (above), but with a few small differences:</p>
        <ol>
            <li><b>Student Initiated – Mobile Device, Ad-Hoc:</b>   In this scenario, the student has an account on MyFlightbook and the instructor may or may not have one, but we bypass the establishment of an account/relationship in order to quickly issue an endorsement on-the-spot.   This is called “Ad-hoc” because no relationship is established.   Because we cannot authenticate the CFI in any other way, but the student has a mobile device, a digitized handwritten signature is captured.   This is the only scenario we allow where a digitized handwritten signature is used, because it is the only one where we can assume that the CFI has physical access to the mobile device that is signed in to the student’s account.   </li>
            <li><b>Student Initiated – Mobile Device, Authenticated</b>: In this scenario, the CFI has both a MyFlightbook account AND the student and CFI have established a student/CFI relationship.   This might be a typical scenario for primary flight training.   Because the instructor is known to MyFlightbook, a digitized handwritten signature is not necessary.   However, the CFI must still authenticate themselves to the device by providing their password.</li>
            <li><b>CFI Initiated – Remote, Authenticated.</b>The CFI and Student are both on MyFlightbook and have a relationship.   The CFI may choose, from within their own account, to issue an endorsement directly to a student.</li>
            <li><strong>CFI Initiated - Offline Student.</strong>&nbsp; This is more of a record keeping task than it is an official endorsement.&nbsp; The CFI creates a record of an endorsement given to a student that is not on MyFlightbook.&nbsp; This is entirely for recordkeeping within the CFI&#39;s account; the student receives nothing from this process.&nbsp; As such, this is not an &quot;official&quot; endorsement.</li>
        </ol>
        <p>Endorsements cannot be deleted or edited, and since they are not validating an underlying object such as a flight, there is no need to even encrypt them or compute a hash, and there is no notion of endorsement becoming invalid.</p>
        <p>In 2018, a few validations were relaxed:</p>
        <ul>
            <li>Backdating of endorsements is now allowed, under the premise that transparency makes such practice clear.&nbsp; The date of issue is captured, which can be backdated, as is the date of creation, which cannot.&nbsp; When these two dates differ, both are clearly displayed, so that there is no ambiguity about what happened.&nbsp; E.g., if an endorsement is issued on August 8 2015 with a date of August 8 2015, then simply &quot;August 8&quot; is shown.&nbsp; But if an endorsement is issued on May 5 2017 with a date of Aug 8 2015, then both the date of the endorsement (August 5 2015) and the date of creation (May 5 2017) are displayed</li>
            <li>Since Ground Instructor certificates do not expire and Ground Instructors can issue some endorsements, an expiration date is no longer strictly required.&nbsp; If present, though, it must be later than the date of the endorsement.&nbsp; E.g., for an endorsement with an effective date of August 5 2015, the certificate expiration must either not be present, or must be on or after August 5 2015. </li>
        </ul>
        <h2>4.3 
 Data Model</h2>
        <p>Each endorsement consists of the following:</p>
        <ul>
            <li>A template (form) which must be filled out</li>
            <li>The name of the student</li>
            <li>The name of the instructor</li>
            <li>Either the username (in the database) of the instructor, if it is digitally issued, or a digitized scribble-signature.</li>
            <li>The Instructor’s CFI (or other appropriate certificate) number (cannot be empty)</li>
            <li>The instructor’s CFI Expiration (must be after the date of the endorsement, if present; some certificates such as ground instructor, allow for issuance of endorsements, but do not expire)</li>
            <li>The date that the endorsement is given</li>
            <li>The date that the endorsement is created</li>
            <li>(Optional) the relevant FAR for the endorsement.</li>
        </ul>
        <h2>4.4 
 Other considerations</h2>
        <p>All of the other discussion above concerning signatures applies here as well.</p>
        <p>To be clear, an endorsement on MyFlightbook technically does not mean “CFI A endorsed student B”, but rather “The person who has authenticated access to account A and the person who has authenticated access to account B have identified A as being a CFI for B, and has endorsed B”</p>
        <h1>5 Revision History</h1>
        <ul>
            <li>2012-12: Initial draft</li>
            <li>2014-01: Reformatting, fix a few typos, added some definitions</li>
            <li>2014-03: Added a bit more clarity around encryption of hashes/tamper detection.</li>
            <li>2016-10: Updated to reference AC120-78A (previously had been AC120-78).</li>
            <li>2018-08: Updated to reference endorsement updates</li>
        </ul>
        </div>
</asp:Content>
