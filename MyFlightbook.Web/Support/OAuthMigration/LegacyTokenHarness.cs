using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using DotNetOpenAuth.Messaging.Bindings;
using DotNetOpenAuth.OAuth2;
using MyFlightbook.OAuth.Legacy;
using Newtonsoft.Json;

class MemKeyStore : ICryptoKeyStore
{
    public readonly Dictionary<string, CryptoKey> Keys = new Dictionary<string, CryptoKey>();
    public CryptoKey GetKey(string bucket, string handle) { CryptoKey k; return Keys.TryGetValue(bucket + "|" + handle, out k) ? k : null; }
    public IEnumerable<KeyValuePair<string, CryptoKey>> GetKeys(string bucket) =>
        Keys.Where(kv => kv.Key.StartsWith(bucket + "|")).Select(kv => new KeyValuePair<string, CryptoKey>(kv.Key.Substring(bucket.Length + 1), kv.Value)).OrderByDescending(kv => kv.Value.ExpiresUtc).ToList();
    public void StoreKey(string bucket, string handle, CryptoKey key) { Keys[bucket + "|" + handle] = key; }
    public void RemoveKey(string bucket, string handle) { Keys.Remove(bucket + "|" + handle); }
}

static class Harness
{
    static int failures = 0;
    static void Check(bool cond, string what) { Console.WriteLine((cond ? "PASS " : "FAIL ") + what); if (!cond) failures++; }

