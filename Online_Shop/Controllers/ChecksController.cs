using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Shop.Data;
using Online_Shop.Models;
using System.Data;

namespace Online_Shop.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ChecksController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public ChecksController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;
        }
        public IActionResult Index()
        {
            var prod = db.Products.Include("Category")
                                  .Include("User")
                                  .Where(p => p.Check == null);

            if(prod.Any())
                ViewBag.UncheckedProducts = prod;
            

            return View();
        }

        public ActionResult Edit(int id)
        {
            Product product = db.Products.Find(id);
            
            return View(product);
        }

        [HttpPost]
        public ActionResult Edit(int id, Product requestProduct)
        {
            Product product = db.Products.Find(id);
            
            try
            {
                product.Check = requestProduct.Check;
                db.SaveChanges();
                TempData["message"] = "Produsul a fost adaugat!";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                //ViewBag.Category = requestCategory;
                return View(requestProduct);
            }
        }

        /*public async Task<ActionResult> Edit(string id)
        {
            ApplicationUser user = db.Users.Find(id);

            user.AllRoles = GetAllRoles();

            var roleNames = await _userManager.GetRolesAsync(user); // Lista de nume de roluri

            // Cautam ID-ul rolului in baza de date
            var currentUserRole = _roleManager.Roles
                                              .Where(r => roleNames.Contains(r.Name))
                                              .Select(r => r.Id)
                                              .First(); // Selectam 1 singur rol
            ViewBag.UserRole = currentUserRole;

            return View(user);
        }*/


    }
}
