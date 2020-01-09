using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2008-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Encryptors
{
    #region concrete encryption classes
    /// <summary>
    /// Encryptor to use to provide credentials to private items in a public context (e.g., stuff in Public folder)
    /// </summary>
    public class UserAccessEncryptor : MFBEncryptor
    {
        public UserAccessEncryptor() : base(LocalConfig.SettingForKey("UserAccessEncryptorKey"), "mfb") { }
    }

    /// <summary>
    /// Encryptor to be used on a per-user basis (uses the username as the key) - not super secure, but enough to make tampering hard. ONLY USE THIS ON DATA THAT IS NOT EXPOSED.  (I.e., flight signatures)
    /// </summary>
    public class UserEncryptor : MFBEncryptor
    {
        public UserEncryptor(string szUser) : base(szUser, null) { }
    }

    /// <summary>
    /// Encryptor to be used for data that is exposed to other people (e.g., RSS feeds or shared flights).
    /// </summary>
    public class SharedDataEncryptor : MFBEncryptor
    {
        public SharedDataEncryptor() : base(LocalConfig.SettingForKey("SharedDataEncryptorKey"), null) { }

        public SharedDataEncryptor(string szBackupKey) : base(LocalConfig.SettingForKey("SharedDataEncryptorKey"), szBackupKey) { }
    }

    /// <summary>
    /// Encryption for webservice authtokens.
    /// </summary>
    public class WebServiceEncryptor : MFBEncryptor
    {
        public WebServiceEncryptor() : base(LocalConfig.SettingForKey("WebAccessEncryptorKey"), "MyFlightbookAPIKey-boythatwashardtoguess") { }
    }

    /// <summary>
    /// Encryptor for peer-to-peer requests
    /// </summary>
    public class PeerRequestEncryptor : MFBEncryptor
    {
        public PeerRequestEncryptor() : base(LocalConfig.SettingForKey("PeerRequestEncryptorKey"), "MFBRelationshipRequestPass") { }
    }

    /// <summary>
    /// Encryptor for admin functions like sending email
    /// </summary>
    public class AdminAuthEncryptor : MFBEncryptor
    {
        public AdminAuthEncryptor() : base(LocalConfig.SettingForKey("AdminAuthAccessKey"), "AuthAdminKeyInPublicDir") { }
    }
    #endregion
    
    /// <summary>
    /// Provides simple symmetrical encryption services for a variety of applications.
    /// </summary>
    public abstract class MFBEncryptor
    {
        readonly private string m_szPass = string.Empty;

        /// <summary>
        /// key to use for legacy support if the current key doesn't work
        /// </summary>
        readonly private string m_szPassFallback = string.Empty;

        /// <summary>
        /// Creates an encryptor using the specified pass keys
        /// </summary>
        /// <param name="szPass">The pass key to use.</param>
        /// <param name="szBackup">A backup to use if necessary for backwards compatibility</param>
        protected MFBEncryptor(string szPass, string szBackup)
        {
            m_szPass = szPass;
            m_szPassFallback = szBackup;
        }

        private string Decrypt(string TextToBeDecrypted, string szPass, bool fUseFallbackOnFail)
        {
            using (RijndaelManaged RijndaelCipher = new RijndaelManaged())
            {
                string DecryptedData = string.Empty;

                try
                {
                    byte[] EncryptedData = Convert.FromBase64String(TextToBeDecrypted);

                    byte[] Salt = Encoding.ASCII.GetBytes(szPass.Length.ToString(CultureInfo.InvariantCulture));
                    //Making of the key for decryption
                    using (PasswordDeriveBytes SecretKey = new PasswordDeriveBytes(szPass, Salt))
                    {
                        //Creates a symmetric Rijndael decryptor object.
                        ICryptoTransform Decryptor = RijndaelCipher.CreateDecryptor(SecretKey.GetBytes(32), SecretKey.GetBytes(16));

                        MemoryStream memoryStream = null;
                        try {
                            memoryStream = new MemoryStream(EncryptedData);
                            //Defines the cryptographics stream for decryption.THe stream contains decrpted data
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, Decryptor, CryptoStreamMode.Read))
                            {
                                memoryStream = null;
                                byte[] PlainText = new byte[EncryptedData.Length];
                                int DecryptedCount = cryptoStream.Read(PlainText, 0, PlainText.Length);

                                //Converting to string
                                DecryptedData = Encoding.Unicode.GetString(PlainText, 0, DecryptedCount);
                            }
                        }
                        finally
                        {
                            if (memoryStream != null)
                                memoryStream.Dispose();
                        }
                    }
                }
                catch (FormatException)
                {
                    // Should never happen - implies bogus base64 data, so return empty string
                    return string.Empty;
                }
                catch (CryptographicException)
                {
                    DecryptedData = fUseFallbackOnFail ? Decrypt(TextToBeDecrypted, m_szPassFallback, false) : TextToBeDecrypted;
                }
                return DecryptedData;
            }
        }

        public string Decrypt(string TextToBeDecrypted)
        {
            return Decrypt(TextToBeDecrypted, m_szPass, !String.IsNullOrEmpty(m_szPassFallback));
        }

        public string Encrypt(string TextToBeEncrypted)
        {
            using (RijndaelManaged RijndaelCipher = new RijndaelManaged())
            {
                byte[] PlainText = System.Text.Encoding.Unicode.GetBytes(TextToBeEncrypted);
                byte[] Salt = Encoding.ASCII.GetBytes(m_szPass.Length.ToString(CultureInfo.InvariantCulture));
                using (PasswordDeriveBytes SecretKey = new PasswordDeriveBytes(m_szPass, Salt))
                {
                    string EncryptedData = string.Empty;

                    //Creates a symmetric encryptor object. 
                    ICryptoTransform Encryptor = RijndaelCipher.CreateEncryptor(SecretKey.GetBytes(32), SecretKey.GetBytes(16));
                    MemoryStream memoryStream = null;
                    MemoryStream result = null;
                    try {
                        result = memoryStream = new MemoryStream();
                        //Defines a stream that links data streams to cryptographic transformations
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, Encryptor, CryptoStreamMode.Write))
                        {
                            memoryStream = null;    // CA2202
                            cryptoStream.Write(PlainText, 0, PlainText.Length);
                            //Writes the final state and clears the buffer
                            cryptoStream.FlushFinalBlock();
                            byte[] CipherBytes = result.ToArray();
                            EncryptedData = Convert.ToBase64String(CipherBytes);
                        }
                    }
                    finally
                    {
                        if (memoryStream != null)
                            memoryStream.Dispose();
                    }
                    return EncryptedData;
                }
            }
        }
    }
}