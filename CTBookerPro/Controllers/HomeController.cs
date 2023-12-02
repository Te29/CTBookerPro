using CTBookerPro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CTBookerPro.Controllers
{
    [RequireHttps]
    public class HomeController : Controller
    {
        private Entities db = new Entities();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your heath helper.";

            return View();
        }

        [Authorize]
        public ActionResult Contact()
        {
            var model = new ContactViewModel
            {
                AvailableEmails = db.AspNetUsers.Select(u => new SelectListItem
                {
                    Value = u.Email,
                    Text = u.Email
                }).ToList()
            };
            return View(model); 
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> Contact(ContactViewModel model)
        {
            if (ModelState.IsValid)
            {
                var sendGridKey = System.Configuration.ConfigurationManager.AppSettings["SendGridKey"];
                var emailService = new EmailSender(sendGridKey);


                if (!string.IsNullOrEmpty(model.RecipientEmail))
                {

                    await emailService.SendEmailAsync(model.RecipientEmail, model.Subject, model.Message, model.Attachment);
                }
                else if (model.SelectedEmails != null && model.SelectedEmails.Any())
                {
                    await emailService.SendBulkEmailAsync(model.SelectedEmails, model.Subject, model.Message, model.Attachment);
                }

                ViewBag.SuccessMessage = "Email(s) sent successfully!";
                return RedirectToAction("Index");
            }

            // Populate available emails for the view
            PopulateAvailableEmails(model);
            return View(model);
        }

        private void PopulateAvailableEmails(ContactViewModel model)
        {
            model.AvailableEmails = db.AspNetUsers.Select(u => new SelectListItem
            {
                Value = u.Email,
                Text = u.Email
            }).ToList();
        }
    }

}
