using System;
using System.Text;
using System.Runtime.Serialization;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2008-2016 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Common exception class for MyFlightbook exceptions
    /// </summary>
    [Serializable]
    public class MyFlightbookException : Exception
    {
        /// <summary>
        /// Name of the active user when the exception was thrown.
        /// </summary>
        public string Username { get; set; }

        public MyFlightbookException()
            : base()
        {
            Username = string.Empty;
        }

        public MyFlightbookException(string message)
            : base(message)
        {
            Username = string.Empty;
        }

        public MyFlightbookException(string message, Exception innerException)
            : base(message, innerException)
        {
            Username = string.Empty;
        }

        public MyFlightbookException(string message, Exception innerException, string szUser)
            : base(message, innerException)
        {
            Username = szUser;
        }

        public MyFlightbookException(string message, string szUser)
            : base(message)
        {
            Username = szUser;
        }

        protected MyFlightbookException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("Username", Username);
        }

        public static void NotifyAdminException(Exception ex)
        {
            if (ex == null)
                return;
            StringBuilder sb = new StringBuilder();
            do
            {
                sb.AppendFormat(CultureInfo.CurrentCulture, "{0}\r\n{1}\r\n{2}\r\n\r\n", ex.GetType().FullName, ex.Message, ex.StackTrace);
                ex = ex.InnerException;
            } while (ex != null);
            util.NotifyAdminEvent("Exception on the MyFlightbook site", sb.ToString(), ProfileRoles.maskSiteAdminOnly);
        }
    }

    /// <summary>
    /// Data validation exception.
    /// </summary>
    [Serializable]
    public class MyFlightbookValidationException : Exception
    {
        public string ParameterName { get; set; }

        public MyFlightbookValidationException()
            : base()
        { }

        public MyFlightbookValidationException(string message)
            : base(message)
        { }

        public MyFlightbookValidationException(string message, Exception innerException)
            : base(message, innerException)
        { }

        protected MyFlightbookValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}