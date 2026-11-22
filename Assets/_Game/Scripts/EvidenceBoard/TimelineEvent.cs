using System;

// Ръчно въведено събитие във времевата линия на разследването (EB-006).
// Датата и часът са въведена от играча информация без системно време и без timezone преобразуване.
public sealed class TimelineEvent
{
    public string Id { get; }
    public string Date { get; private set; } = string.Empty;
    public string Time { get; private set; } = string.Empty;
    public bool HasTime => !string.IsNullOrEmpty(Time);
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int InsertionOrder { get; }

    internal int DateSortKey { get; private set; }
    internal int TimeMinutes { get; private set; }

    internal TimelineEvent(string id, string normalizedDate, int dateSortKey,
        string normalizedTime, int timeMinutes, string title, string description, int insertionOrder)
    {
        Id = id;
        Date = normalizedDate;
        DateSortKey = dateSortKey;
        Time = normalizedTime;
        TimeMinutes = timeMinutes;
        Title = title;
        Description = description ?? string.Empty;
        InsertionOrder = insertionOrder;
    }

    public static bool TryValidateFields(
        string dateInput,
        string optionalTimeInput,
        string titleInput,
        out string normalizedDate,
        out int dateSortKey,
        out string normalizedTime,
        out int timeMinutes,
        out string normalizedTitle,
        out string errorMessage)
    {
        normalizedDate = string.Empty;
        dateSortKey = 0;
        normalizedTime = string.Empty;
        timeMinutes = -1;
        normalizedTitle = string.Empty;

        if (!TryParseDate(dateInput, out normalizedDate, out dateSortKey))
        {
            errorMessage = "Невалидна дата. Използвайте формат YYYY-MM-DD (напр. 2003-10-14).";
            return false;
        }

        if (!TryParseOptionalTime(optionalTimeInput, out normalizedTime, out timeMinutes))
        {
            errorMessage = "Невалиден час. Използвайте формат HH:mm (00:00–23:59) или оставете празно.";
            return false;
        }

        string trimmedTitle = titleInput == null ? string.Empty : titleInput.Trim();
        if (string.IsNullOrEmpty(trimmedTitle))
        {
            errorMessage = "Въведете заглавие на събитието.";
            return false;
        }

        normalizedTitle = trimmedTitle;
        errorMessage = string.Empty;
        return true;
    }

    public static bool TryParseDate(string input, out string normalizedDate, out int sortKey)
    {
        normalizedDate = string.Empty;
        sortKey = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        string trimmed = input.Trim();
        if (trimmed.Length != 10 || trimmed[4] != '-' || trimmed[7] != '-')
            return false;

        if (!TryParseAsciiDigits(trimmed, 0, 4, out int year)
            || !TryParseAsciiDigits(trimmed, 5, 2, out int month)
            || !TryParseAsciiDigits(trimmed, 8, 2, out int day))
            return false;

        if (year < 1 || year > 9999 || month < 1 || month > 12)
            return false;

        int maxDay = DateTime.DaysInMonth(year, month);
        if (day < 1 || day > maxDay)
            return false;

        normalizedDate = trimmed;
        sortKey = year * 10000 + month * 100 + day;
        return true;
    }

    public static bool TryParseOptionalTime(string input, out string normalizedTime, out int minutesOfDay)
    {
        normalizedTime = string.Empty;
        minutesOfDay = -1;
        if (string.IsNullOrWhiteSpace(input))
            return true;

        string trimmed = input.Trim();
        if (trimmed.Length != 5 || trimmed[2] != ':')
            return false;

        if (!TryParseAsciiDigits(trimmed, 0, 2, out int hour)
            || !TryParseAsciiDigits(trimmed, 3, 2, out int minute))
            return false;

        if (hour < 0 || hour > 23 || minute < 0 || minute > 59)
            return false;

        normalizedTime = trimmed;
        minutesOfDay = hour * 60 + minute;
        return true;
    }

    public static int CompareChronological(TimelineEvent a, TimelineEvent b)
    {
        if (ReferenceEquals(a, b)) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        int dateCmp = a.DateSortKey.CompareTo(b.DateSortKey);
        if (dateCmp != 0) return dateCmp;

        // В рамките на същата дата събитията с уточнен час са преди групата "Без уточнен час".
        if (a.HasTime != b.HasTime)
            return a.HasTime ? -1 : 1;

        if (a.HasTime)
        {
            int timeCmp = a.TimeMinutes.CompareTo(b.TimeMinutes);
            if (timeCmp != 0) return timeCmp;
        }

        int orderCmp = a.InsertionOrder.CompareTo(b.InsertionOrder);
        if (orderCmp != 0) return orderCmp;

        return string.CompareOrdinal(a.Id, b.Id);
    }

    internal void ApplyValidatedUpdate(string normalizedDate, int dateSortKey,
        string normalizedTime, int timeMinutes, string normalizedTitle, string description)
    {
        Date = normalizedDate;
        DateSortKey = dateSortKey;
        Time = normalizedTime;
        TimeMinutes = timeMinutes;
        Title = normalizedTitle;
        Description = description ?? string.Empty;
    }

    internal static TimelineEvent CreateNew(string normalizedDate, int dateSortKey,
        string normalizedTime, int timeMinutes, string normalizedTitle, string description, int insertionOrder)
    {
        return new TimelineEvent(
            Guid.NewGuid().ToString("N"),
            normalizedDate,
            dateSortKey,
            normalizedTime,
            timeMinutes,
            normalizedTitle,
            description ?? string.Empty,
            insertionOrder);
    }

    public static TimelineEvent Restore(string id, string date, string optionalTime,
        string title, string description, int insertionOrder)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Timeline event ID cannot be empty.", nameof(id));
        if (insertionOrder < 0)
            throw new ArgumentOutOfRangeException(nameof(insertionOrder), "Insertion order must be non-negative.");
        if (!TryValidateFields(date, optionalTime, title,
                out string normalizedDate, out int dateSortKey,
                out string normalizedTime, out int timeMinutes,
                out string normalizedTitle, out string error))
            throw new ArgumentException(error);

        return new TimelineEvent(
            id.Trim(),
            normalizedDate,
            dateSortKey,
            normalizedTime,
            timeMinutes,
            normalizedTitle,
            description ?? string.Empty,
            insertionOrder);
    }

    private static bool TryParseAsciiDigits(string text, int start, int length, out int value)
    {
        value = 0;
        for (int i = 0; i < length; i++)
        {
            char c = text[start + i];
            if (c < '0' || c > '9')
                return false;
            value = value * 10 + (c - '0');
        }
        return true;
    }
}
