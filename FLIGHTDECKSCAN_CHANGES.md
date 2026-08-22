# FlightDeckScan: what changed in MyFlightbookWeb

Implements the MCDU/FMC/ACARS photo-scan feature directly in the existing web
backend (per our design discussion), rather than as a separate service:
web, Android, and iOS all hit one new authenticated, donor-gated endpoint.

**This code has not been compiled.** No .NET/C# toolchain was available in
the sandbox this was written in - everything below was written by closely
mirroring existing patterns in your codebase (SafeOp, ProcessForImageUpload,
the Gratuity system, LocalConfig, resx/designer.cs pairs), but you'll need to
open it in Visual Studio, build, and fix up anything that doesn't compile
cleanly before this is usable.

## New files

All of the FlightDeckScan logic now lives in its own class library,
`MyFlightbook.FlightDeckScan` (new top-level folder, sibling to
`MyFlightbook.Web`), the same way `MyFlightbook.Telemetry` is separate from
`MyFlightbook.Web`. It targets `netstandard2.0` (same as `MyFlightbook.Data`
and `MyFlightbook.Telemetry`), so it's consumable both by the .NET Framework
web project and by a modern net8.0 test project. `MyFlightbook.Web.csproj`
references it via a normal `<ProjectReference>` instead of compiling the
files directly.

- `MyFlightbook.FlightDeckScan/FlightDeckScanModels.cs` - the data shapes:
  `RawExtraction` (what the model reads off the screen, unnormalized),
  `ScanResult` (the final response), and supporting
  `TimeValue`/`DateValue`/`DurationValue`/`OoiTimes`.
- `FlightDeckScanPrompt.cs` - the system prompt sent to the vision model.
- `FlightDeckScanSchema.cs` - builds the forced-tool-use JSON schema sent to
  the Anthropic API, so the model's reply is always the `RawExtraction`
  shape rather than free-form prose.
- `FlightDeckScanNormalizer.cs` - **the important one.** Pure, static,
  dependency-free logic that turns a `RawExtraction` into a `ScanResult`:
  parses OOOI times (`1331Z`, `13:31`, `05:02:40`, all normalize to `HH:MM`),
  parses dates (`07AUG26`, or `09AUG` with no year - infers the most recent
  non-future year), detects a flight still in progress (OUT/OFF/ON present,
  IN blank), cross-checks BLOCK/FLIGHT against computed OUT/IN and OFF/ON
  and flags (but keeps) a printed value that disagrees, and returns a clean
  `Success: false` + `Error` when nothing could be identified. This is a
  line-by-line port of a Node prototype whose equivalent logic passed 11
  unit tests against your 5 sample images from issue #1577.
- `FlightDeckScanClient.cs` - calls the Anthropic Messages API (forced
  tool-use) with the image, using a static shared `HttpClient`. Reads
  `AnthropicApiKey` (required) and `AnthropicScanModel` (optional, defaults
  to `claude-sonnet-4-5-20250929`) via `LocalConfig.SettingForKey(...)` -
  **you need to add an `AnthropicApiKey` row to the `localconfig` DB table**
  before this works (same place your other secrets/keys already live, not
  web.config - see "Where the API key goes" below).

`MyFlightbook.FlightDeckScan.Tests/FlightDeckScanNormalizerTests.cs` (new
top-level folder, sibling to `MyFlightbook.Telemetry.Tests`, mirroring its
`.csproj` exactly - net8.0, MSTest) - an MSTest port of the same 11 test
cases, covering all 5 sample screens plus edge cases (non-flight-deck photo,
glared-out screen, disagreeing BLOCK time). This is now wired into
`MyFlightbook.sln` and referenced from `MyFlightbook.FlightDeckScan.Tests.csproj`,
so it should show up in Solution Explorer and be runnable from Test Explorer
once you reload the solution.

## Where the API key goes

`AnthropicApiKey` and `AnthropicScanModel` are **not** set in Visual Studio,
web.config, or any project file - they're rows in your `localconfig` MySQL
table, read at runtime via `LocalConfig.SettingForKey(...)` (same mechanism
your other secrets already use). Add them with your own DB client:

```sql
INSERT INTO localconfig (keyName, keyValue) VALUES ('AnthropicApiKey', '<your key from console.anthropic.com>');
-- optional - only needed if you want something other than the built-in default:
INSERT INTO localconfig (keyName, keyValue) VALUES ('AnthropicScanModel', 'claude-sonnet-4-5-20250929');
```

`AnthropicScanModel` is optional; if the row is missing, the code falls back
to `claude-sonnet-4-5-20250929` already, so you only need it if you want to
pin a different model later.

## Changed files

