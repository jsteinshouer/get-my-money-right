namespace Api.Features.Import;

public static partial class Import
{
    /// <summary>
    /// One CSV record read through a column mapping: a parsed date, a normalised description and a
    /// sign-normalised amount, or an honest account of why it could not be read. Both previews — the
    /// Map step's five-row rehearsal and station 3's whole file — read a row the same way, because a
    /// rehearsal that reads differently from the performance is not a rehearsal.
    /// </summary>
    public static class RowReader
    {
        /// <summary>What the household member has said about the file's shape so far.</summary>
        public record class Mapping(
            string Delimiter,
            bool HasHeaderRow,
            string DateFormat,
            string? DateColumn,
            string? DescriptionColumn,
            string? AmountColumn,
            string? DebitColumn,
            string? CreditColumn);

        public record class Read(DateOnly? Date, string? Description, decimal? Amount, string? Error);

        /// <summary>
        /// Splits the file and names its columns. The delimiter and the header flag are the household
        /// member's to correct, so the file is re-split on every read rather than reusing the upload's
        /// guess.
        /// </summary>
        public static (List<string> Columns, List<List<string>> Records) Split(string text, Mapping mapping)
        {
            var delimiter = Csv.IsSupportedDelimiter(mapping.Delimiter) ? mapping.Delimiter[0] : ',';
            var records = Csv.Split(text, delimiter);
            if (records.Count == 0)
            {
                return ([], []);
            }

            var columns = mapping.HasHeaderRow
                ? Csv.ColumnNames(records[0])
                : Csv.PositionalColumnNames(records[0].Count);
            var dataRecords = (mapping.HasHeaderRow ? records.Skip(1) : records)
                .Select(r => r.Select(c => c.Trim()).ToList())
                .ToList();

            return (columns, dataRecords);
        }

        public static Read ReadRow(List<string> record, List<string> columns, Mapping mapping)
        {
            var problems = new List<string>();

            DateOnly? date = null;
            var rawDate = Cell(record, columns, mapping.DateColumn);
            if (rawDate is not null)
            {
                if (Csv.TryParseDate(rawDate, mapping.DateFormat, out var parsed))
                {
                    date = parsed;
                }
                else
                {
                    problems.Add($"\"{rawDate}\" is not a date in {mapping.DateFormat} form.");
                }
            }

            var description = Csv.NormaliseDescription(Cell(record, columns, mapping.DescriptionColumn));

            decimal? amount = null;
            if (!string.IsNullOrEmpty(mapping.AmountColumn))
            {
                var rawAmount = Cell(record, columns, mapping.AmountColumn);
                if (rawAmount is null)
                {
                    problems.Add("This row has no amount.");
                }
                else if (Csv.TryParseAmount(rawAmount, out var parsed))
                {
                    amount = parsed;
                }
                else
                {
                    problems.Add($"\"{rawAmount}\" is not an amount.");
                }
            }
            else if (!string.IsNullOrEmpty(mapping.DebitColumn) || !string.IsNullOrEmpty(mapping.CreditColumn))
            {
                amount = ReadDebitCredit(record, columns, mapping, problems);
            }

            return new Read(date, description, amount, problems.Count == 0 ? null : string.Join(" ", problems));
        }

        /// <summary>
        /// Money out is negative and money in is positive, whatever signs the two columns carried —
        /// that normalisation is the whole reason the pair exists as a mapping option.
        /// </summary>
        private static decimal? ReadDebitCredit(
            List<string> record, List<string> columns, Mapping mapping, List<string> problems)
        {
            var rawDebit = Cell(record, columns, mapping.DebitColumn);
            var rawCredit = Cell(record, columns, mapping.CreditColumn);

            decimal? debit = null;
            if (rawDebit is not null)
            {
                if (!Csv.TryParseAmount(rawDebit, out var parsed))
                {
                    problems.Add($"\"{rawDebit}\" is not an amount.");
                    return null;
                }

                debit = parsed;
            }

            decimal? credit = null;
            if (rawCredit is not null)
            {
                if (!Csv.TryParseAmount(rawCredit, out var parsed))
                {
                    problems.Add($"\"{rawCredit}\" is not an amount.");
                    return null;
                }

                credit = parsed;
            }

            // Plenty of banks zero-fill the unused side rather than leaving it blank, so the side
            // that actually carries a figure decides — not merely the side that is non-empty.
            var debitUsed = debit is not null && debit != 0m;
            var creditUsed = credit is not null && credit != 0m;

            if (debitUsed && creditUsed)
            {
                problems.Add("This row has both a debit and a credit, so its amount is ambiguous.");
                return null;
            }

            if (debitUsed)
            {
                return -Math.Abs(debit!.Value);
            }

            if (creditUsed)
            {
                return Math.Abs(credit!.Value);
            }

            if (debit is null && credit is null)
            {
                problems.Add("This row has neither a debit nor a credit.");
                return null;
            }

            // Both sides read as zero, which is what the file says.
            return 0m;
        }

        /// <summary>An unassigned role, an unknown column and a blank cell all read as nothing.</summary>
        private static string? Cell(List<string> record, List<string> columns, string? columnName)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                return null;
            }

            var index = columns.IndexOf(columnName);
            if (index < 0 || index >= record.Count)
            {
                return null;
            }

            var value = record[index];
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
