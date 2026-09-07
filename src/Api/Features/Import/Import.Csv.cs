using System.Globalization;
using System.Text;

namespace Api.Features.Import;

public static partial class Import
{
    /// <summary>
    /// Reading a bank export, and guessing as little as possible while doing it. The delimiter, the
    /// header row and the date format are detected because a household member has no reason to know
    /// what <c>MM/dd/yyyy</c> means; which column holds the date is never guessed, because a wrong
    /// guess nobody notices is worse than an honest blank.
    /// </summary>
    public static class Csv
    {
        /// <summary>Enough rows to see a date pattern and a sign, few enough to read at a glance.</summary>
        public const int SampleRowCount = 5;

        private static readonly char[] CandidateDelimiters = [',', ';', '\t', '|'];

        /// <summary>
        /// Ordered by preference, so an ambiguous value like <c>08/09/2026</c> resolves the way this
        /// household's banks write it. Month-first beats day-first for exactly that reason.
        /// </summary>
        private static readonly string[] CandidateDateFormats =
        [
            "yyyy-MM-dd",
            "MM/dd/yyyy",
            "M/d/yyyy",
            "dd/MM/yyyy",
            "d/M/yyyy",
            "yyyy/MM/dd",
            "MM-dd-yyyy",
            "dd-MM-yyyy",
            "dd.MM.yyyy",
            "d.M.yyyy",
            "MM/dd/yy",
            "M/d/yy",
        ];

        public const string FallbackDateFormat = "yyyy-MM-dd";

        public static bool IsSupportedDelimiter(string? delimiter) =>
            delimiter is { Length: 1 } && CandidateDelimiters.Contains(delimiter[0]);

        public static bool IsSupportedDateFormat(string? dateFormat) =>
            dateFormat is not null && CandidateDateFormats.Contains(dateFormat);

        /// <summary>Splits the whole file into records, honouring RFC 4180 quoting.</summary>
        public static List<List<string>> Split(string text, char delimiter)
        {
            var records = new List<List<string>>();
            var fields = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;
            var sawAnything = false;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // A doubled quote inside a quoted field is a literal quote.
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }
                    sawAnything = true;
                    continue;
                }

                if (c == '"')
                {
                    inQuotes = true;
                    sawAnything = true;
                }
                else if (c == delimiter)
                {
                    fields.Add(field.ToString());
                    field.Clear();
                    sawAnything = true;
                }
                else if (c is '\r' or '\n')
                {
                    // Swallow the LF of a CRLF pair so it doesn't open an empty record.
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }

