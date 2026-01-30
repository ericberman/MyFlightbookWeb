# MyFlightbookWeb
The website and services for MyFlightbook. This provides the back-end for the mobile apps as well.

 ## Getting Started
 ### Setting up the website
 * Run on any Windows machine with ASP.NET 4.5 or later.
 * Make sure IIS has ASP turned on under "application development features".
 * Make sure IIS has HTTP Redirection turned on under Internet Information Services/World Wide Web Services/Common HTTP Features
 * Create 6 folders under "Images": "Aircraft", "BasicMed", "Endorsements", "OfflineEndorsements", "Flights", and "Telemetry".  Set permissions on them so that Network Service has full control (so that the website can write thumbnails to these folders).  
 NOTE: Visual studio debugging can get very slow if these contain a lot of files/folders, so it's a good idea to mark them as hidden in the file system (top level only is sufficient).
 * Add the following web.config to the Telemetry folder created above, so that it can't serve telemetry directly:
 ~~~~
 <?xml version="1.0"?>
<configuration>
  <system.webServer>
    <authorization>
      <deny users="?" />
      <deny users="*"/>
    </authorization>
  </system.webServer>
</configuration>
 ~~~~
 * Set up the virtual directory for "logbook" pointing to your working directory, convert it to an application. Use ASP.NET 4.5 or later as your application pool. A lot of items point to /logbook, so the root should be the parent folder
and the application should be called "logbook" and point to the /logbook branch.
 * Set up a certificate to enable https.
 * Make sure that IIS is set up to serve .KML, .GPX, .PDF, .JPG, .DOCX, .APK (application/vnd.android.package-archive) etc.
 * Add email.config to App_Data so that you can send email:
~~~~
<?xml version="1.0"?>
<smtp deliveryMethod="Network" from="(your email address)">
    <network defaultCredentials="false" port="587" host="..." userName="..." password="..." />
</smtp>
~~~~
 * Add connection.config to App_Data so that you can talk to the database:
 ~~~~
 <?xml version="1.0"?>
  <connectionStrings>
    <add name="logbookConnectionString" connectionString="server=...;User Id=...;password=...;Persist Security Info=false;database=logbook;CharSet=utf8mb4;Pooling=false" providerName="MySql.Data.MySqlClient" />
  </connectionStrings>
