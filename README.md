# MyFlightbookWeb
The website and service for MyFlightbook.  This provides the back-end for the mobile apps as well.

 ## Getting Started
 ### Setting up the website
 * Run on any Windows machine with ASP.NET 4.5 or later.
 *Make sure IIS has ASP turned on under "application development features"
 * Create 5 folders under "Images": "Aircraft", "BasicMed", "Endorsements", "Flights", and "Telemetry".  Set permissions on them so that Network Service has full control (so that the website can write thumbnails to these folders).  
 NOTE: Visual studio debugging can get very slow if these contain a lot of files/folders, so it's a good idea to mark them as hidden in the file system (top level only is sufficient).
* Set up the virtual directory for "logbook" pointing to your working directory, convert it to an application.  Use ASP.NET 4.5 or later as your application pool.  A lot of items point to /logbook, so the root should be the parent folder
and the application should be called "logbook" and point to the /logbook branch.
* Set up a certificate to enable https.
* Make sure that IIS is set up to serve .KML, .GPX, .PDF, .JPG, .DOCX, .APK (application/vnd.android.package-archive) etc.
 * Add debug.config so that you can turn debug mode on/off locally:
 ~~~~
 <?xml version="1.0"?>
<!-- 
            Set compilation debug="true" to insert debugging 
            symbols into the compiled page. Because this 
            affects performance, set this value to true only 
            during development.
        -->
<compilation defaultLanguage="c#" debug="true" targetFramework="4.6.1">
  <assemblies>
    <add assembly="System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B03F5F7F11D50A3A"/>
    <add assembly="System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B77A5C561934E089"/>
    <add assembly="System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B77A5C561934E089"/>
    <add assembly="System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31BF3856AD364E35"/>
    <add assembly="System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B77A5C561934E089"/>
    <add assembly="System.Data.DataSetExtensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B77A5C561934E089"/>
    <add assembly="System.Net, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B03F5F7F11D50A3A" />
    <add assembly="System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B03F5F7F11D50A3A" />
    <add assembly="System.Net.Http.WebRequest, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B03F5F7F11D50A3A" />
    <add assembly="Microsoft.Web.Infrastructure, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31BF3856AD364E35" />
    <add assembly="System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" />
    <add assembly="System.Threading.Tasks, Version=1.5.11.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" />
    <add assembly="System.IO, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" />
    <add assembly="System.ComponentModel.Composition, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B77A5C561934E089" />
  </assemblies>
</compilation>
~~~~
 * Add email.config so that you can send email:
~~~~
<?xml version="1.0"?>
<smtp deliveryMethod="Network" from="(your email address)">
    <network defaultCredentials="false" port="587" host="..." userName="..." password="..." />
</smtp>
~~~~
 * Add connection.config so that you can talk to the database:
 ~~~~
 <?xml version="1.0"?>
  <connectionStrings>
    <add name="logbookConnectionString" connectionString="server=...;User Id=...;password=...;Persist Security Info=false;database=logbook;Pooling=false" providerName="MySql.Data.MySqlClient" />
  </connectionStrings>
~~~~
 * Review Packages.config and install the requisite products/DLLs (typically via NuGet) into the Bin directory.
 
 ### Setting up the database
 * Install MySQL and import "MinimalDB-xxxx-xx-xx.sql" (in the Support folder), then apply any/all scripts in that folder that are after that date.  The scripts all assue that your database schema is "logbook" (see connection.config above), but you can use any connection you like; just be sure to edit the update scripts if you use other than "logbook".
