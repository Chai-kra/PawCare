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

    // Id of the logged-in customer, read from the login cookie (extension method in Models/AuthHelpers.cs)
    private string? CustomerId => User.CustomerId();

    // ------------------------------------------------------------ My appointments
    public async Task<IActionResult> Menu()
    {
        var model = await db.Appointments
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
            .ToListAsync();

        foreach (var row in model)
            row.CanCancel = BookingRules.CanCancel(row.Status, row.StartTime);

        return View(model);
    }

    // ------------------------------------------------------------ Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new BookingVM();
        await FillListsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(BookingVM vm)
    {
        ServicePackage? package = null;
        Pet? pet = null;
        var petName = (vm.PetName ?? "").Trim();

        // Extra server-side checks that attributes cannot express
        if (ModelState.IsValid)
        {
            package = await db.ServicePackages.FindAsync(vm.ServicePackageId);
            if (package == null)
                ModelState.AddModelError("ServicePackageId", "That service package is no longer available.");

            if (vm.PetType != "Dog" && vm.PetType != "Cat")
                ModelState.AddModelError("PetType", "Please choose dog or cat.");
            if (vm.PetGender != "Male" && vm.PetGender != "Female")
                ModelState.AddModelError("PetGender", "Please choose male or female.");

            // Same customer + same name = the same buddy. Its saved details are kept.
            pet = await db.Pets.FirstOrDefaultAsync(p => p.CustomerId == CustomerId && p.Name == petName);
            if (pet != null && pet.Type != vm.PetType)
                ModelState.AddModelError("PetType", $"You already have a buddy called {pet.Name} ({pet.Type}). Please use a different name.");
        }

        if (ModelState.IsValid)
        {
            var start = vm.Date!.Value.Date + TimeSpan.Parse(vm.SlotTime!);

            // Check again now: the time may have been taken since the page was loaded
            var freeStaff = new List<string>();
            if (BookingRules.IsBookableTime(start, package!.TimeAmount))
                freeStaff = await GetFreeStaffAsync(start, package.TimeAmount, vm.StaffId, pet?.Id);

            if (freeStaff.Count == 0)
            {
                ModelState.AddModelError("SlotTime", "Sorry, that time slot is no longer available. Please choose another.");
            }
            else
            {
               
                if (pet == null)
                {
                    pet = new Pet
                    {
                        Id = await NextPetIdAsync(),
                        Name = petName,
                        Type = vm.PetType!,
                        Gender = vm.PetGender!,
                        Breed = vm.PetBreed!.Trim(),
                        CustomerId = CustomerId
                    };
                    db.Pets.Add(pet);
                }

                var appt = new Appointment
                {
                    Id = await NextAppointmentIdAsync(),
                    CustomerId = CustomerId,
                    PetId = pet.Id,
                    StaffId = freeStaff[0],
                    ServicePackageId = package.Id,
                    StartTime = start,
                    EndTime = start.AddMinutes(package.TimeAmount),
                    Price = package.Price,
                    Notes = string.IsNullOrWhiteSpace(vm.Notes) ? null : vm.Notes.Trim(),
                    Status = AppointmentStatus.PendingPayment,
                    CreatedAt = DateTime.Now
                };

                db.Appointments.Add(appt);
                await db.SaveChangesAsync();

                TempData["Success"] = $"Appointment {appt.Id} created. Please complete payment within {BookingRules.HoldMinutes} minutes to confirm it.";
                return RedirectToAction(nameof(Details), new { id = appt.Id });
            }
        }

        await FillListsAsync(vm);
        return View(vm);
    }

    // ------------------------------------------------------------ AJAX: list the free time
    [HttpGet]
    public async Task<IActionResult> Slots(DateTime date, string servicePackageId, string? staffId, string? petName)
    {
        var minutes = await db.ServicePackages
            .Where(p => p.Id == servicePackageId)
            .Select(p => p.TimeAmount)
            .FirstOrDefaultAsync();

        var name = (petName ?? "").Trim();
        var petId = await db.Pets
            .Where(p => p.CustomerId == CustomerId && p.Name == name)
            .Select(p => p.Id)
            .FirstOrDefaultAsync();

        var times = new List<DateTime>();
        if (minutes > 0)
        {
            for (var t = BookingRules.OpenTime(date); t < BookingRules.CloseTime(date); t = t.AddMinutes(BookingRules.SlotStepMinutes))
            {
                if (BookingRules.IsBookableTime(t, minutes) && (await GetFreeStaffAsync(t, minutes, staffId, petId)).Count > 0)
                    times.Add(t);
            }
        }

        return PartialView("_Slots", times);
    }

    // ------------------------------------------------------------ Details
    public async Task<IActionResult> Details(string id)
    {
        var vm = await db.Appointments
            .Where(a => a.Id == id && a.CustomerId == CustomerId)  
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
            .FirstOrDefaultAsync();
        if (vm == null) return NotFound();

        vm.CanCancel = BookingRules.CanCancel(vm.Status, vm.StartTime);
        return View(vm);
    }

    // ------------------------------------------------------------ Cancel (with reason)
    [HttpGet]
    public async Task<IActionResult> Cancel(string id)
    {
        var a = await db.Appointments.Include(x => x.Pets)
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == CustomerId);
        if (a == null) return NotFound();

        if (!BookingRules.CanCancel(a.Status, a.StartTime))
        {
            TempData["Error"] = $"This appointment can no longer be cancelled online (it must be at least {BookingRules.MinCancelHours} hours away).";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(new CancelVM { Id = a.Id, PetName = a.Pets.Name, StartTime = a.StartTime });
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(CancelVM vm)
    {
        var a = await db.Appointments.Include(x => x.Pets)
            .FirstOrDefaultAsync(x => x.Id == vm.Id && x.CustomerId == CustomerId);
        if (a == null) return NotFound();

        if (!BookingRules.CanCancel(a.Status, a.StartTime))
            ModelState.AddModelError("", "This appointment can no longer be cancelled.");

        if (ModelState.IsValid)
        {
            a.Status = AppointmentStatus.Cancelled;
            a.CancelReason = vm.Reason.Trim();
            a.CancelledAt = DateTime.Now;
            await db.SaveChangesAsync();

            // TODO (Payment module): if already paid, trigger the Stripe refund here.
            TempData["Success"] = "Your appointment has been cancelled.";
            return RedirectToAction(nameof(Menu));
        }

        vm.PetName = a.Pets.Name;
        vm.StartTime = a.StartTime;
        return View(vm);
    }

    // ------------------------------------------------------------ DEV ONLY
    // Stand-in until the Payment module (Stripe) sets the status.
    // Delete this action, and the "Simulate payment" button in Details.cshtml, when Stripe is added.
    [HttpPost]
    public async Task<IActionResult> SimulatePayment(string id)
    {
        var a = await db.Appointments.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == CustomerId);
        if (a == null) return NotFound();

        if (a.Status == AppointmentStatus.PendingPayment)
        {
            a.Status = AppointmentStatus.Confirmed;
            await db.SaveChangesAsync();
            TempData["Success"] = "Payment simulated. Appointment confirmed.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // ------------------------------------------------------------ helpers
    private async Task<string> NextAppointmentIdAsync()
    {
        var last = await db.Appointments.OrderByDescending(a => a.Id).Select(a => a.Id).FirstOrDefaultAsync();
        var n = last == null ? 0 : int.Parse(last.Substring(1));
        return "A" + (n + 1).ToString("D6");
    }

   
    private async Task<string> NextPetIdAsync()
    {
        var ids = await db.Pets.Select(p => p.Id).ToListAsync();
        var max = ids.Select(i => int.TryParse(i.Substring(1), out var v) ? v : 0).DefaultIfEmpty(0).Max();
        return "P" + (max + 1).ToString("D6");
    }

    private async Task FillListsAsync(BookingVM vm)
    {
        // Only staff with Role "Staff" can handle a pet (admins cannot)
        vm.Staff = await db.Staff.Where(s => s.Role == "Staff").OrderBy(s => s.Name)
            .Select(s => new SelectListItem { Text = s.Name, Value = s.Id })
            .ToListAsync();

        vm.Packages = await db.ServicePackages.OrderBy(p => p.Id)
            .Select(p => new PackageOptionVM { Id = p.Id, ServiceType = p.ServiceType, Price = p.Price, TimeAmount = p.TimeAmount })
            .ToListAsync();
    }

    // Which staff are free from 'start' for 'minutes'? Returns their Ids (empty list = the time is not available)
    private async Task<List<string>> GetFreeStaffAsync(DateTime start, int minutes, string? staffId, string? petId)
    {
        var end = start.AddMinutes(minutes);
        var holdCutoff = DateTime.Now.AddMinutes(-BookingRules.HoldMinutes);

        // Appointments that overlap this time. Cancelled ones, and unpaid ones older than the hold time, do not count.
        var clashes = await db.Appointments
            .Where(a => a.StartTime < end && a.EndTime > start
                     && a.Status != AppointmentStatus.Cancelled
                     && !(a.Status == AppointmentStatus.PendingPayment && a.CreatedAt < holdCutoff))
            .ToListAsync();

        // The pet cannot be in two places at once
        if (!string.IsNullOrEmpty(petId) && clashes.Any(a => a.PetId == petId))
            return new List<string>();

        // Staff who can handle pets and are not busy (or just the one the customer picked)
        var busyStaff = clashes.Select(a => a.StaffId).ToList();
        var staff = db.Staff.Where(s => s.Role == "Staff" && !busyStaff.Contains(s.Id));
        if (!string.IsNullOrEmpty(staffId))
            staff = staff.Where(s => s.Id == staffId);

        return await staff.Select(s => s.Id).ToListAsync();
    }
}