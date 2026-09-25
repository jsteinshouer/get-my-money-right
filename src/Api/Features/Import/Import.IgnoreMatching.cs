namespace Api.Features.Import;

public static partial class Import
{
    /// <summary>
    /// Holding a rule up against a row. Deliberately the dullest matching imaginable — case-insensitive
    /// text, against the same normalised description the preview prints — because a household member
    /// writes a rule by reading a row, and a rule that matches something other than what they read is
    /// a rule they cannot reason about. No regex, no wildcards.
    /// </summary>
    public static class IgnoreMatching
    {
        /// <summary>
        /// The first rule that catches this description, or null. First by id, so the reason printed
        /// against a row is the same one every time it is read.
        /// </summary>
        public static ImportIgnoreRule? FirstMatch(IEnumerable<ImportIgnoreRule> rules, string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return null;
            }

            return rules.FirstOrDefault(rule => Matches(rule, description));
        }

        public static bool Matches(ImportIgnoreRule rule, string description) => rule.MatchType switch
        {
            IgnoreMatchType.Contains => description.Contains(rule.MatchText, StringComparison.OrdinalIgnoreCase),
            IgnoreMatchType.StartsWith => description.StartsWith(rule.MatchText, StringComparison.OrdinalIgnoreCase),
            IgnoreMatchType.Equals => description.Equals(rule.MatchText, StringComparison.OrdinalIgnoreCase),
            _ => false,
        };

        /// <summary>
        /// The rule said back in words — "contains AUTOPAY" — so a struck row names what caught it and
        /// an over-broad rule is diagnosable at a glance rather than by opening the rules page.
        /// </summary>
        public static string Describe(ImportIgnoreRule rule) => rule.MatchType switch
        {
            IgnoreMatchType.StartsWith => $"starts with {rule.MatchText}",
            IgnoreMatchType.Equals => $"is {rule.MatchText}",
            _ => $"contains {rule.MatchText}",
        };
    }
}
