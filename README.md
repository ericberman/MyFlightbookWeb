# MyFlightbookWeb
The website and service for MyFlightbook.  This provides the back-end for the mobile apps as well.

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
 * Numerous binary libraries (via NuGet), including Ajax libraries, iCal libraries, Zip, AWS, DropBox, OneDrive, etc.
 
 ## Getting Started
 * Install MySQL and import "MinimalDB-xxxx-xx-xx.sql" (in the Support folder), then apply any/all scripts in that folder that are after that date.
 * Need to set packet size to at least 10-15MB:	show variables like 'max_allowed_packet';	SET GLOBAL max_allowed_packet=16777216;
 * Install ASP.NET 4.5.  Make sure IIS has ASP turned on under "application development features"
 * Install [WkHtmlToPdf](http://wkhtmltopdf.org/) to create PDFs; download link is [here](http://download.gna.org/wkhtmltopdf/0.12/0.12.4/wkhtmltox-0.12.4_mingw-w64-cross-win64.exe). 
 * Populate the LocalConfig table with values for the relevant keys.  LocalConfig is for keys and secrets that I don't want to have in the code
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

(More TBD)
