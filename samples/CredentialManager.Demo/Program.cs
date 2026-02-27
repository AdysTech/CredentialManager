using System.Net;
using System.Text.Json;
using AdysTech.CredentialManager;

#pragma warning disable SCS0015 // Hardcoded passwords — this is a demo application

// -----------------------------------------------------------------------
// CredentialManager Demo — exercises the full API surface
// -----------------------------------------------------------------------
// Run:  dotnet run --project samples/CredentialManager.Demo
// -----------------------------------------------------------------------

const string Target = "CredentialManager.Demo";
const string TargetAttribs = "CredentialManager.Demo.Attribs";

try
{
    // --- 1. Save a credential ---
    Console.WriteLine("=== 1. Save Credential ===");
    var netCred = new NetworkCredential("demo_user", "s3cret!", "WORKGROUP");
    var saved = CredentialManager.SaveCredentials(Target, netCred);
    if (saved != null)
        Console.WriteLine($"  Saved: target={saved.TargetName}, user={saved.UserName}, persistence={saved.Persistence}");
    else
        Console.WriteLine("  SaveCredentials returned null (unexpected).");

    // --- 2. Retrieve the credential ---
    Console.WriteLine("\n=== 2. Retrieve Credential ===");
    var retrieved = CredentialManager.GetCredentials(Target);
    if (retrieved != null)
        Console.WriteLine($"  Retrieved: user={retrieved.UserName}, password={retrieved.Password}, domain={retrieved.Domain}");
    else
        Console.WriteLine("  Not found.");

    // --- 3. ICredential with comment and attributes ---
    Console.WriteLine("\n=== 3. ICredential with Comment & Attributes ===");
    var iCred = new NetworkCredential("token_user", "eyJhbGciOi...").ToICredential()!;
    iCred.TargetName = TargetAttribs;
    iCred.Comment = "API token for demo service (visible via API only, not in Credential Manager UI)";
    iCred.Persistence = Persistence.Session;  // Not persisted across reboots
    iCred.Attributes = new Dictionary<string, object>(StringComparer.Ordinal)
    {
        { "role", "admin" },
        { "expiresAt", DateTime.UtcNow.AddHours(1).ToString("o") },
        { "retryCount", 3 }
    };
    iCred.SaveCredential();

    var iRetrieved = CredentialManager.GetICredential(TargetAttribs);
    if (iRetrieved != null)
    {
        Console.WriteLine($"  Target:      {iRetrieved.TargetName}");
        Console.WriteLine($"  User:        {iRetrieved.UserName}");
        Console.WriteLine($"  Comment:     {iRetrieved.Comment}");
        Console.WriteLine($"  Persistence: {iRetrieved.Persistence}");
        Console.WriteLine($"  Attributes:  {iRetrieved.Attributes?.Count ?? 0}");
        if (iRetrieved.Attributes != null)
        {
            foreach (var attr in iRetrieved.Attributes)
            {
                // Attributes come back as JsonElement — demonstrate typed access
                var je = (JsonElement)attr.Value;
                Console.WriteLine($"    [{attr.Key}] = {je} (kind: {je.ValueKind})");
            }
        }
    }

    // --- 4. Enumerate all credentials ---
    Console.WriteLine("\n=== 4. Enumerate Credentials ===");
    var all = CredentialManager.EnumerateICredentials();
    if (all != null)
    {
        Console.WriteLine($"  Total credentials in store: {all.Count}");
        // Show first 5
        foreach (var c in all.Take(5))
            Console.WriteLine($"    - {c.TargetName} (type={c.Type}, user={c.UserName})");
        if (all.Count > 5)
            Console.WriteLine($"    ... and {all.Count - 5} more");
    }
    else
    {
        Console.WriteLine("  No credentials found.");
    }

    // --- 5. Basic Auth header ---
    Console.WriteLine("\n=== 5. Basic Auth Header ===");
    if (retrieved != null)
    {
        var authString = retrieved.GetBasicAuthString();
        Console.WriteLine($"  Authorization: Basic {authString}");
    }

    // --- 6. Persistence options ---
    Console.WriteLine("\n=== 6. Persistence Options ===");
    Console.WriteLine($"  Session:      {Persistence.Session} ({(uint)Persistence.Session})");
    Console.WriteLine($"  LocalMachine: {Persistence.LocalMachine} ({(uint)Persistence.LocalMachine}) [default]");
    Console.WriteLine($"  Enterprise:   {Persistence.Enterprise} ({(uint)Persistence.Enterprise})");

    // --- 7. Error handling ---
    Console.WriteLine("\n=== 7. Error Handling ===");
    var missing = CredentialManager.GetCredentials("NonExistent_Target_12345");
    Console.WriteLine($"  GetCredentials for missing target: {(missing == null ? "null (correct)" : "unexpected value")}");

    try
    {
        CredentialManager.SaveCredentials(null!, netCred);
    }
    catch (ArgumentNullException ex)
    {
        Console.WriteLine($"  Null target throws: {ex.GetType().Name} (correct)");
    }

    // --- Cleanup ---
    Console.WriteLine("\n=== Cleanup ===");
    CredentialManager.RemoveCredentials(Target);
    Console.WriteLine($"  Removed: {Target}");
    CredentialManager.RemoveCredentials(TargetAttribs);
    Console.WriteLine($"  Removed: {TargetAttribs}");

    Console.WriteLine("\nAll demos completed successfully.");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\nError: {ex.GetType().Name}: {ex.Message}");
    Console.ResetColor();

    // Attempt cleanup on error — ignore failures since the credential may not exist
    try { CredentialManager.RemoveCredentials(Target); } catch (CredentialAPIException) { /* best-effort cleanup */ }
    try { CredentialManager.RemoveCredentials(TargetAttribs); } catch (CredentialAPIException) { /* best-effort cleanup */ }

    return 1;
}

return 0;