                    fields.Add(field.ToString());
                    field.Clear();
                    if (sawAnything)
                    {
                        records.Add(fields);
                    }
                    fields = [];
                    sawAnything = false;
                }
                else
                {
                    field.Append(c);
                    sawAnything = true;
                }
            }

            fields.Add(field.ToString());
            if (sawAnything)
            {
                records.Add(fields);
            }

            return records;
        }

        /// <summary>
        /// The delimiter that yields more than one column and the same column count on every line.
        /// Ties go to the earlier candidate, so a plain comma export is never read as something else.
        /// </summary>
        public static char DetectDelimiter(string text)
        {
            var best = CandidateDelimiters[0];
            var bestColumns = 0;

            foreach (var candidate in CandidateDelimiters)
            {
                var records = Split(text, candidate).Take(6).ToList();
                if (records.Count == 0)
                {
                    continue;
                }

                var columns = records[0].Count;
                if (columns <= 1 || records.Any(r => r.Count != columns))
                {
                    continue;
                }

                if (columns > bestColumns)
                {
                    best = candidate;
                    bestColumns = columns;
                }
            }

            return best;
        }

        /// <summary>
        /// A first line is a header when nothing in it reads as a figure or a date. Bank exports that
        /// open straight into data are rarer, but they exist, and they must not lose their first row.
        /// </summary>
        public static bool DetectHasHeaderRow(List<List<string>> records)
        {
            if (records.Count == 0)
            {
                return false;
            }

            return !records[0].Any(cell =>
                TryParseAmount(cell, out _) || CandidateDateFormats.Any(f => TryParseDate(cell, f, out _)));
        }

        /// <summary>
        /// Scans every sampled cell for the format that reads the most values. This finds the format
        /// without deciding which column is the date — the roles stay the household member's call.
        /// </summary>
        public static string DetectDateFormat(IEnumerable<List<string>> dataRecords)
        {
            var cells = dataRecords.Take(SampleRowCount).SelectMany(r => r).ToList();
            var best = FallbackDateFormat;
            var bestHits = 0;

            foreach (var format in CandidateDateFormats)
            {
                var hits = cells.Count(cell => TryParseDate(cell, format, out _));
                if (hits > bestHits)
                {
                    best = format;
                    bestHits = hits;
                }
            }

            return best;
        }

        public static bool TryParseDate(string raw, string? format, out DateOnly value)
        {
            var trimmed = raw.Trim();
            if (trimmed.Length == 0 || string.IsNullOrEmpty(format))
            {
                value = default;
                return false;
            }

            return DateOnly.TryParseExact(trimmed, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
        }

        /// <summary>
        /// Reads a money cell the way exports actually write one: currency symbols, a leading plus,
        /// accountancy's parenthesised negative, and either separator convention — <c>2,410.55</c> or
        /// <c>2.410,55</c>. A cell it cannot read with certainty is refused rather than guessed at: a
        /// wrong figure that looks plausible is the one failure this screen exists to prevent.
        /// </summary>
        public static bool TryParseAmount(string raw, out decimal value)
        {
            value = 0m;
            var trimmed = raw.Trim();
            if (trimmed.Length == 0)
            {
                return false;
            }

            var negative = false;
            if (trimmed.StartsWith('(') && trimmed.EndsWith(')'))
            {
                negative = true;
                trimmed = trimmed[1..^1].Trim();
            }

            var stripped = new StringBuilder(trimmed.Length);
            foreach (var c in trimmed)
            {
                if (char.IsAsciiDigit(c) || c is '.' or ',' or '-' or '+')
                {
                    stripped.Append(c);
                }
                else if (c is ' ' or '\u00a0' or '$' or '\u00a3' or '\u20ac')
                {
                    // Currency marks and spacing are noise, not information.
                    continue;
                }
                else
                {
                    return false;
                }
            }

            var digits = stripped.ToString();
            if (digits.StartsWith('-'))
            {
                negative = !negative;
                digits = digits[1..];
            }
            else if (digits.StartsWith('+'))
            {
                digits = digits[1..];
            }

            // A sign anywhere but the front is not a figure this app should pretend to understand.
            if (digits.Length == 0 || digits.Contains('-') || digits.Contains('+'))
            {
                return false;
            }

            if (!TryNormaliseSeparators(digits, out var normalised))
            {
                return false;
            }

            if (!decimal.TryParse(
                    normalised, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value))
            {
                return false;
            }

            if (negative)
            {
                value = -value;
            }

            return true;
        }

        /// <summary>
        /// Resolves which of <c>.</c> and <c>,</c> is the decimal point. With both present the later one
        /// decides, which is true of every convention in use. With only commas, a single one trailed by
        /// one or two digits is a decimal comma and anything else must be well-formed thousands groups.
        /// </summary>
        private static bool TryNormaliseSeparators(string digits, out string normalised)
        {
            normalised = string.Empty;
            var lastDot = digits.LastIndexOf('.');
            var lastComma = digits.LastIndexOf(',');

            if (lastDot >= 0 && lastComma >= 0)
            {
                var decimalSeparator = lastDot > lastComma ? '.' : ',';
                var groupSeparator = decimalSeparator == '.' ? ',' : '.';
                if (!GroupsAreWellFormed(digits, decimalSeparator, groupSeparator))
                {
                    return false;
                }

                normalised = digits.Replace(groupSeparator.ToString(), string.Empty).Replace(decimalSeparator, '.');
                return true;
            }

            if (lastComma >= 0)
            {
                var parts = digits.Split(',');
                if (parts.Length == 2 && parts[1].Length is 1 or 2)
                {
                    normalised = digits.Replace(',', '.');
                    return true;
                }

                if (!AreThousandsGroups(parts))
                {
                    return false;
                }

                normalised = digits.Replace(",", string.Empty);
                return true;
            }

            if (lastDot >= 0)
            {
                var parts = digits.Split('.');
                if (parts.Length == 2)
                {
                    normalised = digits;
                    return true;
                }

                // Repeated dots can only be grouping; a second decimal point is not a figure.
                if (!AreThousandsGroups(parts))
                {
                    return false;
                }

                normalised = digits.Replace(".", string.Empty);
                return true;
            }

            normalised = digits;
            return true;
        }

        private static bool GroupsAreWellFormed(string digits, char decimalSeparator, char groupSeparator)
        {
            var decimalIndex = digits.LastIndexOf(decimalSeparator);
            var fraction = digits[(decimalIndex + 1)..];
            if (fraction.Length == 0 || fraction.Contains(groupSeparator) || fraction.Contains(decimalSeparator))
            {
                return false;
            }

            return AreThousandsGroups(digits[..decimalIndex].Split(groupSeparator));
        }

        private static bool AreThousandsGroups(string[] parts) =>
            parts.Length >= 2
            && parts[0].Length is >= 1 and <= 3
            && parts.Skip(1).All(part => part.Length == 3);

        /// <summary>Column names when the file has no header row of its own.</summary>
        public static List<string> PositionalColumnNames(int count) =>
            Enumerable.Range(1, count).Select(i => $"Column {i}").ToList();

        /// <summary>
        /// The header row's names, made unique. A column's name is its identity — in the mapping table,
        /// in the saved mapping, and when reading a row — so a blank or repeated one (an export with two
        /// Amount columns, or a trailing comma) would otherwise make one of them unmappable.
        /// </summary>
        public static List<string> ColumnNames(List<string> headerRecord)
        {
            var names = new List<string>(headerRecord.Count);
            var taken = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < headerRecord.Count; i++)
            {
                var name = headerRecord[i].Trim();
                if (name.Length == 0)
                {
                    name = $"Column {i + 1}";
                }

                if (!taken.Add(name))
                {
                    var suffix = 2;
                    string candidate;
                    do
                    {
                        candidate = $"{name} ({suffix})";
                        suffix++;
                    }
                    while (!taken.Add(candidate));
                    name = candidate;
                }

                names.Add(name);
            }

            return names;
        }
    }
}