- **`ImageController.cs`**
  - `ProcessForImageUpload` gets one new early-return branch: if there's no
    `txtAuthToken` at all AND the caller has an authenticated browser
    session (forms-auth cookie), use that. This is purely additive - it
    only fires when neither a token nor an OAuth header is present, so
    existing native-app and OAuth callers are unaffected. This is what lets
    the website call the same endpoint as the apps.
  - New action: `ScanFlightDeckImage(string txtAuthToken)`. Follows the
    exact shape of `UploadFlightImage` right above it: `ProcessForImageUpload`
    for auth, then `EarnedGratuity.UserQualifies(szUser, GratuityTypes.FlightDeckScan)`
    for the donor gate (throws `MyFlightbookException` with the new
    `errNotAuthorizedFlightDeckScan` message if not qualifying - `SafeOp`
    turns that into an HTTP 400 with the message as the body, same as the
    existing video-upload gate), then calls `FlightDeckScanClient` +
    `FlightDeckScanNormalizer` and returns the `ScanResult` as camelCase
    JSON (via a `CamelCasePropertyNamesContractResolver`, so field names
    match what a JS/Kotlin/Swift client would expect: `flightNumber`,
    `blockTime.hhmm`, etc.).

- **`AppCode/Utility/Payment.cs`**
  - Added `GratuityTypes.FlightDeckScan` to the enum, a case in
    `GratuityFromType`, and a new `FlightDeckScanGratuity : Gratuity` class
    mirroring `StoreVideosGratuity` exactly - same $10/366-day terms, per
    your call to keep it consistent with the video gratuity tier.

- **`App_GlobalResources/LocalizedText.resx` + `.designer.cs`**
  - Added `GratuityNameFlightDeckScan`, `GratuityThanksFlightDeckScan`,
    `GratuityDescriptionFlightDeckScan` (mirroring the `...Video` triplet),
    and `errNotAuthorizedFlightDeckScan` (mirroring `errNotAuthorizedVideos`).
    I hand-edited both the `.resx` and its `.designer.cs` in the same
    pattern as the existing Video entries - if Visual Studio regenerates
    `.designer.cs` from the `.resx` on next save, that's expected and fine.

- **`MyFlightbook.Web.csproj`**
  - Removed the `<Compile Include="...">` entries that used to point at
    `AppCode\FlightDeckScan\*.cs` (that code moved to the new
    `MyFlightbook.FlightDeckScan` project - see above) and added a
    `<ProjectReference>` to `MyFlightbook.FlightDeckScan.csproj` instead,
    in the same alphabetical spot as the other project references.

- **`MyFlightbook.sln`**
  - Registered the two new projects, `MyFlightbook.FlightDeckScan` and
    `MyFlightbook.FlightDeckScan.Tests`, so they show up in Solution
    Explorer and Test Explorer.

## One cleanup step you need to do by hand

The 5 FlightDeckScan `.cs` files used to live at
`MyFlightbook.Web\AppCode\FlightDeckScan\`. They've been moved to the new
`MyFlightbook.FlightDeckScan\` project folder, and `MyFlightbook.Web.csproj`
no longer compiles anything from the old location - but I can't delete
files on your machine, only write/move them through the folder you shared.
**Please delete the now-empty `MyFlightbook.Web\AppCode\FlightDeckScan\`
folder yourself** once you've confirmed the new structure builds. It should
already be empty; if it isn't (e.g. some leftover from a partial sync),
double-check before deleting.

## Before this runs

1. Add `AnthropicApiKey` (and optionally `AnthropicScanModel`) rows to the
   `localconfig` table - see "Where the API key goes" above.
2. Delete the orphaned `MyFlightbook.Web\AppCode\FlightDeckScan\` folder
   (see above).
3. Open in Visual Studio - it should prompt to reload since the `.sln` and
   two `.csproj` files changed. Build, fix whatever doesn't compile (I'd bet
   on small things - a namespace resolution or two - rather than anything
   structural, since every pattern here was copied from working code in
   your own repo, but I genuinely can't promise it compiles clean on the
   first try without a toolchain to check it).
4. Run the tests in `MyFlightbook.FlightDeckScan.Tests` from Test Explorer.
5. Smoke-test `POST /mvc/Image/ScanFlightDeckImage` against your 5 sample
   images once a build is running, both as a donor and non-donor account,
   and once with no `txtAuthToken` while logged into the website to confirm
   the cookie-auth fallback works.

## Not done yet (next steps, your call on priority)

- **The website UI**: a page/partial with a file input and JS that POSTs to
  `ScanFlightDeckImage` and pre-fills a pending flight form from the
  response. Nothing built here yet.
- **Android/iOS client updates**: the Kotlin/Swift client libraries from
  earlier in this conversation were written against the throwaway Node
  prototype's `x-api-key` header scheme - they need to be rewritten to POST
  multipart with a `txtAuthToken` field (matching how the apps already call
  `UploadFlightImage`) against this real endpoint instead.
- **The logging/review queue** for scans that fail or get overridden, which
  we agreed to treat as a fast-follow after this core feature ships.
