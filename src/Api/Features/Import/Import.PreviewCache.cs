using System.Collections.Concurrent;

namespace Api.Features.Import;

public static partial class Import
{
    /// <summary>
    /// Holds an uploaded file between the Upload and Map Columns steps. Nothing here is a record of
    /// anything — it is the file the household member is still looking at — so it lives in memory and
    /// expires on its own rather than earning a table.
    /// </summary>
    public class PreviewCache
    {
        public static readonly TimeSpan Lifetime = TimeSpan.FromHours(2);

        /// <summary>A two-person household is never mid-way through more uploads than this.</summary>
        public const int MaxEntries = 32;

        private readonly ConcurrentDictionary<string, Entry> _entries = new();

        public record class Entry(int AccountId, string FileName, string Text, DateTimeOffset ExpiresAt);

        public string Add(int accountId, string fileName, string text)
        {
            Prune();

            // Only the newest upload for an account is ever read back, so an earlier one is dead weight
            // holding a whole file in memory.
            foreach (var (existingToken, existing) in _entries)
            {
                if (existing.AccountId == accountId)
                {
                    _entries.TryRemove(existingToken, out _);
                }
            }

            var token = Guid.NewGuid().ToString("N");
            _entries[token] = new Entry(accountId, fileName, text, DateTimeOffset.UtcNow.Add(Lifetime));

            // A hard ceiling, so abandoned previews cannot accumulate until something else evicts them.
            while (_entries.Count > MaxEntries)
            {
                var oldest = _entries.OrderBy(e => e.Value.ExpiresAt).First();
                _entries.TryRemove(oldest.Key, out _);
            }

            return token;
        }

        public Entry? Find(string token)
        {
            if (!_entries.TryGetValue(token, out var entry))
            {
                return null;
            }

            if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                _entries.TryRemove(token, out _);
                return null;
            }

            return entry;
        }

        private void Prune()
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var (token, entry) in _entries)
            {
                if (entry.ExpiresAt <= now)
                {
                    _entries.TryRemove(token, out _);
                }
            }
        }
    }
}
