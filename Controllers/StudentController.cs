using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SchoolProject.Controllers
{
    public class StudentController : Controller
    {
        [Authorize(Roles = " Student")]
        public IActionResult Dashboard()
        {
            var currentUserName = User.Identity.Name;
            return View();
        }
    }
}
