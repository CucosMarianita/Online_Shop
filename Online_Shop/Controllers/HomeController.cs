using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Shop.Data;
using Online_Shop.Models;
using System.Diagnostics;
using System.Globalization;

namespace Online_Shop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<HomeController> logger

            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;

            _logger = logger;

        }

        public IActionResult Index(string sortBy)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Products");
            }

            var products = db.Products.Include("Category").Include("User").Include("Comments").Where(p => p.Check == true);

            // SORTARE

            ViewBag.SortPriceParam = sortBy == "Price" ? "Price_desc" : "Price";
            ViewBag.SortRatingParam = sortBy == "Rating" ? "Rating_desc" : "Rating";

            switch (sortBy)
            {
                case "Price_desc":
                    products = products.OrderByDescending(x => x.Price);
                    break;
                case "Price":
                    products = products.OrderBy(x => x.Price);
                    break;
                case "Rating_desc":
                    products = products.OrderByDescending(x => x.Stars);
                    break;
                case "Rating":
                    products = products.OrderBy(x => x.Stars);
                    break;
                default:
                    products = products.OrderBy(x => x.Id);
                    break;

            }


            // MOTOR DE CAUTARE

            var search = "";

            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim(); // eliminam spatiile libere 



                List<int> productsIds = db.Products.Where
                                        (
                                         p => p.Title.Contains(search)
                                         || p.Content.Contains(search)
                                        ).Select(p => p.Id).ToList();

                // Cautare in comentarii (Content)
                List<int> productIdsOfCommentsWithSearchString = db.Comments
                                        .Where
                                        (
                                         c => c.Content.Contains(search)
                                        ).Select(c => (int)c.ProductId).ToList();

                // Se formeaza o singura lista formata din toate id-urile selectate anterior
                List<int> mergedIds = productsIds.Union(productIdsOfCommentsWithSearchString).ToList();


                // Lista produselor care contin cuvantul cautat
                // fie in articol -> Title si Content
                // fie in comentarii -> Content
                products = db.Products.Where(product => mergedIds.Contains(product.Id))
                                      .Include("Category")
                                      .Include("User");


            }

            ViewBag.SearchString = search;

            // AFISARE PAGINATA

            // Alegem sa afisam 3 produse pe pagina
            int _perPage = 6;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.message = TempData["message"].ToString();
            }


            // Fiind un numar variabil de articole, verificam de fiecare data utilizand 
            // metoda Count()

            int totalItems = products.Where(p => p.Check == true)
                                    .Count();


            // Se preia pagina curenta din View-ul asociat
            // Numarul paginii este valoarea parametrului page din ruta
            // /Articles/Index?page=valoare

            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);

            // Pentru prima pagina offsetul o sa fie zero
            // Pentru pagina 2 o sa fie 3 
            // Asadar offsetul este egal cu numarul de articole care au fost deja afisate pe paginile anterioare
            var offset = 0;

            // Se calculeaza offsetul in functie de numarul paginii la care suntem
            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * _perPage;
            }

            // Se preiau articolele corespunzatoare pentru fiecare pagina la care ne aflam 
            // in functie de offset
            var paginatedProducts = products.Skip(offset).Take(_perPage);


            // Preluam numarul ultimei pagini

            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);




            // Trimitem articolele cu ajutorul unui ViewBag catre View-ul corespunzator
            ViewBag.Products = paginatedProducts;

            if (search != "")
            {
                ViewBag.PaginationHome = "/Home/Index/?search=" + search + "&page";
            }
            else
            {
                ViewBag.PaginationHome = "/Home/Index/?page";
            }


            return View(products.ToList());
        }

        public IActionResult Show(int id)
        {
            Product product = db.Products.Include("Category")
                                         .Include("User")
                                         .Include("Comments")
                                         .Include("Comments.User")
                                         .Where(prod => prod.Id == id)
                                         .First();

            // Adaugam bookmark-urile utilizatorului pentru dropdown
            ViewBag.UserCarts = db.Carts
                                      .Where(b => b.UserId == _userManager.GetUserId(User))
                                      .ToList();
            SetAccessRights();

            return View(product);

        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            if (User.IsInRole("Colab"))
            {
                ViewBag.AfisareButoane = true;
            }

            ViewBag.EsteAdmin = User.IsInRole("Admin");

            ViewBag.UserCurent = _userManager.GetUserId(User);
        }
    }
}