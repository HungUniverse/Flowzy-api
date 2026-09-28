using Cronos;
using Flowzy.Service.Exceptions;
using System.Text.RegularExpressions;

namespace Flowzy.Service.Backup;

public sealed class BackupCron
{
    private readonly IReadOnlyList<CronExpression> expressions;
    private BackupCron(IReadOnlyList<CronExpression> expressions) => this.expressions = expressions;

    public static BackupCron Parse(string text)
    {
        try
        {
            text = text.Trim();
            text = text.ToLowerInvariant() switch
            {
                "@yearly" or "@annually" => "0 0 0 1 1 *", "@monthly" => "0 0 0 1 * *",
                "@weekly" => "0 0 0 * * 0", "@daily" or "@midnight" => "0 0 0 * * *", "@hourly" => "0 0 * * * *", _ => text
            };
            var fields = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length != 6) throw new FormatException();
            fields[4] = ReplaceNames(fields[4], ["JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"], 1);
            fields[5] = ReplaceNames(fields[5], ["SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT"], 0);
            // Cronos accepts extra syntax that Spring does not: reject it before parsing.
            for (var i = 0; i < fields.Length; i++)
            {
                if (fields[i].Contains('?') && (i is not (3 or 5) || fields[i] != "?")) throw new FormatException();
                if (Regex.IsMatch(fields[i], @"L-\d+W", RegexOptions.IgnoreCase)) throw new FormatException();
                foreach (Match range in Regex.Matches(fields[i], @"(?<!L)(\d+)-(\d+)", RegexOptions.IgnoreCase))
                    if (int.Parse(range.Groups[1].Value) > int.Parse(range.Groups[2].Value)) throw new FormatException();
            }
            // Spring allows comma-separated special day selectors (e.g. L,15 or MONL,FRIL).
            // Cronos parses each selector separately; the union of cross-products preserves AND between day fields.
            var expressions = new List<CronExpression>();
            foreach (var monthDay in fields[3].Split(','))
            foreach (var weekDay in fields[5].Split(','))
                expressions.Add(CronExpression.Parse($"{fields[0]} {fields[1]} {fields[2]} {monthDay} {fields[4]} {weekDay}", CronFormat.IncludeSeconds));
            return new(expressions);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or NullReferenceException)
        { throw new BadRequestException("Invalid cron expression"); }
    }

    private static string ReplaceNames(string text, string[] names, int first)
    {
        text = text.ToUpperInvariant();
        for (var i = 0; i < names.Length; i++) text = text.Replace(names[i], (i + first).ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
        return text;
    }

    public static BackupTimeZone Zone(string text)
    {
        try
        {
            text = text.Trim();
            if (text == "Z") return new("Z", null, TimeSpan.Zero);
            // Java ZoneId accepts IANA names and fixed offsets, not Windows timezone IDs.
            if (text == "UTC" || text == "GMT" || text == "UT") return new(text, null, TimeSpan.Zero);
            var offset = Regex.Match(text, @"^(UTC|GMT|UT)?([+-])(\d{1,2})(?::?(\d{2}))?(?::?(\d{2}))?$", RegexOptions.CultureInvariant);
            if (offset.Success)
            {
                var hours = int.Parse(offset.Groups[3].Value); var minutes = offset.Groups[4].Success ? int.Parse(offset.Groups[4].Value) : 0;
                var seconds = offset.Groups[5].Success ? int.Parse(offset.Groups[5].Value) : 0;
                if (hours > 18 || minutes > 59 || seconds > 59 || hours == 18 && (minutes != 0 || seconds != 0)) throw new TimeZoneNotFoundException();
                var span = TimeSpan.FromSeconds((hours * 3600 + minutes * 60 + seconds) * (offset.Groups[2].Value == "-" ? -1 : 1));
                var suffix = span == TimeSpan.Zero ? "" : $"{offset.Groups[2].Value}{hours:00}:{minutes:00}" + (seconds == 0 ? "" : $":{seconds:00}");
                var id = offset.Groups[1].Value + suffix; if (id.Length == 0) id = "Z";
                return new(id, null, span);
            }
            if (!TimeZoneInfo.TryConvertIanaIdToWindowsId(text, out _)) throw new TimeZoneNotFoundException();
            return new(text, TimeZoneInfo.FindSystemTimeZoneById(text), TimeSpan.Zero);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException or NullReferenceException)
        { throw new BadRequestException("Invalid timezone"); }
    }

    public DateTime Next(DateTime fromUtc, BackupTimeZone zone)
    {
        var local = zone.Region is null ? fromUtc + zone.Offset : TimeZoneInfo.ConvertTimeFromUtc(fromUtc, zone.Region);
        // Evaluate in wall time and resolve offsets ourselves. Spring skips nonexistent
        // DST times and permits both occurrences of a repeated wall time.
        var cursor = local;
        if (zone.Region?.IsAmbiguousTime(local) == true)
        {
            var offsets = zone.Region.GetAmbiguousTimeOffsets(local);
            cursor -= offsets.Max() - offsets.Min();
        }
        DateTime? best = null;
        while (true)
        {
            var next = expressions.Select(expression => expression.GetNextOccurrence(DateTime.SpecifyKind(cursor, DateTimeKind.Utc))).Min();
            if (next is null) break;
            var wall = DateTime.SpecifyKind(next.Value, DateTimeKind.Unspecified);
            cursor = wall;
            if (zone.Region?.IsInvalidTime(wall) == true) continue;
            var offsets = zone.Region?.IsAmbiguousTime(wall) == true ? zone.Region.GetAmbiguousTimeOffsets(wall) : [zone.Region?.GetUtcOffset(wall) ?? zone.Offset];
            foreach (var offset in offsets)
            {
                var utc = DateTime.SpecifyKind(wall - offset, DateTimeKind.Utc);
                if (utc > fromUtc && (best is null || utc < best)) best = utc;
            }
            if (best is not null && wall >= local) break;
        }
        return best ?? throw new BadRequestException("Cron expression does not produce a next run time");
    }
}

public sealed record BackupTimeZone(string Id, TimeZoneInfo? Region, TimeSpan Offset);
