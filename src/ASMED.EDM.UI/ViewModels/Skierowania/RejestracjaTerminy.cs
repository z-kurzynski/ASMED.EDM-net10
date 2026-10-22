using ASMED.EDM.Data.Services;
using MySqlConnector;
using Syncfusion.UI.Xaml.Scheduler;

namespace ASMED.EDM.UI.ViewModels.Skierowania;

/// <summary>
/// Wspólna logika wyszukiwania wolnego terminu w kalendarzu rejestracji
/// (zakres godzin i długość slotu zgodne z DaysViewSettings kalendarzy).
/// </summary>
public static class RejestracjaTerminy
{
    public static readonly TimeSpan DayStart = TimeSpan.FromHours(10);
    public static readonly TimeSpan DayEnd   = TimeSpan.FromHours(15);
    public const int SlotMinutes = 5;

    public static string ZakresGodzinText => $"{DayStart:hh\\:mm}-{DayEnd:hh\\:mm}";

    /// <summary>Zwraca R_ID rejestracji powiązanej ze skierowaniem lub null.</summary>
    public static int? GetRejestracjaId(DbConnectionFactory dbFactory, int skierowanieId)
    {
        using var conn = dbFactory.CreateConnection();
        conn.Open();
        using var cmd = (MySqlCommand)conn.CreateCommand();
        cmd.CommandText = "SELECT R_ID FROM Rejestracja WHERE R_S_ID = @sid LIMIT 1";
        cmd.Parameters.AddWithValue("@sid", skierowanieId);
        var result = cmd.ExecuteScalar();
        return result == null || result == DBNull.Value ? null : Convert.ToInt32(result);
    }

    /// <summary>
    /// Pierwszy wolny slot w danym dniu (od początku do końca godzin kalendarza).
    /// Rejestracja o R_ID = <paramref name="ownRId"/> nie blokuje terminu.
    /// </summary>
    public static DateTime? FindFirstFreeSlot(IEnumerable<ScheduleAppointment> events, DateTime day, int? ownRId)
    {
        var date = day.Date;
        var dayEnd = date + DayEnd;

        var ownNotesPrefix = ownRId.HasValue ? $"R_ID:{ownRId.Value} " : null;
        var dayEvents = events
            .Where(e => e.StartTime.Date == date)
            .Where(e => ownNotesPrefix == null
                        || !(e.Notes?.StartsWith(ownNotesPrefix, StringComparison.Ordinal) ?? false))
            .ToList();

        for (var candidate = date + DayStart;
             candidate.AddMinutes(SlotMinutes) <= dayEnd;
             candidate = candidate.AddMinutes(SlotMinutes))
        {
            var slotEnd = candidate.AddMinutes(SlotMinutes);
            if (!dayEvents.Any(e => e.StartTime < slotEnd && e.EndTime > candidate))
                return candidate;
        }

        return null;
    }
}
