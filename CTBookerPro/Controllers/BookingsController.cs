using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using CTBookerPro.Models;
using Microsoft.AspNet.Identity;
using System.Web.Hosting;
using Rotativa;
using System.Text;


namespace CTBookerPro.Controllers
{
    public class BookingsController : Controller
    {
        private Entities db = new Entities();

        // GET: Bookings
        // Only doctors and patient can create/check booking
        [Authorize(Roles = "doctor,patient")]
        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();

            // Can only see the booking whitch related to them
            if (User.IsInRole("doctor"))
            {
                var bookings = db.Bookings.Where(b => b.Doctor.Id == userId).ToList();
                return View(bookings);
            }

            if (User.IsInRole("patient"))
            {
                var bookings = db.Bookings.Where(b => b.Patient.Id == userId).ToList();
                return View(bookings);
            }

            return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
        }

        // GET: Bookings/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Booking booking = db.Bookings.Find(id);
            if (booking == null)
            {
                return HttpNotFound();
            }
            return View(booking);
        }

        // GET: Bookings/Create
        // I add a [Authorize] attribute to implement security to ensure only authorized user can access the page. 
        [Authorize(Roles = "doctor,patient")]
        public ActionResult Create()
        {
            ViewBag.DoctorList = new SelectList(db.AspNetUsers.OfType<Doctor>(), "Id", "DoctorName");
            return View();
        }

        // POST: Bookings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Here are default security implementations for online attack:
        // [ValidateAntiForgeryToken], ModelState.IsValid  
        public ActionResult Create([Bind(Include = "Id,Date,Description,Location")] Booking booking, string DoctorId, double DoctorRating)
        {
            if (ModelState.IsValid)
            {
                //Checking booking conflict
                var isConflict = db.Bookings.Any(b => b.Doctor.Id == DoctorId && DbFunctions.TruncateTime(b.Date) == DbFunctions.TruncateTime(booking.Date));
                if (isConflict)
                {
                    ModelState.AddModelError("", "The doctor is already booked on this date.");
                    ViewBag.DoctorList = new SelectList(db.AspNetUsers.OfType<Doctor>(), "Id", "DoctorName");
                    return View(booking);
                }

                string currentUserId = User.Identity.GetUserId();
                var currentPatient = db.AspNetUsers.OfType<Patient>().SingleOrDefault(p => p.Id == currentUserId);
                var selectedDoctor = db.AspNetUsers.OfType<Doctor>().SingleOrDefault(d => d.Id == DoctorId);

                if (currentPatient != null && selectedDoctor != null)
                {
                    booking.Patient = currentPatient;
                    booking.Doctor = selectedDoctor;

                    int existingBookingsCount = selectedDoctor.Bookings.Count;

                    // (existingBookingsCount + 1) is because doctor has a default rating
                    if (existingBookingsCount == 0)
                    {
                        selectedDoctor.Rating = DoctorRating;
                    }
                    else
                    {
                        selectedDoctor.Rating = (selectedDoctor.Rating * existingBookingsCount + DoctorRating) / (existingBookingsCount + 1);
                    }

                    db.Bookings.Add(booking);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    if (currentPatient == null)
                        ModelState.AddModelError("", "The user isn't a valid patient account.");

                    if (selectedDoctor == null)
                        ModelState.AddModelError("", "Invalid choice.");
                }
            }

            ViewBag.DoctorList = new SelectList(db.AspNetUsers.OfType<Doctor>(), "Id", "DoctorName");
            return View(booking);
        }

        [HttpGet]
        public JsonResult GetDoctorAppointments(string doctorId)
        {
            var appointments = db.Bookings.Where(b => b.Doctor.Id == doctorId)
                                          .Select(b => new { start = b.Date, title = "Booked", doctorId = b.Doctor.Id })
                                          .ToList();

            return Json(appointments, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult CheckAvailability(string doctorId, DateTime date)
        {
            bool isAvailable = !db.Bookings.Any(b => b.Doctor.Id == doctorId && b.Date == date);
            return Json(new { isAvailable }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetDoctorBookingShare(string doctorId)
        {

            var totalBookings = db.Bookings.Count();


            var doctorBookingsCount = db.Bookings.Where(b => b.Doctor.Id == doctorId).Count();


            var doctorShare = (double)doctorBookingsCount / totalBookings * 100;
            var otherShare = 100 - doctorShare;

            var doctorRating = db.Bookings.Where(b => b.Doctor.Id == doctorId).Average(b => b.Doctor.Rating);

            var result = new
            {
                DoctorShare = doctorShare,
                OtherShare = otherShare,
                Rating = doctorRating
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Authorize(Roles = "patient")]
        public ActionResult ExportPDF(string exportFormat)
        {
            var userId = User.Identity.GetUserId();
            var bookings = db.Bookings.Where(b => b.Patient.Id == userId).ToList();

            if (bookings.Count == 0)
            {
                TempData["Message"] = "No bookings to export.";
                return RedirectToAction("Index");
            }

            if (exportFormat == "pdf")
            {
                var pdf = new ViewAsPdf("ExportPDF", bookings)
                {
                    FileName = "BookingReport.pdf"
                };
                return pdf;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "patient")]
        public ActionResult ExportCSV()
        {
            var userId = User.Identity.GetUserId();
            var bookings = db.Bookings.Where(b => b.Patient.Id == userId).ToList();

            if (bookings.Count == 0)
            {
                TempData["Message"] = "No bookings to export.";
                return RedirectToAction("Index");
            }

            StringBuilder csv = new StringBuilder();

            // Add headers
            csv.AppendLine("Date,Description,Location,Doctor,Rating");

            // Add data from bookings
            foreach (var booking in bookings)
            {
                csv.AppendLine($"{booking.Date.ToShortDateString()},{booking.Description},{booking.Location},{booking.Doctor.DoctorName},{booking.Doctor.Rating.ToString("F2")}");
            }

            byte[] buffer = Encoding.ASCII.GetBytes(csv.ToString());

            return File(buffer, "text/csv", "Bookings.csv");
        }


        // GET: Bookings/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Booking booking = db.Bookings.Find(id);
            if (booking == null)
            {
                return HttpNotFound();
            }
            return View(booking);
        }

        // POST: Bookings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Date,Description,Location")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                db.Entry(booking).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(booking);
        }

        // GET: Bookings/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Booking booking = db.Bookings.Find(id);
            if (booking == null)
            {
                return HttpNotFound();
            }
            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Booking booking = db.Bookings.Find(id);
            db.Bookings.Remove(booking);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
