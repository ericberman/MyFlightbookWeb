using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2015-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
 * This file contains classes to handle Amazon notifications
 * via SNS - particularly elastic transcoder
 * 
*******************************************************/

namespace AWSNotifications
{
    public static class SNSUtility
    {
        /* End User License Agreement

        This license governs use of the accompanying software. If you use the software,
        you accept this license. If you do not accept the license, do not use the
        software.

        1. Definitions

        The terms "reproduce," "reproduction," "derivative works," and "distribution"
        have the same meaning here as under U.S. copyright law.

        A "contribution" is the original software, or any additions or changes to the
        software.

        A "contributor" is any person that distributes its contribution under this
        license.

        "Licensed patents" are a contributor's patent claims that read directly on its
        contribution.

        2. Grant of Rights

        (A) Copyright Grant- Subject to the terms of this license, including the license
        conditions and limitations in section 3, each contributor grants you a
        non-exclusive, worldwide, royalty-free copyright license to reproduce its
        contribution, prepare derivative works of its contribution, and distribute its
        contribution or any derivative works that you create.

        (B) Patent Grant- Subject to the terms of this license, including the license
        conditions and limitations in section 3, each contributor grants you a
        non-exclusive, worldwide, royalty-free license under its licensed patents to
        make, have made, use, sell, offer for sale, import, and/or otherwise dispose of
        its contribution in the software or derivative works of the contribution in the
        software.

        3. Conditions and Limitations

        (A) No Trademark License- This license does not grant you rights to use any
        contributors' name, logo, or trademarks.

        (B) If you bring a patent claim against any contributor over patents that you
        claim are infringed by the software, your patent license from such contributor
        to the software ends automatically.

        (C) If you distribute any portion of the software, you must retain all
        copyright, patent, trademark, and attribution notices that are present in the
        software.

        (D) If you distribute any portion of the software in source code form, you may
        do so only under this license by including a complete copy of this license with
        your distribution. If you distribute any portion of the software in compiled or
        object code form, you may only do so under a license that complies with this
        license.

        (E) The software is licensed "as-is." You bear the risk of using it. The
        contributors give no express warranties, guarantees or conditions. You may have
        additional consumer rights under your local laws which this license cannot
        change. To the extent permitted under your local laws, the contributors exclude
        the implied warranties of merchantability, fitness for a particular purpose and
        non-infringement.
         */
        /// <summary>
        /// Validate the signature of an SNS message.  Uses code adapted from SprightlySoft; see license above
        /// </summary>
        /// <param name="szStringToValidate">The string to sign; see http://docs.aws.amazon.com/sns/latest/dg/SendMessageToHttp.verify.signature.html </param>
        /// <param name="szCertPath">Certificate URL</param>
        /// <param name="szSignature">The signature as passed in</param>
        /// <returns>True if it passes validation.</returns>
        public static bool ValidateSignature(string szStringToValidate, string szCertPath, string szSignature)
        {
            System.Uri MyUri = new System.Uri(szCertPath);

            //Check if the domain name in the SigningCertURL is an Amazon URL.
            if (MyUri.Host.EndsWith(".amazonaws.com", StringComparison.OrdinalIgnoreCase))
            {
                byte[] SignatureBytes = Convert.FromBase64String(szSignature);

                //Check the cache for the Amazon signing cert.
                byte[] PEMFileBytes = (byte[])HttpRuntime.Cache[szCertPath];

                if (PEMFileBytes == null)
                {
                    //Download the Amazon signing cert and save it to cache.
                    using (System.Net.WebClient MyWebClient = new System.Net.WebClient())
                        PEMFileBytes = MyWebClient.DownloadData(szCertPath);

                    HttpRuntime.Cache[szCertPath] = PEMFileBytes;
                }

                using (X509Certificate2 MyX509Certificate2 = new X509Certificate2(PEMFileBytes))
                {
                    RSACryptoServiceProvider MyRSACryptoServiceProvider = (RSACryptoServiceProvider)MyX509Certificate2.PublicKey.Key;

                    using (SHA1Managed MySHA1Managed = new SHA1Managed())
                    {
                        byte[] HashBytes = MySHA1Managed.ComputeHash(Encoding.UTF8.GetBytes(szStringToValidate));

                        return MyRSACryptoServiceProvider.VerifyHash(HashBytes, CryptoConfig.MapNameToOID("SHA1"), SignatureBytes);
                    }
                }
            }
            else
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Amazon AWS SNS notification - see http://docs.aws.amazon.com/sns/latest/dg/json-formats.html
    /// </summary>
    public class SNSNotification
    {
        #region properties
        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonProperty("MessageId")]
        public string MessageId { get; set; }

        [JsonProperty("TopicArn")]
        public string TopicArn { get; set; }

        [JsonProperty("Subject")]
        public string Subject { get; set; }

        [JsonProperty("Message")]
        public string Message { get; set; }

        [JsonProperty("Timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("SignatureVersion")]
        public string SignatureVersion { get; set; }

        [JsonProperty("Signature")]
        public string Signature { get; set; }

        [JsonProperty("SigningCertURL")]
        public string SigningCertURL { get; set; }

        [JsonProperty("UnsubscribeURL")]
        public string UnsubscribeURL { get; set; }
        #endregion

        public SNSNotification() { }

        /// <summary>
        /// Verifies the integrity of the subscription request.  Code adapted from SprightlySoft SNSAutoConfirm sample.  License is:
        /// SprightlySoft SNS Auto Confirm.  License is reproduced above in SNSUtility
        /// </summary>
        /// <returns>True if the subscription matches</returns>
        public Boolean VerifySignature()
        {
            StringBuilder sbGenerated = new StringBuilder();
            sbGenerated.Append("Message\n").Append(Message).Append("\n");
            sbGenerated.Append("MessageId\n").Append(MessageId).Append("\n");

            if (!String.IsNullOrEmpty(Subject))
                sbGenerated.Append("Subject\n").Append(Subject).Append("\n");

            sbGenerated.Append("Timestamp\n").Append(Timestamp).Append("\n");
            sbGenerated.Append("TopicArn\n").Append(TopicArn).Append("\n");
            sbGenerated.Append("Type\n").Append(Type).Append("\n");

            return SNSUtility.ValidateSignature(sbGenerated.ToString(), SigningCertURL, Signature);
        }
    }

    /// <summary>
    /// Amazon AWS SNS subscription confirmation - see http://docs.aws.amazon.com/sns/latest/dg/json-formats.html
    /// </summary>
    public class SNSSubscriptionConfirmation
    {
        #region properties
        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonProperty("MessageId")]
        public string MessageId { get; set; }

        [JsonProperty("Token")]
        public string Token { get; set; }

        [JsonProperty("TopicArn")]
        public string TopicArn { get; set; }

        [JsonProperty("Message")]
        public string Message { get; set; }

        [JsonProperty("SubscribeURL")]
        public string SubscribeURL { get; set; }

        [JsonProperty("Timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("SignatureVersion")]
        public string SignatureVersion { get; set; }

        [JsonProperty("Signature")]
        public string Signature { get; set; }

        [JsonProperty("SigningCertURL")]
        public string SigningCertURL { get; set; }
        #endregion

        public SNSSubscriptionConfirmation() { }

        /// <summary>
        /// Verifies the integrity of the subscription request.  Code adapted from SprightlySoft SNSAutoConfirm sample.  License is:
        /// SprightlySoft SNS Auto Confirm.  License is reproduced above in SNSUtility
        /// </summary>
        /// <returns>True if the subscription matches</returns>
        public Boolean VerifySignature()
        {
            // we can bypass the signature if the subscribe URL we are being asked to hit doesn't belong to AmazonAWS.
            Uri uri = new Uri(SubscribeURL);
            if (!uri.Host.EndsWith("amazonaws.com", StringComparison.OrdinalIgnoreCase))
                return false;

            StringBuilder sbGenerated = new StringBuilder();
            sbGenerated.Append("Message\n").Append(Message).Append("\n");
            sbGenerated.Append("MessageId\n").Append(MessageId).Append("\n");
            sbGenerated.Append("SubscribeURL\n").Append(SubscribeURL).Append("\n");
            sbGenerated.Append("Timestamp\n").Append(Timestamp).Append("\n");
            sbGenerated.Append("Token\n").Append(Token).Append("\n");
            sbGenerated.Append("TopicArn\n").Append(TopicArn).Append("\n");
            sbGenerated.Append("Type\n").Append(Type).Append("\n");

            return SNSUtility.ValidateSignature(sbGenerated.ToString(), SigningCertURL, Signature);
        }
    }

    /// <summary>
    /// Amazon AWS SNS unsubscribe confirmation - see http://docs.aws.amazon.com/sns/latest/dg/json-formats.html
    /// </summary>
    public class SNSUnsubscribeConfirmation
    {
        #region properties
        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonProperty("MessageId")]
        public string MessageId { get; set; }

        [JsonProperty("Token")]
        public string Token { get; set; }

        [JsonProperty("TopicArn")]
        public string TopicArn { get; set; }

        [JsonProperty("Message")]
        public string Message { get; set; }

        [JsonProperty("SubscribeURL")]
        public string SubscribeURL { get; set; }

        [JsonProperty("Timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("SignatureVersion")]
        public string SignatureVersion { get; set; }

        [JsonProperty("Signature")]
        public string Signature { get; set; }

        [JsonProperty("SigningCertURL")]
        public string SigningCertURL { get; set; }
        #endregion

        public SNSUnsubscribeConfirmation() { }
    }

    public class ETSInput
    {
        #region properties
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("frameRate")]
        public string FrameRate { get; set; }

        [JsonProperty("resolution")]
        public string Resolution { get; set; }

        [JsonProperty("aspectRatio")]
        public string AspectRatio { get; set; }

        [JsonProperty("interlaced")]
        public string Interlaced { get; set; }

        [JsonProperty("container")]
        public string Container { get; set; }
        #endregion

        public ETSInput() { }
    }

    public class ETSOutput
    {
        #region Properties
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("presetId")]
        public string PresetId { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("thumbnailPattern")]
        public string ThumbnailPattern { get; set; }

        [JsonProperty("rotate")]
        public string Rotate { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("statusDetail")]
        public string StatusDetail { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }
        #endregion

        public ETSOutput() { }
    }

    /// <summary>
    /// JSON declaration for result of Elastic Transcoder Service (ETS) process
    /// </summary>
    public class AWSETSStateMessage
    {
        public enum ETSStates { COMPLETED, PROGRESSING, WARNING, ERROR }

        #region properties
        [JsonProperty("state")]
        public string State { get; set; }

        /// <summary>
        /// Returns the state of the job as an ETSStates value
        /// </summary>
        public ETSStates JobState
        {
            get
            {
                ETSStates s;
                if (Enum.TryParse<ETSStates>(State, out s))
                    return s;
                return ETSStates.PROGRESSING;
            }
        }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }

        [JsonProperty("messageDetails")]
        public string MessageDetails { get; set; }

        [JsonProperty("jobId")]
        public string JobId { get; set; }

        [JsonProperty("pipelineId")]
        public string PipelineId { get; set; }

        [JsonProperty("input")]
        public ETSInput Input { get; set; }

        [JsonProperty("outputs")]
        public ETSOutput[] Outputs { get; set; }
        #endregion

        public AWSETSStateMessage() { }
    }
}