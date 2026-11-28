PawCare update bundle
=====================
1. Unzip over your repo root (folders already match: PawCare/...). Overwrite when asked.
2. Delete the files listed in DELETE-THESE.txt.
3. Close Visual Studio. Delete PawCare/App_Data/*.mdf and *.ldf (old DB was made by EnsureCreated, which is now removed).
4. Reopen, Package Manager Console:
      Add-Migration SyncModel
      Update-Database
   (Adds Pets.Gender and makes Customers.PasswordHash NOT NULL again.)
5. Run seed.sql once on the new database (staff, packages, services, products).
6. Run. Register -> the verification link prints in the Visual Studio Output / console window
   (no SMTP configured). Open it, log in, then Book.

Changed files
-------------
Program.cs, Helper.cs (new), Models/ViewModels.cs (new), Models/Database.cs,
Controllers/AccountController.cs, HomeController.cs, CartController.cs (adds Checkout placeholder),
Views/Account/{Index,Login,Register,ResendVerification}.cshtml,
Views/Booking/{Details,Menu}.cshtml (Details back-link fixed Index->Menu; Menu header gets 7th column),
Views/Booking/Cancel.cshtml (new, was missing), wwwroot/js/booking.js (fixes broken quote), wwwroot/css/booking.css (was empty).
BookingController, create.cshtml, _Slots.cshtml, _Layout.cshtml are unchanged.
Not compiled or run here (no .NET SDK available).
