using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CTBookerPro.Models
{
    public class ContactViewModel
    {
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public HttpPostedFileBase Attachment { get; set; }
        public List<string> SelectedEmails { get; set; }
        public IEnumerable<SelectListItem> AvailableEmails { get; set; }
        public string RecipientEmail { get; set; } // Add this line
    }
}