~~~~
 * Review Packages.config and install the requisite products/DLLs (typically via NuGet) into the Bin directory.
 
 ### Setting up the database
 * Install MySQL and import "MinimalDB-xxxx-xx-xx.sql" (in the Support folder), then apply any/all scripts in that folder that are after that date.  The scripts all assue that your database schema is "logbook" (see connection.config above), but you can use any connection you like; just be sure to edit the update scripts if you use other than "logbook".
 * Populate the LocalConfig table with values for the relevant keys.  LocalConfig is for keys and secrets (e.g., oAuth access/secret pairs) that one doesn't want to have in the code, and this is necessary for mapping, social media, etc. to work.  A development site should work without most of these, but will have degraded functionality.  See below for LocalConfig settings and what they mean
 * Need to set packet size to at least 10-15MB:	show variables like 'max_allowed_packet';	SET GLOBAL max_allowed_packet=16777216.
 * Not a bad idea to bump up group_concat_max_len to something like 2048.
 * Depending on where MySQL is hosted, may need to set  lower_case_table_names=1, since the code is not consistent about upper/lower case for table names.
 * If on 5.7, may need any of the following: 
 * sql_mode ALLOW_INVALID_DATES
 
 #### LocalConfig settings
 * AdminAuthAccessKey - Enables use of certain admin-only functionality (provides an encrytion seed)
 * AuthorizedWebServiceClients - Comma separated list of authorized clients of the web services (i.e., the iOS and Android apps)
 * AWSAccessKey - Access key for Amazon Web services
 * AWSMediaConvertRoleArn - Arn for converting media (video) files on AWS
 * AWSSecretKey - Secret for Amazon Web Services
 * BoxClientID - oAuth ID for Box.com
 * BoxClientSecret - oAuth secret for Box.com
 * CloudAhoyID - oAuth ID for CloudAhoy oAuth
 * CloudAhoySecret - oAuth secret for CloudAhoy oAuth
 * DebugDomains - Identifies local domains (e.g., http://localhost) from which oAuth requests may originate
 * DropboxAccessID - Access key for Dropbox
 * DropboxClientSecret - Secret for Dropbox
 * ETSPipelineID - Amazon Elastic Transcoder ID for processing videos
 * ETSPipelineIDDebug - Amazon Elastic Transcoder ID for processing videos from a debug/development website
 * ETSPipelineIDStaging - Amazon Elastic Transcoder ID for processing videos from staging website
 * FacebookAccessID - Access key for Facebook (Obsolete)
 * facebookAppId - ID to link to the MyFlightbook Facebook page
 * FacebookClientSecret - Secret for Facebook (Obsolete)
 * FlightCrewViewClientID - oAuth clientID for interacting with FlightCrewView
 * FlightCrewViewClientSecret - oAuth secret for interacting with FlightCrewView
 * FlyStoAccessID - oAuth clientID for interacting with FlySto
 * FlyStoClientSecret - oAuth secret for interacting with FlySto
 * GoogleAdClient - ID for google adsense ads (shown only on select public pages)
 * GoogleAdHorizontalSlot - ID for adsense horizontal ads
 * GoogleAdVerticalSlot - ID for adsense vertical ads
 * GoogleAnalyticsDeveloper - ID for google analytics on developer machines (Obsolete)
 * GoogleAnalyticsProduction - ID for google analytics on production environment. (Obsolete)
 * GoogleAnalyticsGA4Developer - ID for current Google Analytics GA4 on developer machine
 * GoogleAnalyticsGA4Production - ID for current Google Analytics GA4 on production environment
 * GoogleDriveAccessID - oAuth ID for Google Drive
 * GoogleDriveClientSecret - oAuth secret for Google Drive
 * GoogleMapID - ID for google maps (required to use AdvancedMarkerElement and recaptcha)
 * GoogleMapsKey - Key for using Google maps. Get your own
 * GooglePlusAccessID - Access key for Google Plus (Obsolete)
 * GooglePlusAPIKey - API key for Google Plus (Obsolete)
 * GooglePlusClientSecret - Secret for Google Plus (Obsolete)
 * GroundSchoolDiscountLink - promotion text (in markup) for a donation tier
 * LeonClientID - oAuth ID for Leon Scheduling System
 * LeonClientSecret - oAuth secret for Leon Scheduling System
 * OneDriveAccessID - oAuth ID for OneDrive
 * OneDriveClientSecret - oAuth secret for OneDrive
 * PeerRequestEncryptorKey - Key used for encrypting/decrypting requests between peer users
 * rbClientID - oAuth ID for RosterBuster (production)
 * rbClientDIDDev - oAuth ID for RosterBuster (development)
 * recaptchaKey - ID for using Google's recaptcha
 * recaptchValidateEndpoint - the url for validating recaptcha
 * SharedDataEncryptorKey - Key used to encrypt access to data being shared with the world
 * ShuntState - "shunted" to shunt the site, otherwise empty
 * ShuntMessage - the message to display while the site is shunted.
 * StripeLiveKey - key for interacting with stripe in the live environment
 * StripeLiveWebhook - webhook for receiving notifications in the live environment from Stripe
 * StripeTestKey - key for interacting with stripe in the development environment
 * StripeTestWebhook - webhook for receiving notifications in the development environment from Stripe
 * TwitterAccessID - oAuth ID for Twitter (obsolete)
 * TwitterClientSecret - Secret for Twitter (Obsolete)
 * UseAWSS3 - Set to "yes" to migrate images to S3. Best to leave this "no" for local debugging
 * UseOOF - set to "yes" to auto-respond to contact-me requests with an out-of-office message.
 * UserAccessEncryptorKey - Used to share flights
 * UserPasswordHashKey - Hash key used when storing hashed passwords in the database
 * WebAccessEncryptorKey - key to encrypt/decrypt authorizations on the web service
 * wkhtmlpath - file system full ppatname to where wkhtmltopdf.exe


The following are for directories where images/pdfs/videos are stored:
 * AircraftPixDir - app-relative (e.g., "~/...") path for aircraft pictures.
 * FlightsPixDir - app-relative (e.g., "~/...") path for flight pictures.
 * BasicMedDir - app-relative (e.g., "~/...") path for basicmed pictures.
 * TelemetryDir - app-relative (e.g., "~/...") path for telemetry.
 * EndorsementsPixDir - app-relative (e.g., "~/...") path for endorsement pictures.
 * OfflineEndorsementsPixDir - app-relative (e.g., "~/...") path for offline endorsements.

 ### Additional items
* Install [WKHtmlToPdf](http://wkhtmltopdf.org/) to create PDFs. Download link is [here](https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6-1/wkhtmltox-0.12.6-1.msvc2015-win64.exe)

### Live Site Only
These are tasks to do ONLY on the live site; there should be no need to set them up on a development environment:
* Set up a scheduled task to send nightly stats, delete drop-box cache, send nightly email.
* Install root files (support directory) so that http://xxx/ will go straight to the default home page, and so that favicon.ico will work. Need to edit the file and ensure that default.aspx is the top-level default doc.
* Ensure reverse DNS is set up for host so that email can be received
* Set up custom errors in web.config, but TURN THEM OFF in IIS so that oAuth will return errors. IIS->(site)->Error Pages->Edit Feature Settings->Detailed Errors.
* Use a custom application pool.  Then, in IIS Manager go to that pool and under "Edit Application Pool", click "Recycling...".  Set the pool to automatically recycle once daily (I use 10pm) and if it uses too much memory (I use 2000000KB - i.e., 2GB)
* Ensure that 3306 (database) port is closed, ensure that firewall is otherwise appropriately configured

## Additional attributions and licenses
This source code is provided under the GNU license, but it incorporates other code as well from a variety of other sources, and each such work is covered by its respective license. This includes, but is not limited to:
 * [Ourairports](https://github.com/davidmegginson/ourairports-data) - Open database of worldwide airports ("Unlicense")
 * [DayPilot](https://javascript.daypilot.org/) Web-based calendar code for club aircraft scheduling (Apache license)
 * [CSV utilities](http://www.heikniemi.fi/jhlib/) Read/write CSV data (free/unlicensed)
 * [EXIF utilities](https://www.codeproject.com/Articles/7888/A-library-to-simplify-access-to-image-metadata) Read/write metadata for images. (free/unlicensed)
 * oAuth1 support for twitter (source forgotten, but source code provided)
 * [DotNetOpenAuth](http://dotnetopenauth.net/) Provides oAuth2 support (both client and server). (MS-Pl License)
 * Celestial code (for computing day/night) is adapted from NOAA code at http://www.srrb.noaa.gov/highlights/sunrise/sunrise.html and http://www.srrb.noaa.gov/highlights/sunrise/calcdetails.html
 * [Membership and Role management](https://www.codeproject.com/Articles/12301/Membership-and-Role-providers-for-MySQL) Provides account management and role-based security for MySQL (free/unlicensed)
 * [wkHtmlTox](https://wkhtmltopdf.org/) Renders HTML to PDF documents. (LGPLv3, but I'm not redistributing any code so I *think* I don't need to include the license in the source code directly...)
 * [SecuritySwitch](https://www.nuget.org/packages/SecuritySwitch/4.4.0) Enables declaration of which pages must be HTTP or HTTPS (New BSD license)
 * [Endless Scroll](https://github.com/fredwu/jquery-endless-scroll) Enables "endless scroll" functionality in web browsers (MIT/GPL license)
 * [JQuery](http://jquery.org) Javascript support library for a bunch of other functionality (MIT License)
 * [OverlappingMarkerSpiderfier](https://github.com/jawj/OverlappingMarkerSpiderfier) Enables cleaner handling of overlapping markers on Google Maps - click on one of the overlapping markers, and they spread out like spider-legs, so you can click on the one you want. (MIT License)
 * [Scribble-signature control](https://www.codeproject.com/Articles/432675/Building-a-Signature-Control-Using-Canvas) - Enables fingernail scribbling for signing flights using HTML 5 ([Code Project Open License](http://www.codeproject.com/info/cpol10.aspx))
 * [Todataurl-png.js](http://code.google.com/p/todataurl-png-js/) Converts a bitmap into a data URL which can be posted in a form. ("Other" open source license, but code is unmodified)
 * [DotNetZip](https://dotnetzip.codeplex.com/) – Microsoft Public License
 * [BxSlider](http://bxslider.com/) - Enables smooth slideshows of images. (MIT License); includes jquery.fitvids.js which is [WTFPL license](http://sam.zoy.org/wtfpl/)
 * [ImageMagick](https://github.com/dlemstra/Magick.NET) - Enables support for HEIC (Apache License)
 * Numerous binary libraries (via NuGet), including Ajax libraries, iCal libraries, Zip, AWS, DropBox, OneDrive, etc.  Current packages
 
