namespace YaeBlog.Extensions;

public static class DateOnlyExtensions
{
    extension(DateOnly date)
    {
        public static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

        public DateOnly LastMonday
        {
            get
            {
                return date.DayOfWeek switch
                {
                    DayOfWeek.Monday => date,
                    DayOfWeek.Sunday => date.AddDays(-6),
                    _ => date.AddDays(1 - (int)date.DayOfWeek)
                };
            }
        }

        public int DayNumberOfWeek
        {
            get
            {
                return date.DayOfWeek switch
                {
                    DayOfWeek.Sunday => 7,
                    _ => (int)date.DayOfWeek + 1
                };
            }
        }
    }
}
