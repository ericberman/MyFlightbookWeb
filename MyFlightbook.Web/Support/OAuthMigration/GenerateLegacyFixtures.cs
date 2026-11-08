using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using DotNetOpenAuth.Messaging.Bindings;
using DotNetOpenAuth.OAuth2;

/******************************************************
 * Mints genuine DotNetOpenAuth 4.3.4 tokens with FIXED test keys and prints them as C# constants for
 * MyFlightbook.OAuth.Tests/LegacyFixtures.cs.  Needs the DNOA DLLs from MyFlightbook.Web\bin (net48 or Mono).
 * Re-run only if you want fresh fixtures; the keys here are test-only and protect nothing.
*******************************************************/

class FixedKeyStore : ICryptoKeyStore
{
    public readonly Dictionary<string, CryptoKey> K = new Dictionary<string, CryptoKey>();
    public CryptoKey GetKey(string b, string h) { CryptoKey k; return K.TryGetValue(b + "|" + h, out k) ? k : null; }
    public IEnumerable<KeyValuePair<string, CryptoKey>> GetKeys(string b) => K.Where(kv => kv.Key.StartsWith(b + "|")).Select(kv => new KeyValuePair<string, CryptoKey>(kv.Key.Substring(b.Length + 1), kv.Value)).ToList();
    public void StoreKey(string b, string h, CryptoKey k) { K[b + "|" + h] = k; }
    public void RemoveKey(string b, string h) { K.Remove(b + "|" + h); }
}

static class GenerateLegacyFixtures
{
    static void Main()
    {
        byte[] key = Enumerable.Range(0, 32).Select(i => (byte)(i * 7 + 3)).ToArray();
        var store = new FixedKeyStore();
        store.StoreKey("https://localhost/dnoa/oauth_refresh_token", "Ab-9", new CryptoKey(key, DateTime.UtcNow.AddDays(28)));

        var asAS = typeof(AuthorizationServer).Assembly;
        Type rtType = asAS.GetType("DotNetOpenAuth.OAuth2.ChannelElements.RefreshToken", true);
        object fmt = rtType.GetMethod("CreateFormatter", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { store });
        Func<string, string, string[], string> mint = (client, user, scopes) =>
        {
            object rt = Activator.CreateInstance(rtType, true);
            rtType.GetProperty("ClientIdentifier").SetValue(rt, client);
            rtType.GetProperty("User").SetValue(rt, user);
            var s = (HashSet<string>)rtType.GetProperty("Scope").GetValue(rt);
            foreach (string sc in scopes) s.Add(sc);
            return (string)fmt.GetType().GetMethod("Serialize").Invoke(fmt, new object[] { rt });
        };

        string rsaXml;
        string access;
        using (var rsa = new RSACryptoServiceProvider(1024))
        {
            rsaXml = rsa.ToXmlString(true);
            var at = new AuthorizationServerAccessToken() { Lifetime = TimeSpan.FromDays(14), AccessTokenSigningKey = rsa, ResourceServerEncryptionKey = rsa, ClientIdentifier = "partner", User = "pilot_joe" };
            at.Scope.Add("totals");
            access = (string)typeof(AuthorizationServerAccessToken).GetMethod("Serialize", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(at, null);
        }

        var sb = new StringBuilder();
        sb.AppendLine("        // Generated " + DateTime.UtcNow.ToString("u") + " by Support/OAuthMigration/GenerateLegacyFixtures.cs using DotNetOpenAuth 4.3.4");
        sb.AppendLine("        public const string RefreshKeyHandle = \"Ab-9\";");
        sb.AppendLine("        public static readonly byte[] RefreshKey = Convert.FromBase64String(\"" + Convert.ToBase64String(key) + "\");");
        sb.AppendLine("        public const string PartnerRefreshToken = \"" + mint("partner", "pilot_joe", new[] { "currency", "totals" }) + "\";   // partner / pilot_joe / currency totals");
        sb.AppendLine("        public const string ProxyRefreshTokenSue = \"" + mint("publicClientProxy", "pilot_sue", new[] { "currency", "readflight" }) + "\";   // publicClientProxy / pilot_sue / currency readflight");
        sb.AppendLine("        public const string ProxyRefreshTokenBob = \"" + mint("publicClientProxy", "pilot_bob", new[] { "currency" }) + "\";   // publicClientProxy / pilot_bob / currency");
        sb.AppendLine("        public const string RsaKeyXml = \"" + rsaXml + "\";");
        sb.AppendLine("        public const string PartnerAccessToken = \"" + access + "\";   // partner / pilot_joe / totals, 14-day lifetime");
        Console.Write(sb.ToString());
    }
}
