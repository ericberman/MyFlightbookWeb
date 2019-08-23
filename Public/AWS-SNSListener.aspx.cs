using AWSNotifications;
using MyFlightbook.Image;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Net;

/******************************************************
 * 
 * Copyright (c) 2015-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_AWS_SNSListener : System.Web.UI.Page
{
    protected T ReadJSONObject<T>()
    {
        Request.InputStream.Seek(0, System.IO.SeekOrigin.Begin);
        var r = new StreamReader(Request.InputStream);
        var inputString = r.ReadToEnd();
        return (T)JsonConvert.DeserializeObject<T>(inputString);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
            pnlTest.Visible = true;

        if (String.Compare(Request.RequestType, "POST", StringComparison.OrdinalIgnoreCase) == 0)
        {
            string szMessageType = (Request.Headers["x-amz-sns-message-type"] ?? string.Empty).ToUpperInvariant();
            switch (szMessageType)
            {
                case "NOTIFICATION":
                    SNSNotification snsNotification = ReadJSONObject<SNSNotification>();
                    if (String.IsNullOrEmpty(snsNotification.Signature) || snsNotification.VerifySignature())
                    {
                        MFBImageInfo mfbii = new MFBImageInfo(snsNotification);  // simply creating the object will do all that is necessary.
                    }
                    Response.Clear();
                    Response.Write("OK");
                    Response.End();
                    break;
                case "SUBSCRIPTIONCONFIRMATION":
                    {
                        SNSSubscriptionConfirmation snsSubscription = ReadJSONObject<SNSSubscriptionConfirmation>();

                        // Visit the URL to confirm it.
                        if (snsSubscription.VerifySignature())
                        {
                            using (WebClient wc = new System.Net.WebClient())
                            {
                                byte[] rgdata = wc.DownloadData(snsSubscription.SubscribeURL);
                                string szContent = System.Text.UTF8Encoding.UTF8.GetString(rgdata);
                            }
                        }
                    }
                    Response.Clear();
                    Response.Write("OK");
                    Response.End();
                    break;
                case "UNSUBSCRIBECONFIRMATION":
                    // Nothing to do for now.
                    break;
                default:
                    // Test messages/etc. can go here.
                    break;
            }
        }
    }

    protected void SendTestPost(string szContent, string snsMessageType)
    {
        HttpWebRequest hr = (HttpWebRequest)HttpWebRequest.Create(Request.Url);
        hr.Method = "POST";
        hr.Headers.Add(String.Format(CultureInfo.InvariantCulture, "x-amz-sns-message-type: {0}", snsMessageType));
        hr.Headers.Add("x-amz-sns-message-id: xxx");
        hr.Headers.Add("x-amz-sns-message-id: yyy");
        Request.ContentType = "text/plain; charset=UTF-8";

        Stream RequestStream = null;

        try
        {
            byte[] rgBytes = System.Text.Encoding.UTF8.GetBytes(szContent);
            hr.ContentLength = rgBytes.Length;
            hr.Timeout = 10000;
            RequestStream = hr.GetRequestStream();
            RequestStream.Write(rgBytes, 0, rgBytes.Length);
            WebResponse response = hr.GetResponse();
        }
        catch (WebException) { }
        catch (InvalidOperationException) { }
        catch (NotSupportedException) { }
        finally
        {
            if (RequestStream != null)
                RequestStream.Close();
        }
    }

    protected void btnTestSubscribe_Click(object sender, EventArgs e)
    {
        if (String.IsNullOrEmpty(txtTestJSONData.Text))
        {
            SNSSubscriptionConfirmation sc = new SNSSubscriptionConfirmation()
            {
                Type = "SubscriptionConfirmation",
                MessageId = Guid.NewGuid().ToString(),
                Token = "thisisthetoken",
                Message = Resources.Admin.AWSSubscriptionTestMessage,
                SubscribeURL = "http://google.com",
                SignatureVersion = "1",
                Signature = "EXAMPLEpH+DcEwjAPg8O9mY8dReBSwksfg2S7WKQcikcNKWLQjwu6A4VbeS0QHVCkhRS7fUQvi2egU3N858fiTDN6bkkOxYDVrY0Ad8L10Hs3zH81mtnPk5uvvolIC1CXGu43obcgFxeL3khZl8IKvO61GWB6jI9b5+gLPoBc1Q=",
                SigningCertURL = "https://sns.us-west-2.amazonaws.com/SimpleNotificationService-f3ecfb7224c7233fe7bb5f59f96de52f.pem",
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddThh:mm:ssZ", CultureInfo.InvariantCulture),
                TopicArn = "arn:aws:sns:us-west-2:123456789012:MyTopic"
            };
        }
        else
            SendTestPost(txtTestJSONData.Text, "SubscriptionConfirmation");
    }

    protected void btnTestNotify_Click(object sender, EventArgs e)
    {
        SNSNotification sns = new SNSNotification() { Message = txtTestJSONData.Text };
        SendTestPost(JsonConvert.SerializeObject(sns), "Notification");
    }
}