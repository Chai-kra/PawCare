namespace PawCare.Models;

public static class BookingRules
{
    public const int SlotStepMinutes = 30; // a new slot starts every 30 minutes
    public const int LeadMinutes = 60; // must book at least 1 hour ahead
    public const int MaxAdvanceDays = 60; // cannot book more than 60 days ahead
    public const int HoldMinutes = 15; //unpaid booking cannot hold the slot for 15 minutes
    public const int MinCancelHours = 24; // customer can cancel only if appointment is more than 24 hours away

    public static bool CanCancel(AppointmentStatus status, DateTime startTime) =>
        (status == AppointmentStatus.PendingPayment || status == AppointmentStatus.Confirmed)
        && startTime > DateTime.Now.AddHours(MinCancelHours);


    public static (DateTime Open, DateTime Close)? OpeningHours(DateTime date)
    {
        var d = date.Date;
        return d.DayOfWeek switch
        {
            DayOfWeek.Sunday => null,
            DayOfWeek.Saturday => (d.AddHours(9), d.AddHours(16)),
            _ => (d.AddHours(9), d.AddHours(18))
        };
    }
}