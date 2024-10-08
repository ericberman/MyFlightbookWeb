//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option or rebuild the Visual Studio project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Web.Application.StronglyTypedResourceProxyBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class EmailTemplates {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal EmailTemplates() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Resources.EmailTemplates", global::System.Reflection.Assembly.Load("App_GlobalResources"));
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Per your request, your account has been closed and its associated data deleted.  This email contains a backup of your flights (if any) just prior to deletion, just in case.  It is, after all, your data..
        /// </summary>
        internal static string AccountDeletedBody {
            get {
                return ResourceManager.GetString("AccountDeletedBody", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to %APP_NAME% - Account Deleted.
        /// </summary>
        internal static string AccountDeletedSubject {
            get {
                return ResourceManager.GetString("AccountDeletedSubject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to  Your account on %APP_NAME% has been locked due to too many failed attempts to sign in or to reset the password.
        ///
        ///Until it is unlocked, you will be unable to sign in or reset your password, even if you use the correct existing password.
        ///
        ///The site administrators have been notified and will unlock it for you shortly.  Or, you can use the Contact link on the website (https://%APP_URL%) to ask for help resetting your password.
        ///
        ///Thank-you,
        ///The %APP_NAME% Team
        ///
        ///To contact us, please visit https://%APP_URL [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string AccountLocked {
            get {
                return ResourceManager.GetString("AccountLocked", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to  Your account on %APP_NAME% was locked due to too many failed sign-in or failed password attempts in a short period of time.
        ///
        ///Once locked, you cannot sign in, even with the correct password.
        ///
        ///We have unlocked your account, so you can try again.  If you need to reset your password, you can do that, but you will need to know the secret answer to the question that you provided when you set up your account.  
        ///
        ///If you still cannot sign in, please use the &quot;Contact Us&quot; link on the website at https://%APP_URL% [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string AccountUnlocked {
            get {
                return ResourceManager.GetString("AccountUnlocked", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to  Dear {0}
        ///
        ///You have the aircraft &quot;{1}&quot; in your account on %APP_NAME%.
        ///
        ///Sometimes, aircraft registrations are re-assigned from one aircraft to another.  When this happens, we create multiple versions of the aircraft in the system.
        ///
        ///This aircraft had been identified as being:
        ///
        ///     *{2}*
        ///
        ///However, a new version has been created which is:
        ///
        ///     *{3}*
        ///
        ///The version used for your account is:
        ///
        ///     *{4}*
        ///
        ///If this is correct, there is no action to take.
        ///
        ///If it is NOT correct, please go to https:/ [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string AircraftTailSplit {
            get {
                return ResourceManager.GetString("AircraftTailSplit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Dear {0}:
        ///
        ///The backup of your logbook on %APP_NAME% to Box was not successful.  
        ///
        ///The error message was:
        ///   {1}
        ///
        ///{2}
        ///
        ///Please contact us using the &quot;Contact Us&quot; link on the website at https://%APP_URL% if you have any questions about this.
        ///
        ///Thank-you..
        /// </summary>
        internal static string BoxFailure {
            get {
                return ResourceManager.GetString("BoxFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to %APP_NAME% - Backup failed to Box.
        /// </summary>
        internal static string BoxFailureSubject {
            get {
                return ResourceManager.GetString("BoxFailureSubject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You have changed your email address from *{0}* to *{1}*.
        ///
        ///If this was not your intent, please visit [%APP_NAME%](https://%APP_URL%) and change your e-mail as appropriate.
        ///.
        /// </summary>
        internal static string ChangeEmailConfirmation {
            get {
                return ResourceManager.GetString("ChangeEmailConfirmation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to  &lt;p&gt;Your password has been reset.  You may now return to the site and log-in again, using your e-mail address and your new password.&lt;/p&gt;
        ///
        ///&lt;p&gt;Your new password for %APP_NAME% is:&lt;/p&gt;
        ///
        ///&lt;p style=&quot;font-size: 18pt; font-weight:bold; margin-left: 6em&quot;&gt;&lt;% Password %&gt;&lt;/p&gt;
        ///
        ///&lt;p&gt;Please note that your password is case sensitive!&lt;/p&gt;
        ///
        ///&lt;p&gt;We also STRONGLY recommend that you change this password after signing in.  You can do this &lt;a href=&quot;https://%APP_URL%%APP_ROOT%/mvc/prefs/account&quot;&gt;viewing your account&lt;/a&gt;.&lt;/p&gt;        /// [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string ChangePassEmail {
            get {
                return ResourceManager.GetString("ChangePassEmail", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Thank-you for contacting %APP_NAME%.
        ///
        ///I usually respond to requests within a few hours.  
        ///
        ///However, I will be traveling with intermittant Internet access from April 4-22, so I will be slower than usual to reply.  I&apos;ll be checking in daily, and can handle simple requests like account access issues pretty easily, but if you have a more involved question, it may be at the back end of that window before I can reply.
        ///
        ///The FAQ on the site addresses many common questions; please give that a try.
        ///.
        /// </summary>
        internal static string ContactMeResponse {
            get {
                return ResourceManager.GetString("ContactMeResponse", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Dear {0}:
        ///
        ///Thank-you very much for your donation to %APP_NAME%.  Your support is very greatly appreciated, and helps to keep the service free!
        ///
        ///Depending on the level at which you gave, you may have earned a gratuity; this has been activated on your behalf.
        ///
        ///Please don&apos;t hesitate to [contact us](https://%APP_URL%%APP_ROOT%/mvc/pub/contact) with any thoughts, questions, concerns, or ideas at.  We love hearing from you!
        ///.
        /// </summary>
        internal static string DonationThankYou {
            get {
                return ResourceManager.GetString("DonationThankYou", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to  Dear {0}:
        ///
        ///Thank-you very much for your previous donation to %APP_NAME%.  Your support is very greatly appreciated, and helps to keep the service free!
        ///
        ///Your last donation entitled you to a year of nightly cloud storage backups of your logbook, which has now expired.
        ///
        ///Please consider continuing your support for %APP_NAME% with a new donation, and your backups will resume.
        ///
        ///You can view your donation history and make a new donation at [%APP_NAME%](https://%APP_URL%%APP_ROOT%/mvc/Donate).
        ///
        ///Thanks ag [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string DropboxExpired {
            get {
                return ResourceManager.GetString("DropboxExpired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Dear {0}:
        ///
        ///Thank-you very much for your previous donation to %APP_NAME%.  Your support is very greatly appreciated, and helps to keep the service free!
        ///
        ///Your last donation entitled you to a year of nightly cloud storage backups of your logbook.  
        ///
        ///Please consider continuing your support for %APP_NAME% with a new donation, and your backups will continue uninterrupted.
        ///
        ///You can view your donation history and make a new donation at [%APP_NAME%](https://%APP_URL%%APP_ROOT%/mvc/Donate)
        ///.
        /// </summary>
        internal static string DropboxExpiring {
            get {
                return ResourceManager.GetString("DropboxExpiring", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Dear {0}:
        ///
        ///The backup of your logbook on %APP_NAME% to Dropbox was not successful.  
        ///
        ///The error message was:
        ///   {1}
        ///
        ///{2}
        ///
        ///Please contact us using the &quot;Contact Us&quot; link on the website at https://%APP_URL% if you have any questions about this.
        ///
        ///Thank-you..
        /// </summary>
        internal static string DropboxFailure {
            get {
                return ResourceManager.GetString("DropboxFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to %APP_NAME% - Backup failed to Dropbox.
        /// </summary>
        internal static string DropboxFailureSubject {
            get {
                return ResourceManager.GetString("DropboxFailureSubject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Dear {0}:
        ///
        ///Thank-you very much for your previous donation to %APP_NAME%.  Your support is very greatly appreciated, and helps to keep the service free!
        ///
        ///Your last donation was a year ago entitled you to a year of eternal gratitude, which has now expired.
        ///
        ///Please consider continuing your support for %APP_NAME% with a new donation, and your eternal gratitude will resume.
        ///
        ///You can view your donation history and make a new donation at [%APP_NAME%](https://%APP_URL%%APP_ROOT%/mvc/Donate).
        ///.
        /// </summary>
        internal static string EternalGratitudeExpired {
            get {
                return ResourceManager.GetString("EternalGratitudeExpired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Dear {0}:
        ///
        ///Thank-you very much for your previous donation to %APP_NAME%.  Your support is very greatly appreciated, and helps to keep the service free!
        ///
        ///Your last donation entitled you to a year of eternal gratitude.  
        ///
        ///Please consider continuing your support for %APP_NAME% with a new donation, and will continue to receive eternal gratitude for another year.
        ///
        ///You can view your donation history and make a new donation at [%APP_NAME%](https://%APP_URL%%APP_ROOT%/mvc/Donate).
        ///
        ///.
        /// </summary>
        internal static string EternalGratitudeExpiring {
            get {
                return ResourceManager.GetString("EternalGratitudeExpiring", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Per your request, your flights on %APP_NAME% have been deleted.  This email contains a backup of your flights (if any) just prior to deletion, just in case..
        /// </summary>
        internal static string FlightsDeletedBody {
            get {
                return ResourceManager.GetString("FlightsDeletedBody", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to %APP_NAME% - Flights Deleted.
        /// </summary>
        internal static string FlightsDeletedSubject {
            get {
                return ResourceManager.GetString("FlightsDeletedSubject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Dear {0}:
        ///
        ///The backup of your logbook on %APP_NAME% to GoogleDrive was not successful.  
        ///
        ///The error message was:
        ///   {1}
        ///
        ///{2}
        ///
        ///Please contact us using the &quot;Contact Us&quot; link on the website at https://%APP_URL% if you have any questions about this.
        ///
        ///Thank-you..
        /// </summary>
        internal static string GoogleDriveFailure {
            get {
                return ResourceManager.GetString("GoogleDriveFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to %APP_NAME% - Backup failed to GoogleDrive.
        /// </summary>
        internal static string GoogleDriveFailureSubject {
            get {
                return ResourceManager.GetString("GoogleDriveFailureSubject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to  &lt;!DOCTYPE html PUBLIC &quot;-//W3C//DTD XHTML 1.0 Transitional//EN&quot; &quot;http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd&quot;&gt;
        ///&lt;html xmlns=&quot;http://www.w3.org/1999/xhtml&quot;&gt;
        ///&lt;head&gt;
        ///    &lt;link href=&quot;https://%APP_URL%%APP_ROOT%/Public/StyleSheet.css&quot; rel=&quot;stylesheet&quot; type=&quot;text/css&quot; /&gt;
        ///&lt;/head&gt;
        ///&lt;body style=&quot;min-width: 300px; max-width: 800px; margin-left: auto; margin-right: auto;&quot;&gt;
        ///    &lt;p style=&quot;text-align:center&quot;&gt;&lt;img alt=&quot;%APP_NAME% Logo&quot; src=&quot;https://%APP_URL%%APP_LOGO%&quot; /&gt;&lt;/p&gt;
        ///    %BODYCONTENT%
        ///    &lt;br /&gt;&lt; [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string HTMLTemplate {
            get {
                return ResourceManager.GetString("HTMLTemplate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Dear {0}:
        ///
        ///The backup of your logbook on %APP_NAME% to OneDrive was not successful.  
        ///
        ///The error message was:
        ///   {1}
        ///
        ///{2}
        ///
        ///Please contact us using the &quot;Contact Us&quot; link on the website at https://%APP_URL% if you have any questions about this.
        ///
        ///Thank-you..
        /// </summary>
        internal static string OneDriveFailure {
            get {
                return ResourceManager.GetString("OneDriveFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to %APP_NAME% - Backup failed to OneDrive.
        /// </summary>
        internal static string OneDriveFailureSubject {
            get {
                return ResourceManager.GetString("OneDriveFailureSubject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Your password has been changed on %APP_NAME%.
        ///
        ///If this is what you expected, then no action is required.  If you did not make this change, however, please sign in and change your password, or contact us (use the &quot;Contact Us&quot; link at https://%APP_URL%) for help.
        ///.
        /// </summary>
        internal static string PasswordChanged {
            get {
                return ResourceManager.GetString("PasswordChanged", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Thank-you for using %APP_NAME%.
        /// </summary>
        internal static string ThankYouCloser {
            get {
                return ResourceManager.GetString("ThankYouCloser", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to  	&lt;p&gt;
        ///		&lt;img alt=&quot;%APP_NAME% Logo&quot; src=&quot;https://%APP_URL%%APP_LOGO%&quot; /&gt;
        ///	&lt;/p&gt;
        ///	&lt;p&gt;Welcome to %APP_NAME%, an online pilot&apos;s logbook.&lt;/p&gt;
        ///	&lt;p&gt;
        ///		%APP_NAME% keeps your logbook in the cloud, so you have access to it from anywhere and any device.  (It&apos;s your data, so you always have the ability to download it from the &lt;a href=&quot;https://%APP_URL%&quot;&gt;%APP_NAME% Website&lt;/a&gt;.)
        ///	&lt;/p&gt;
        ///	&lt;p&gt;
        ///		With %APP_NAME%, you can:
        ///	&lt;/p&gt;
        ///	&lt;ul&gt;
        ///		&lt;li&gt;Track your flights and access your logbook anywhere.&lt;/li&gt;
        ///		&lt;li&gt;
        ///			Enter n [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string WelcomeEmail {
            get {
                return ResourceManager.GetString("WelcomeEmail", resourceCulture);
            }
        }
    }
}
