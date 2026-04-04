namespace SlipSnap.E2ETests.Helpers;

public class AppFixture : IDisposable
{
    public AppFixture()
    {
        // Stop any leftover instances, then launch fresh (with build to ensure exe exists)
        try { ScriptRunner.Run("stop-app.ps1"); } catch { /* ignore */ }
        var result = ScriptRunner.Run("launch-app.ps1");
        ProcessId = result.RootElement.GetProperty("pid").GetInt32();
    }

    public int ProcessId { get; }

    public void Dispose()
    {
        try { ScriptRunner.Run("stop-app.ps1"); } catch { /* ignore */ }
    }
}

[CollectionDefinition("App")]
public class AppCollection : ICollectionFixture<AppFixture> { }
