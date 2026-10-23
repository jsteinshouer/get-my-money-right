using static Api.Features.Transactions.Transactions;

namespace Api.Features.Import;

public static partial class Import
{
    /// <summary>An assignment rule as the pipeline holds it: the rule, and the tags it applies.</summary>
    public record class AssignmentRuleInForce(ImportAssignmentRule Rule, IReadOnlyList<int> TagIds);

    /// <summary>
    /// What the rules decided for one row, and which rules decided it. A rule that matched but whose
    /// every field an earlier rule had already settled decided nothing, and is not cited.
    /// </summary>
    public record class Assignment(
        int? CategoryId,
        NeedWant? NeedWant,
        IReadOnlyList<int> TagIds,
        IReadOnlyList<ImportAssignmentRule> DecidedBy);

    /// <summary>
    /// Reading a row through every assignment rule in force. Earliest rule wins — category and
    /// Need/Want each come from the first matching rule by id that sets them, the same first-by-id
    /// convention the ignore rules use — while tags accumulate across every matching rule, so a
    /// purchase tracked from two angles keeps both.
    /// </summary>
    public static class AssignmentResolution
    {
        /// <param name="rules">The rules in force, in id order.</param>
        /// <returns>Null when no rule decided anything about this row.</returns>
        public static Assignment? Resolve(IEnumerable<AssignmentRuleInForce> rules, string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return null;
            }

            int? categoryId = null;
            NeedWant? needWant = null;
            var tagIds = new List<int>();
            var decidedBy = new List<ImportAssignmentRule>();

            foreach (var (rule, ruleTagIds) in rules.Where(r => RuleMatching.Matches(r.Rule, description)))
            {
                var decided = false;
                if (categoryId is null && rule.CategoryId is not null)
                {
                    categoryId = rule.CategoryId;
                    decided = true;
                }

                if (needWant is null && rule.NeedWant is not null)
                {
                    needWant = rule.NeedWant;
                    decided = true;
                }

                var newTags = ruleTagIds.Where(id => !tagIds.Contains(id)).ToList();
                if (newTags.Count > 0)
                {
                    tagIds.AddRange(newTags);
                    decided = true;
                }

                if (decided)
                {
                    decidedBy.Add(rule);
                }
            }

            return decidedBy.Count == 0 ? null : new Assignment(categoryId, needWant, tagIds, decidedBy);
        }
    }
}
