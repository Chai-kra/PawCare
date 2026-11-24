using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PawCare.Models;

namespace PawCare.Controllers;

[Authorize(Roles = "Customer")]
public class BookingController : Controller
{
    private readonly Database db;

    public BookingController(Database db)
    {
        this.db = db;
    }

   
    private string CustomerId => User.Identity!.Name!;

    // ------------------------------------------------------------ My appointments
    public IActionResult Index()
    {
        var model = db.Appointments
                      .Where(a => a.CustomerId == CustomerId)
                      .OrderByDescending(a => a.StartTime)
                      .Select(a => new AppointmentListVM
                      {
                          Id = a.Id,
                          StartTime = a.StartTime,
                          PetName = a.Pets.Name,
                          StaffName = a.Staffs.Name,
                          ServiceType = a.ServicePackages.ServiceType,
                          Price = a.Price,
                          Status = a.Status
                      })
                      .ToList();

        foreach (var item in model)
        {
            item.CanCancel = BookingRules.CanCancel(item.Status, item.StartTime);
        }

        return View(model);
    }

    // ------------------------------------------------------------ Create
    [HttpGet]
    public IActionResult Create()
    {
        var vm = new BookingVM();
        FillLists(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(BookingVM vm)
    {
        // Date range 
        if (vm.Date != null && vm.Date.Value.Date < DateTime.Today)
            ModelState.AddModelError("Date", "Appointment date cannot be in the past.");
        if (vm.Date != null && vm.Date.Value.Date > DateTime.Today.AddDays(BookingRules.MaxAdvanceDays))
            ModelState.AddModelError("Date", $"Appointments can only be booked up to {BookingRules.MaxAdvanceDays} days ahead.");

        // The chosen package must exist
        ServicePackage? package = null;
        if (!string.IsNullOrEmpty(vm.ServicePackageId))
            package = db.ServicePackages.Find(vm.ServicePackageId);
        if (ModelState.IsValid && package == null)
            ModelState.AddModelError("ServicePackageId", "That service package is no longer available.");

        // A customer may only book for their own pets
        if (ModelState.IsValid && !db.Pets.Any(p => p.Id == vm.PetId && p.CustomerId == CustomerId))
            ModelState.AddModelError("PetId", "Invalid pet selected.");

        if (ModelState.IsValid)
        {
            var start = vm.Date!.Value.Date + TimeSpan.Parse(vm.SlotTime!);
            var minutes = package!.TimeAmount;

            // Check again now: the time may have been taken since the page was loaded
            var freeStaff = new List<string>();
            if (BookingRules.IsBookableTime(start, minutes))
                freeStaff = GetFreeStaff(start, minutes, vm.StaffId, vm.PetId);

            if (freeStaff.Count == 0)
            {
                ModelState.AddModelError("SlotTime", "Sorry, that time slot is no longer available. Please choose another.");
            }
            else
            {
                var appt = new Appointment
                {
                    Id = NextAppointmentId(),
                    CustomerId = CustomerId,
                    PetId = vm.PetId!,
                    StaffId = freeStaff[0],
                    ServicePackageId = package.Id,
                    StartTime = start,
                    EndTime = start.AddMinutes(minutes),
                    Price = package.Price,
                    Notes = string.IsNullOrWhiteSpace(vm.Notes) ? null : vm.Notes.Trim(),
                    Status = AppointmentStatus.PendingPayment,
                    CreatedAt = DateTime.Now
                };

                db.Appointments.Add(appt);
                db.SaveChanges();

                TempData["Info"] = $"Appointment {appt.Id} created. Please complete payment within {BookingRules.HoldMinutes} minutes to confirm it.";
                return RedirectToAction("Details", new { id = appt.Id });
            }
        }

        FillLists(vm);
        return View(vm);
    }

    // ------------------------------------------------------------ AJAX: list the free times
    [HttpGet]
    public IActionResult Slots(DateTime date, string? servicePackageId, string? staffId, string? petId)
    {
        var times = new List<DateTime>();

        ServicePackage? package = null;
        if (!string.IsNullOrEmpty(servicePackageId))
            package = db.ServicePackages.Find(servicePackageId);

        if (package != null)
        {
            var close = BookingRules.CloseTime(date);

            // Try every 30-minute start time of the day
            for (var t = BookingRules.OpenTime(date); t < close; t = t.AddMinutes(BookingRules.SlotStepMinutes))
            {
                if (BookingRules.IsBookableTime(t, package.TimeAmount)
                    && GetFreeStaff(t, package.TimeAmount, staffId, petId).Count > 0)
                {
                    times.Add(t);
                }
            }
        }

        return PartialView("_Slots", times);
    }

    // ------------------------------------------------------------ Details
    public IActionResult Details(string id)
    {
        var vm = db.Appointments
                   .Where(a => a.Id == id && a.CustomerId == CustomerId)   // own records only
                   .Select(a => new AppointmentDetailVM
                   {
                       Id = a.Id,
                       StartTime = a.StartTime,
                       EndTime = a.EndTime,
                       PetName = a.Pets.Name,
                       StaffName = a.Staffs.Name,
                       ServiceType = a.ServicePackages.ServiceType,
                       TimeAmount = a.ServicePackages.TimeAmount,
                       Price = a.Price,
                       Status = a.Status,
                       Notes = a.Notes,
                       CancelReason = a.CancelReason
                   })
                   .FirstOrDefault();
        if (vm == null) return NotFound();

        vm.CanCancel = BookingRules.CanCancel(vm.Status, vm.StartTime);
        return View(vm);
    }

    // ------------------------------------------------------------ Cancel (with reason)
    [HttpGet]
    public IActionResult Cancel(string id)
    {
        var a = db.Appointments.Include(x => x.Pets)
                  .FirstOrDefault(x => x.Id == id && x.CustomerId == CustomerId);
        if (a == null) return NotFound();

        if (!BookingRules.CanCancel(a.Status, a.StartTime))
        {
            TempData["Error"] = $"This appointment can no longer be cancelled online (it must be at least {BookingRules.MinCancelHours} hours away).";
            return RedirectToAction("Details", new { id });
        }

        var vm = new CancelVM { Id = a.Id, PetName = a.Pets.Name, StartTime = a.StartTime };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Cancel(CancelVM vm)
    {
        var a = db.Appointments.Include(x => x.Pets)
                  .FirstOrDefault(x => x.Id == vm.Id && x.CustomerId == CustomerId);
        if (a == null) return NotFound();

        if (!BookingRules.CanCancel(a.Status, a.StartTime))
            ModelState.AddModelError("", "This appointment can no longer be cancelled.");

        if (ModelState.IsValid)
        {
            a.Status = AppointmentStatus.Cancelled;
            a.CancelReason = vm.Reason.Trim();
            a.CancelledAt = DateTime.Now;
            db.SaveChanges();

            // TODO (Payment module): if already paid, trigger the Stripe refund here.
            TempData["Info"] = "Your appointment has been cancelled.";
            return RedirectToAction("Index");
        }

        vm.PetName = a.Pets.Name;
        vm.StartTime = a.StartTime;
        return View(vm);
    }

    // ------------------------------------------------------------ DEV ONLY
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SimulatePayment(string id)
    {
        var a = db.Appointments.FirstOrDefault(x => x.Id == id && x.CustomerId == CustomerId);
        if (a == null) return NotFound();

        if (a.Status == AppointmentStatus.PendingPayment)
        {
            a.Status = AppointmentStatus.Confirmed;
            db.SaveChanges();
            TempData["Info"] = "Payment simulated. Appointment confirmed.";
        }
        return RedirectToAction("Details", new { id });
    }

    // ------------------------------------------------------------ helpers
    private string NextAppointmentId()
    {
        var last = db.Appointments.OrderByDescending(a => a.Id).Select(a => a.Id).FirstOrDefault();
        int number = last == null ? 1 : int.Parse(last.Substring(1)) + 1;
        return "A" + number.ToString("D6");
    }

    // Fill the dropdowns and radio buttons of the booking form
    private void FillLists(BookingVM vm)
    {
        vm.Pets = db.Pets
                    .Where(p => p.CustomerId == CustomerId)
                    .OrderBy(p => p.Name)
                    .Select(p => new SelectListItem { Text = p.Name + " (" + p.Breed + ")", Value = p.Id })
                    .ToList();

        // Only staff with Role "Staff" can handle a pet (admins cannot)
        vm.Staff = db.Staff
                     .Where(s => s.Role == "Staff")
                     .OrderBy(s => s.Name)
                     .Select(s => new SelectListItem { Text = s.Name, Value = s.Id })
                     .ToList();

        vm.Packages = db.ServicePackages
                        .OrderBy(p => p.Id)
                        .Select(p => new PackageOptionVM { Id = p.Id, ServiceType = p.ServiceType, Price = p.Price, TimeAmount = p.TimeAmount })
                        .ToList();
    }

    // Which staff are free from 'start' for 'minutes'? Returns their Ids (empty list = nobody, time not available)
    private List<string> GetFreeStaff(DateTime start, int minutes, string? staffId, string? petId)
    {
        var end = start.AddMinutes(minutes);
        var holdCutoff = DateTime.Now.AddMinutes(-BookingRules.HoldMinutes);

        // Appointments that overlap this time. Cancelled ones, and unpaid ones older than the hold time, do not count.
        var clashes = db.Appointments
                        .Where(a => a.StartTime < end && a.EndTime > start
                                 && a.Status != AppointmentStatus.Cancelled
                                 && !(a.Status == AppointmentStatus.PendingPayment && a.CreatedAt < holdCutoff))
                        .ToList();

        // The pet cannot be in two places at once
        if (!string.IsNullOrEmpty(petId) && clashes.Any(a => a.PetId == petId))
            return new List<string>();

        // Staff who can handle pets (or just the one the customer picked)
        var staff = db.Staff.Where(s => s.Role == "Staff");
        if (!string.IsNullOrEmpty(staffId))
            staff = staff.Where(s => s.Id == staffId);

        // Remove the staff who are already busy
        var busyIds = clashes.Select(a => a.StaffId).ToList();
        return staff.Where(s => !busyIds.Contains(s.Id)).Select(s => s.Id).ToList();
    }
}