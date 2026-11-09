namespace RaceDay.Mvc.Helpers
{
    public static class Format
    {
        public static string Ordinal(int n)
        {
            var mod100 = n % 100;
            if (mod100 is >= 11 and <= 13) return $"{n}th";
            return (n % 10) switch { 1 => $"{n}st", 2 => $"{n}nd", 3 => $"{n}rd", _ => $"{n}th" };
        }

        public static string Position(int? position, int total) =>
            position == null ? "No position recorded"
            : total > 0 ? $"{Ordinal(position.Value)} out of {total} finishers"
            : $"{Ordinal(position.Value)}";

        public static string Time(TimeSpan? t) =>
            t == null ? "-" : $"{(int)t.Value.TotalHours:00}:{t.Value.Minutes:00}:{t.Value.Seconds:00}";

        public static string Date(DateTime d) => d.ToString("ddd, d MMM yyyy");
        public static string DateAt(DateTime d) => d.ToString("ddd, d MMM yyyy 'at' HH:mm");
        public static string Km(decimal? km) => km == null ? "Distance TBC" : $"{km.Value:0.##} km";

        public static string TypeLabel(string type) => type switch
        {
            "run" => "Run", "walk" => "Walk", "cycle" => "Cycle", _ => type
        };

        public static string Initials(string? name)
        {
            var parts = (name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? "?" : string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
        }
    }
}
