using System;

// Keep discovery side effects in memory in tests that acquire or merge items.
internal sealed class CodexDiscoveryTestScope : IDisposable
{
    private sealed class MemoryStore : ICodexDiscoveryStore
    {
        private string json;
        public string Read() => json;
        public void Write(string value) => json = value;
    }

    private readonly IDisposable scope = CodexDiscoveryHistory.OverrideCurrent(
        new CodexDiscoveryHistory(new MemoryStore()));

    public void Dispose() => scope.Dispose();
}
