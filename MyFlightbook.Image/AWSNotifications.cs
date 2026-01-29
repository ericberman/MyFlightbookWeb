using MyFlightbook;
using MyFlightbook.Image;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

/******************************************************
 * 
 * Copyright (c) 2015-2026 MyFlightbook LLC
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
                byte[] PEMFileBytes = (byte[])util.GlobalCache.Get(szCertPath);

                if (PEMFileBytes == null)
                {
                    //Download the Amazon signing cert and save it to cache.
                    using (System.Net.WebClient MyWebClient = new System.Net.WebClient())
                        PEMFileBytes = MyWebClient.DownloadData(szCertPath);

                    util.GlobalCache.Set(szCertPath, PEMFileBytes, DateTimeOffset.UtcNow.AddDays(1));
                }

                using (X509Certificate2 MyX509Certificate2 = new X509Certificate2(PEMFileBytes))
                {
                    RSACryptoServiceProvider MyRSACryptoServiceProvider = (RSACryptoServiceProvider)MyX509Certificate2.PublicKey.Key;

#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms
                    // this only works with SHA1...
                    using (SHA1Managed MySHA1Managed = new SHA1Managed())
#pragma warning restore CA5350 // Do Not Use Weak Cryptographic Algorithms
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

        public static T ReadJSONObject<T>(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            stream.Seek(0, System.IO.SeekOrigin.Begin);
            using (var r = new StreamReader(stream))
            {
                var inputString = r.ReadToEnd();
                return (T)JsonConvert.DeserializeObject<T>(inputString);
            }
        }

        public static async Task<bool> ProcessAWSSNSMessage(Stream stream, string szMessageType)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            switch ((szMessageType ?? string.Empty).ToUpperInvariant())
            {
                case "NOTIFICATION":
                    SNSNotification snsNotification = ReadJSONObject<SNSNotification>(stream);
                    if (String.IsNullOrEmpty(snsNotification.Signature) || snsNotification.VerifySignature())
                    {
                        _ = await new MFBImageInfo().InitFromSNSNotification(snsNotification);
                    }
                    break;
                case "SUBSCRIPTIONCONFIRMATION":
                    {
                        SNSSubscriptionConfirmation snsSubscription = ReadJSONObject<SNSSubscriptionConfirmation>(stream);

                        // Visit the URL to confirm it.
                        if (snsSubscription.VerifySignature())
                        {
                            using (WebClient wc = new WebClient())
                            {
                                byte[] rgdata = wc.DownloadData(snsSubscription.SubscribeLink);
                                _ = Encoding.UTF8.GetString(rgdata);
                            }
                        }
                    }
                    break;
                case "UNSUBSCRIBECONFIRMATION":
                    // Nothing to do for now.
                    break;
                default:
                    // Test messages/etc. can go here.
                    break;
            }
            return true;
        }

        public static void SendTestPost(string szContent, string snsMessageType, Uri uri)
        {
            var hr = (HttpWebRequest) HttpWebRequest.Create(uri);
            hr.Method = "POST";
            hr.Headers.Add(String.Format(CultureInfo.InvariantCulture, "x-amz-sns-message-type: {0}", snsMessageType));
            hr.Headers.Add("x-amz-sns-message-id: xxx");
            hr.Headers.Add("x-amz-sns-message-id: yyy");
            hr.ContentType = "text/plain; charset=UTF-8";

            byte[] rgBytes = Encoding.UTF8.GetBytes(szContent);
            hr.ContentLength = rgBytes.Length;
            hr.Timeout = 10000;

            using (Stream RequestStream = hr.GetRequestStream())
            {
                {
                    RequestStream.Write(rgBytes, 0, rgBytes.Length);
                    using (WebResponse wr = hr.GetResponse())
                    {
                        // nothing to do here.
                    }
                }
            }
        }
    }

    /// <summary>
    /// Amazon AWS SNS notification - see http://docs.aws.amazon.com/sns/latest/dg/json-formats.html
    /// </summary>
    public class SNSNotification
    {
#pragma warning disable CA1507 // Use nameof to express symbol names
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
        public string SigningCertLink { get; set; }

        [JsonProperty("UnsubscribeURL")]
        public string UnsubscribeLink { get; set; }
        #endregion
#pragma warning restore CA1507 // Use nameof to express symbol names

        public SNSNotification() { }

        /// <summary>
        /// Verifies the integrity of the subscription request.  Code adapted from SprightlySoft SNSAutoConfirm sample.  License is:
        /// SprightlySoft SNS Auto Confirm.  License is reproduced above in SNSUtility
        /// </summary>
        /// <returns>True if the subscription matches</returns>
        public Boolean VerifySignature()
        {
            StringBuilder sbGenerated = new StringBuilder();
            sbGenerated.Append("Message\n").Append(Message).Append('\n');
            sbGenerated.Append("MessageId\n").Append(MessageId).Append('\n');

            if (!String.IsNullOrEmpty(Subject))
                sbGenerated.Append("Subject\n").Append(Subject).Append('\n');

            sbGenerated.Append("Timestamp\n").Append(Timestamp).Append('\n');
            sbGenerated.Append("TopicArn\n").Append(TopicArn).Append('\n');
            sbGenerated.Append("Type\n").Append(Type).Append('\n');

            return SNSUtility.ValidateSignature(sbGenerated.ToString(), SigningCertLink, Signature);
        }
    }

    /// <summary>
    /// Amazon AWS SNS subscription confirmation - see http://docs.aws.amazon.com/sns/latest/dg/json-formats.html
    /// </summary>
    public class SNSSubscriptionConfirmation
    {
#pragma warning disable CA1507 // Use nameof to express symbol names
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
        public string SubscribeLink { get; set; }

        [JsonProperty("Timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("SignatureVersion")]
        public string SignatureVersion { get; set; }

        [JsonProperty("Signature")]
        public string Signature { get; set; }

        [JsonProperty("SigningCertURL")]
        public string SigningCertLink { get; set; }
        #endregion
#pragma warning restore CA1507 // Use nameof to express symbol names

        public SNSSubscriptionConfirmation() { }

        /// <summary>
        /// Verifies the integrity of the subscription request.  Code adapted from SprightlySoft SNSAutoConfirm sample.  License is:
        /// SprightlySoft SNS Auto Confirm.  License is reproduced above in SNSUtility
        /// </summary>
        /// <returns>True if the subscription matches</returns>
        public Boolean VerifySignature()
        {
            // we can bypass the signature if the subscribe URL we are being asked to hit doesn't belong to AmazonAWS.
            Uri uri = new Uri(SubscribeLink);
            if (!uri.Host.EndsWith("amazonaws.com", StringComparison.OrdinalIgnoreCase))
                return false;

            StringBuilder sbGenerated = new StringBuilder();
            sbGenerated.Append("Message\n").Append(Message).Append('\n');
            sbGenerated.Append("MessageId\n").Append(MessageId).Append('\n');
            sbGenerated.Append("SubscribeURL\n").Append(SubscribeLink).Append('\n');
            sbGenerated.Append("Timestamp\n").Append(Timestamp).Append('\n');
            sbGenerated.Append("Token\n").Append(Token).Append('\n');
            sbGenerated.Append("TopicArn\n").Append(TopicArn).Append('\n');
            sbGenerated.Append("Type\n").Append(Type).Append('\n');

            return SNSUtility.ValidateSignature(sbGenerated.ToString(), SigningCertLink, Signature);
        }
    }

    /// <summary>
    /// Amazon AWS SNS unsubscribe confirmation - see http://docs.aws.amazon.com/sns/latest/dg/json-formats.html
    /// </summary>
    public class SNSUnsubscribeConfirmation
    {
#pragma warning disable CA1507 // Use nameof to express symbol names
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
        public string SubscribeLink { get; set; }

        [JsonProperty("Timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("SignatureVersion")]
        public string SignatureVersion { get; set; }

        [JsonProperty("Signature")]
        public string Signature { get; set; }

        [JsonProperty("SigningCertURL")]
        public string SigningCertLink { get; set; }
        #endregion
#pragma warning restore CA1507 // Use nameof to express symbol names

        public SNSUnsubscribeConfirmation() { }
    }
}