    static int Main()
    {
        var asAS = typeof(AuthorizationServer).Assembly;
        var store = new MemKeyStore();
        // Pre-seed a key (as the cryptokeys table would have) so DNOA doesn't need to log about creating one (log4net absent under mono)
        var seed = new byte[32]; RandomNumberGenerator.Create().GetBytes(seed);
        store.StoreKey("https://localhost/dnoa/oauth_refresh_token", "Ab-9", new CryptoKey(seed, DateTime.UtcNow.AddDays(28)));

        // ---- Refresh token minted by real DNOA ----
        Type rtType = asAS.GetType("DotNetOpenAuth.OAuth2.ChannelElements.RefreshToken", true);
        object rt = Activator.CreateInstance(rtType, true);
        rtType.GetProperty("ClientIdentifier").SetValue(rt, "publicClientProxy");
        rtType.GetProperty("User").SetValue(rt, "pilot_joe");
        var scope = (HashSet<string>)rtType.GetProperty("Scope").GetValue(rt);
        scope.Add("currency"); scope.Add("readflight"); scope.Add("addflight");
        object fmt = rtType.GetMethod("CreateFormatter", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { store });
        DateTime before = DateTime.UtcNow.AddSeconds(-1);
        string refreshToken = (string)fmt.GetType().GetMethod("Serialize").Invoke(fmt, new object[] { rt });
        Console.WriteLine("DNOA refresh token: " + refreshToken);
        Console.WriteLine("Key store now has: " + string.Join(", ", store.Keys.Keys));

        Func<string, string, byte[]> lookup = (b, h) => store.GetKey(b, h)?.Key;
        LegacyTokenContents c;
        Check(LegacyDnoaTokenReader.TryReadRefreshToken(refreshToken, lookup, out c), "decode DNOA refresh token");
        if (c != null)
        {
            Check(c.ClientIdentifier == "publicClientProxy", "client id = " + c.ClientIdentifier);
            Check(c.User == "pilot_joe", "user = " + c.User);
            Check(c.Scope.SetEquals(new[] { "currency", "readflight", "addflight" }), "scope = " + string.Join(" ", c.Scope));
            Check(c.UtcIssued >= before && c.UtcIssued <= DateTime.UtcNow.AddSeconds(1), "issued = " + c.UtcIssued.ToString("o"));
            Check(c.BagType == "DotNetOpenAuth.OAuth2.ChannelElements.RefreshToken", "bag type = " + c.BagType);
        }

        // Tampering / wrong keys must fail
        char[] chars = refreshToken.ToCharArray(); int pos = refreshToken.Length - 10; chars[pos] = chars[pos] == 'A' ? 'B' : 'A';
        Check(!LegacyDnoaTokenReader.TryReadRefreshToken(new string(chars), lookup, out c), "tampered token rejected");
        Check(!LegacyDnoaTokenReader.TryReadRefreshToken(refreshToken, (b, h) => new byte[32], out c), "wrong key rejected");
        Check(!LegacyDnoaTokenReader.TryReadRefreshToken(refreshToken, (b, h) => null, out c), "missing key rejected");
        Check(!LegacyDnoaTokenReader.TryReadRefreshToken("abcd!!!!", lookup, out c), "garbage rejected without throwing");

        // Round trip through DNOA's own decoder still works (sanity)
        object rt2 = Activator.CreateInstance(rtType, true);
        fmt.GetType().GetMethod("Deserialize", new[] { rtType, typeof(string), typeof(DotNetOpenAuth.Messaging.IProtocolMessage), typeof(string) })
            .Invoke(fmt, new object[] { rt2, refreshToken, null, null });
        Check((string)rtType.GetProperty("User").GetValue(rt2) == "pilot_joe", "DNOA itself decodes its own token (sanity)");

        // Second token reuses the same key handle (DNOA picks the newest key with > 1 day life)
        string refreshToken2 = (string)fmt.GetType().GetMethod("Serialize").Invoke(fmt, new object[] { rt });
        Check(refreshToken2.Split('!')[0] == refreshToken.Split('!')[0], "same key handle reused for subsequent tokens");

        // ---- Access token minted by real DNOA ----
        using (var rsa = new RSACryptoServiceProvider(1024))
        {
            var at = new AuthorizationServerAccessToken() { Lifetime = TimeSpan.FromDays(14), AccessTokenSigningKey = rsa, ResourceServerEncryptionKey = rsa };
            // AccessToken's ClientIdentifier/User/Scope come from AuthorizationDataBag
            at.ClientIdentifier = "someConfidentialClient";
            at.User = "pilot_joe";
            at.Scope.Add("totals");
            MethodInfo ser = typeof(AuthorizationServerAccessToken).GetMethod("Serialize", BindingFlags.NonPublic | BindingFlags.Instance);
            string accessToken = (string)ser.Invoke(at, null);
            Console.WriteLine("DNOA access token (" + accessToken.Length + " chars): " + accessToken.Substring(0, 40) + "...");

            Check(LegacyDnoaTokenReader.TryReadAccessToken(accessToken, rsa, rsa, out c), "decode DNOA access token");
            if (c != null)
            {
                Check(c.ClientIdentifier == "someConfidentialClient" && c.User == "pilot_joe" && c.Scope.SetEquals(new[] { "totals" }), "access token fields");
                Check(c.Lifetime == TimeSpan.FromDays(14), "lifetime = " + c.Lifetime);
                Check(!c.IsExpired(DateTime.UtcNow) && c.IsExpired(DateTime.UtcNow.AddDays(15)), "expiration logic");
                Check(c.BagType == "DotNetOpenAuth.OAuth2.AccessToken", "bag type = " + c.BagType);
            }
            using (var other = new RSACryptoServiceProvider(1024))
                Check(!LegacyDnoaTokenReader.TryReadAccessToken(accessToken, other, rsa, out c), "access token with wrong signing key rejected");
            Check(!LegacyDnoaTokenReader.TryReadRefreshToken(accessToken, lookup, out c), "access token not accepted as refresh token");
            Check(!LegacyDnoaTokenReader.TryReadAccessToken(refreshToken, rsa, rsa, out c), "refresh token not accepted as access token");
        }

        // ---- AuthorizationState JSON compatibility ----
        var dnoaState = new DotNetOpenAuth.OAuth2.AuthorizationState(new[] { "files.readwrite", "offline_access" })
        {
            AccessToken = "at-123", RefreshToken = "rt-456", Callback = new Uri("https://myflightbook.com/logbook/mvc/oAuth/OneDriveRedir?x=1"),
            AccessTokenIssueDateUtc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), AccessTokenExpirationUtc = new DateTime(2026, 1, 2, 4, 4, 5, DateTimeKind.Utc)
        };
        string json = JsonConvert.SerializeObject(dnoaState);
        Console.WriteLine("DNOA AuthorizationState JSON: " + json);
        var mine = JsonConvert.DeserializeObject<MyFlightbook.OAuth.AuthorizationState>(json);
        Check(mine.AccessToken == "at-123" && mine.RefreshToken == "rt-456", "tokens carried over");
        Check(mine.Callback == dnoaState.Callback, "callback carried over");
        Check(mine.AccessTokenIssueDateUtc == dnoaState.AccessTokenIssueDateUtc && mine.AccessTokenExpirationUtc == dnoaState.AccessTokenExpirationUtc, "dates carried over");
        Check(mine.Scope.SetEquals(dnoaState.Scope), "scopes carried over: " + string.Join(" ", mine.Scope));
        // And the reverse (in case of a rollback, new JSON must still load into DNOA's class)
        string jsonNew = JsonConvert.SerializeObject(mine);
        var back = JsonConvert.DeserializeObject<DotNetOpenAuth.OAuth2.AuthorizationState>(jsonNew);
        Check(back.RefreshToken == "rt-456" && back.Scope.SetEquals(dnoaState.Scope) && back.AccessTokenExpirationUtc == dnoaState.AccessTokenExpirationUtc, "new JSON loads into DNOA class (rollback-safe)");
        // Through the Profile prefs path: object -> dynamic -> reserialize -> T
        object asDynamic = JsonConvert.DeserializeObject(json);
        var viaPrefs = JsonConvert.DeserializeObject<MyFlightbook.OAuth.AuthorizationState>(JsonConvert.SerializeObject(asDynamic));
        Check(viaPrefs.RefreshToken == "rt-456" && viaPrefs.Scope.Count == 2, "prefs dictionary round-trip");

        Console.WriteLine(failures == 0 ? "\nALL PASSED" : "\n" + failures + " FAILURE(S)");
        return failures;
    }
}