* Populate the LocalConfig table with values for the relevant keys.  LocalConfig is for keys and secrets (e.g., oAuth access/secret pairs) that I don't want to have in the code, and this is necessary for mapping, social media, etc. to work.  A development site should work without most of these, but will have degraded functionality.  See below for LocalConfig settings and what they mean
 * Need to set packet size to at least 10-15MB:	show variables like 'max_allowed_packet';	SET GLOBAL max_allowed_packet=16777216;
 * Not a bad idea to bump up group_concat_max_len to something like 2048.
 * Depending on where MySQL is hosted, may need to set  lower_case_table_names=1, since the code is not consistent about upper/lower case for table names.
 * If on 5.7, may need any of the following:
     *  sql_mode ALLOW_INVALID_DATES
 #### LocalConfig settings
  * AdminAuthAccessKey - enables use of certain admin-only functionality 
 * AuthorizedWebServiceClients - comma separated list of authorized clients of the web services.
 * AWSAccessKey - Access key for Amazon Web services
 * AWSSecretKey - Secret for Amazon Web Services
 * DebugDomains - identifies local domains (e.g., http://localhost) from which oAuth requests may originate
 * DropboxAccessID - Access key for Dropbox
 * DropboxClientSecret - Secret for Dropbox
 * ETSPipelineID - Amazon Elastic Transcoder ID for processing videos
 * ETSPipelineIDDebug - Amazon Elastic Transcoder ID for processing videos from a debug/development website
 * FacebookAccessID - Access key for Facebook
 * FacebookClientSecret - Secret for Facebook
 * GoogleDriveAccessID - Access key for Google Drive
 * GoogleDriveClientSecret - Secret for Google Drive
 * GoogleMapsKey - Key for using Google maps.  Get your own.
 * GooglePlusAccessID - Access key for Google Plus
 * GooglePlusAPIKey - API key for Google Plus
 * GooglePlusClientSecret - Secret for Google Plus
 * OneDriveAccessID - Access Key for OneDrive
 * OneDriveClientSecret - Secret for OneDrive
 * PeerRequestEncryptorKey - Key used for encrypting/decrypting requests between peer users
 * SharedDataEncryptorKey - Key used to encrypt access to data being shared with the world.
 * TwitterAccessID - Access key for Twitter
 * TwitterClientSecret - Secret for Twitter
 * UseAWSS3 - set to "yes" to migrate images to S3.  Best to leave this "no" for local debugging
 * UserAccessEncryptorKey - used to share flights
 * UserPasswordHashKey - Hash key used when storing hashed passwords in the database
 * WebAccessEncryptorKey - key to encrypt/decrypt authorizations on the web service.

 ### Additional items
* Install [WKHtmlToPdf](http://wkhtmltopdf.org/) to create PDFs.  Download link is [here](http://download.gna.org/wkhtmltopdf/0.12/0.12.4/wkhtmltox-0.12.4_mingw-w64-cross-win64.exe)

### Live Site Only
These are tasks to do ONLY on the live site; there should be no need to set them up on a development environment:
* Set up a scheduled task to send nightly stats, delete drop-box cache, send nightly email.
* Install root files (support directory) so that http://xxx/ will go straight to the default home page, and so that favicon.ico will work.  Need to edit the file and ensure that default.aspx is the top-level default doc.
* Ensure reverse DNS is set up for host so that email can be received
* Set up custom errors in web.config, but TURN THEM OFF in IIS so that oAuth will return errors.  IIS->(site)->Error Pages->Edit Feature Settings->Detailed Errors.
* Ensure that 3306 (database) port is closed, ensure that firewall is otherwise appropriately configured

## Additional attributions and licenses
This source code is provided under the GNU license, but it incorporates other code as well from a variety of other sources, and each such work is covered by its respective license.  This includes, but is not limited to:
 * [DayPilot](https://javascript.daypilot.org/) Web-based calendar code for club aircraft scheduling (Apache license)
 * [CSV utilities](http://www.heikniemi.fi/jhlib/) Read/write CSV data (free/unlicensed)
 * [EXIF utilities](https://www.codeproject.com/Articles/7888/A-library-to-simplify-access-to-image-metadata) Read/write metadata for images. (free/unlicensed)
 * oAuth1 support for twitter (source forgotten, but source code provided)
 * [DotNetOpenAuth](http://dotnetopenauth.net/) Provides oAuth2 support (both client and server). (MS-Pl License)
 * Celestial code (for computing day/night) is adapted from NOAA code at http://www.srrb.noaa.gov/highlights/sunrise/sunrise.html and http://www.srrb.noaa.gov/highlights/sunrise/calcdetails.html
 * [Membership and Role management](https://www.codeproject.com/Articles/12301/Membership-and-Role-providers-for-MySQL) Provides account management and role-based security for MySQL (free/unlicensed)
 * [wkHtmlTox](https://wkhtmltopdf.org/) Renders HTML to PDF documents. (LGPLv3, but I'm not redistributing any code so I *think* I don't need to include the license in the source code directly...)
 * [WebPageSecurity](https://www.codeproject.com/KB/aspnet/WebPageSecurity.aspx?fid=29017&df=90&mpp=25&noise=3&sort=Position&view=Quick&fr=126) Enables declaration of which pages must be HTTP or HTTPS (BSD license)
 * [Endless Scroll](https://github.com/fredwu/jquery-endless-scroll) Enables "endless scroll" functionality in web browsers (MIT/GPL license)
 * [JQuery](http://jquery.org) Javascript support library for a bunch of other functionality (MIT License)
 * [OverlappingMarkerSpiderfier](https://github.com/jawj/OverlappingMarkerSpiderfier) Enables cleaner handling of overlapping markers on Google Maps - click on one of the overlapping markers, and they spread out like spider-legs, so you can click on the one you want. (MIT License)
 * [Scribble-signature control](https://www.codeproject.com/Articles/432675/Building-a-Signature-Control-Using-Canvas) - Enables fingernail scribbling for signing flights using HTML 5 ([Code Project Open License](http://www.codeproject.com/info/cpol10.aspx))
 * [Todataurl-png.js](http://code.google.com/p/todataurl-png-js/) Converts a bitmap into a data URL which can be posted in a form. ("Other" open source license, but code is unmodified)
 * [DotNetZip](https://dotnetzip.codeplex.com/) – Microsoft Public License
 * [BxSlider](http://bxslider.com/) - Enables smooth slideshows of images. (MIT License); includes jquery.fitvids.js which is [WTFPL license](http://sam.zoy.org/wtfpl/)
 * Numerous binary libraries (via NuGet), including Ajax libraries, iCal libraries, Zip, AWS, DropBox, OneDrive, etc.
 
