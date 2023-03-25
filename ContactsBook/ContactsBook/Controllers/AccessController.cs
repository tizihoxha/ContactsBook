using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace ContactsBook.Controllers
{
    //[Authorize]
    public class AccessController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        public AccessController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }
        //Listing the roles

        public IActionResult First()
        {
            var roles = _roleManager.Roles;
            return View(roles);
        }
        [HttpGet]
        //[Authorize(Roles = "Admin")]
        public IActionResult Create() 
        {
            return View();
        }
        [HttpPost]

        public IActionResult Create(IdentityRole role) 
        {
            if (!_roleManager.RoleExistsAsync(role.Name).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(role.Name)).GetAwaiter().GetResult();
            }
            return RedirectToAction("First");
        }
    }
}
