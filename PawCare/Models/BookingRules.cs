namespace PawCare.Models;

public static class BookingRules
{
    public const int SlotStepMinutes = 30; // a new slot starts every 30 minutes
    public const int LeadMinutes = 60; // must book at least 1 hour ahead
    public const int MaxAdvanceDays = 60; // cannot book more than 60 days ahead
    public const int HoldMinutes = 15; // an unpaid booking holds the slot for 15 minutes only
    public const int MinCancelHours = 24; // customer can cancel only if appointment is more than 24 hours away

   
    public static bool IsOpen(DateTime date)
    {
        return date.DayOfWeek != DayOfWeek.Sunday;
    }

    public static DateTime OpenTime(DateTime date)
    {
        return date.Date.AddHours(9);
    }

    public static DateTime CloseTime(DateTime date)
    {
        if (date.DayOfWeek == DayOfWeek.Saturday)
            return date.Date.AddHours(16);
        return date.Date.AddHours(18);
    }

   
    public static bool IsBookableTime(DateTime start, int minutes)
    {
        return IsOpen(start)
            && start >= OpenTime(start)
            && start.AddMinutes(minutes) <= CloseTime(start)
            && start.Minute % SlotStepMinutes == 0
            && start >= DateTime.Now.AddMinutes(LeadMinutes);
    }

    public static bool CanCancel(AppointmentStatus status, DateTime startTime)
    {
        return (status == AppointmentStatus.PendingPayment || status == AppointmentStatus.Confirmed)
            && startTime > DateTime.Now.AddHours(MinCancelHours);
    }
}