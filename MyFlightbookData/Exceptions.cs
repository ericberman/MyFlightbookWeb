using System;
using System.Runtime.Serialization;

/******************************************************
 * 
 * Copyright (c) 2008-2025 MyFlightbook LLC
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
    }
}