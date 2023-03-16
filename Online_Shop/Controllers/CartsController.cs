using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Shop.Data;
using Online_Shop.Data.Migrations;
using Online_Shop.Models;
using System;
using System.Data;

namespace Online_Shop.Controllers
{
    [Authorize]
    public class CartsController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public CartsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;
        }

        [Authorize(Roles = "User")]
        // fiecare utilizator vede cosul sau de cumparaturi
        // HttpGet - implicit
        public IActionResult Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
            }

            SetAccessRights();

            if (User.IsInRole("User"))
            {
                //creez cosul de cumparaturi daca nu exista

                try
                {
                    Models.Cart oldCart = db.Carts.Where(c => c.UserId == _userManager.GetUserId(User)).First();
                  
                    /* var query = from cart in db.Carts.Include("User")
                                    .Where(b => b.UserId == _userManager.GetUserId(User))
                                 select cart.UserId;*/
                }

                catch (Exception ex)
                {
                    Models.Cart cart = new Models.Cart();
                    cart.UserId = _userManager.GetUserId(User);
                    db.Carts.Add(cart);
                }
                db.SaveChanges();

                var carts = from cart in db.Carts.Include("User")
                               .Where(b => b.UserId == _userManager.GetUserId(User))
                                select cart;

                ViewBag.Carts = carts;

                return View();
            }
            /*elsey
            if (User.IsInRole("Admin"))
            {
                var bookmarks = from bookmark in db.Bookmarks.Include("User")
                                select bookmark;

                ViewBag.Bookmarks = bookmarks;

                return View();
            }*/

            else
            {
                TempData["message"] = "Nu aveti drepturi";
                return RedirectToAction("Index", "Products");
            }

        }

        // Afisarea tuturor produselor pe care utilizatorul le-a salvat in 
        // cos

        [Authorize(Roles = "User")]
        public IActionResult Show(int id)
        {
            SetAccessRights();

            if (User.IsInRole("User"))
            {
                var carts = db.Carts
                                  .Include("ProductCarts.Product.Category")
                                  .Include("ProductCarts.Product.User")
                                  .Include("User")
                                  .Where(b => b.Id == id)
                                  .Where(b => b.UserId == _userManager.GetUserId(User))
                                  .FirstOrDefault();

                if (carts == null)
                {
                    TempData["message"] = "Nu aveti drepturi";
                    return RedirectToAction("Index", "Products");
                }

                return View(carts);
            }

          /*  else
            if (User.IsInRole("Admin"))
            {
                var bookmarks = db.Bookmarks
                                  .Include("ArticleBookmarks.Article.Category")
                                  .Include("ArticleBookmarks.Article.User")
                                  .Include("User")
                                  .Where(b => b.Id == id)
                                  .FirstOrDefault();


                if (bookmarks == null)
                {
                    TempData["message"] = "Resursa cautata nu poate fi gasita";
                    return RedirectToAction("Index", "Articles");
                }


                return View(bookmarks);
            }*/

            else
            {
                TempData["message"] = "Nu aveti drepturi";
                return RedirectToAction("Index", "Products");
            }
        }


        [Authorize(Roles = "User")]
        public IActionResult New()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "User")]
        public ActionResult New(Models.Cart bm)
        {
            bm.UserId = _userManager.GetUserId(User);

            // am modificat cu try in loc de verificarea exceptiei
            try
            {
                db.Carts.Add(bm);
                db.SaveChanges();
                TempData["message"] = "Colectia a fost adaugata";
                return RedirectToAction("Index");
            }

            catch (Exception ex)
            {
                return View(bm);
            }
        }


        // Conditiile de afisare a butoanelor de editare si stergere
        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            if (User.IsInRole("Colab") || User.IsInRole("User"))
            {
                ViewBag.AfisareButoane = true;
            }

            ViewBag.EsteAdmin = User.IsInRole("Admin");

            ViewBag.UserCurent = _userManager.GetUserId(User);
        }
    }
}
