using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Online_Shop.Data;
using Online_Shop.Models;
using System;
using System.Globalization;


namespace Online_Shop.Controllers
{
    [Authorize]

    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;
        
        /*  ADAUGAREA IMAGINII */

        // Variabila locala de tip ApplicationDbContext
        private ApplicationDbContext _context;
        private IWebHostEnvironment _env;


        public ProductsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment env
            )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;

            _context = context;
            _env = env;
        }

        // Se afiseaza lista tuturor produselor impreuna cu categoria 
        // din care fac parte
        // Pentru fiecare produs se afiseaza si userul care a postat produsul respectiv
        // HttpGet implicit
        [Authorize(Roles = "User,Colab,Admin")]
        public IActionResult Index(string sortBy)
        {
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
                ViewBag.PaginationBaseUrl = "/Products/Index/?search=" + search + "&page";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Products/Index/?page";
            }


            return View(products.ToList());
        }

        // Se afiseaza un singur articol in functie de id-ul sau 
        // impreuna cu categoria din care face parte
        // In plus sunt preluate si toate comentariile asociate unui articol
        // Se afiseaza si userul care a postat articolul respectiv
        // HttpGet implicit
        [Authorize(Roles = "User,Colab,Admin")]
        public IActionResult Show(int id)
        {
            Product product = db.Products.Include("Category")
                                         .Include("User")
                                         .Include("Comments")
                                         .Include("Comments.User")
                                         .Where(prod => prod.Id == id)
                                         .First();

            // Adaugam bookmark-urile utilizatorului 
            ViewBag.UserCarts = db.Carts
                                      .Where(b => b.UserId == _userManager.GetUserId(User))
                                      .ToList();
            SetAccessRights();

            return View(product);
  
        }

        /* private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            if (User.IsInRole("Colab"))
            {
                ViewBag.AfisareButoane = true;
            }

            ViewBag.UserCurent = _userManager.GetUserId(User);
            ViewBag.EsteAdmin = User.IsInRole("Admin");
            return View(product);
        } */


        // Adaugarea unui comentariu asociat unui produs in baza de date
        [HttpPost]
        [Authorize(Roles = "User,Colab,Admin")]
        public IActionResult Show([FromForm] Comment comment)
        {
            comment.Date = DateTime.Now;
            comment.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Comments.Add(comment);
                db.SaveChanges();

                if (comment.Rating != null)         // Daca stergem un comentariu cu rating updatam in baza de date produsul
                {
                    var product = db.Products.Where(pd => pd.Id == comment.ProductId).First();     //Obtinem produsul 
                    product.Stars = UpdateRating(comment, true);         //Updatam rating-ul
                }

                db.SaveChanges();

                return Redirect("/Products/Show/" + comment.ProductId);
            }

            else
            {
                Product prod = db.Products.Include("Category")
                                         .Include("User")
                                         .Include("Comments")
                                         .Include("Comments.User")
                                         .Where(p => p.Id == comment.ProductId)
                                         .First();

                

                // Adaugam bookmark-urile utilizatorului pentru dropdown
                ViewBag.UserCarts = db.Carts
                                          .Where(b => b.UserId == _userManager.GetUserId(User))
                                          .ToList();

                SetAccessRights();

                return View(prod);
            }
        }



        [HttpPost]
        public IActionResult Add_in_Cart([FromForm] ProductCart productCart)
        {
            // Daca modelul este valid
            if (ModelState.IsValid)
            {
                // Verificam daca avem deja articolul in cosul de cumparaturi
                if (db.ProductCarts
                                .Where(pb => pb.ProductId == productCart.ProductId)
                                .Where(pb => pb.CartId == productCart.CartId)
                                .Count() > 0)
                {
                    TempData["message"] = "Acest produs este deja adaugat in cos";
                    TempData["messageType"] = "alert-danger";
                }
                else
                {
                    // Adaugam asocierea intre articol si bookmark 
                    db.ProductCarts.Add(productCart);
                    // Salvam modificarile
                    db.SaveChanges();

                    // Adaugam un mesaj de success
                    TempData["message"] = "Produsul a fost adaugat in cos";
                    TempData["messageType"] = "alert-success";
                }

            }
            else
            {
                TempData["message"] = "Nu s-a putut adauga produsul in cos";
                TempData["messageType"] = "alert-danger";
            }

            // Ne intoarcem la pagina articolului
            return Redirect("/Products/Show/" + productCart.ProductId);
        }



        // Se afiseaza formularul in care se vor completa datele unui articol
        // impreuna cu selectarea categoriei din care face parte
        // Doar utilizatorii cu rolul de Colab sau Admin pot adauga articole in platforma
        // HttpGet implicit
        [Authorize(Roles = "Colab,Admin")]
        public IActionResult New()
        {
            //var categories = from categ in db.Categories
            //                 select categ;

            //ViewBag.Categories = categories;

            Product product = new Product();

            product.Categ = GetAllCategories();

            return View(product);
        }


        // Se adauga produsul in baza de date
        // Doar utilizatorii cu rolul de Colab sau Admin pot adauga articole in platforma
       /* [HttpPost]
        [Authorize(Roles = "Colab,Admin")]
        public IActionResult New(Product product)
        {
            
            product.UserId = _userManager.GetUserId(User);

            //product.Categ = GetAllCategories();
            //product.UserId = _userManager.GetUserId(User);


            if (ModelState.IsValid)
            {

                db.Products.Add(product);
                db.SaveChanges();
                TempData["message"] = "Produsul a fost adaugat";
                return RedirectToAction("Index");
            }
            else
            {
                product.Categ = GetAllCategories();
                return View(product);
            }
        }*/


        // Se editeaza un produs existent in baza de date impreuna cu categoria din care face parte
        // Categoria se selecteaza dintr-un dropdown
        // HttpGet implicit
        // Se afiseaza formularul impreuna cu datele aferente articolului din baza de date

        [Authorize(Roles = "Colab,Admin")]
        public IActionResult Edit(int id)
        {
            Product product = db.Products.Include("Category")
                                         .Where(prod => prod.Id == id)
                                         .First();

            product.Categ = GetAllCategories();

            if (product.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                return View(product);
            }
            else
            {
                TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui produs care nu va apartine";
                return RedirectToAction("Index");
            }

        }


        // Se adauga articolul modificat in baza de date
        [HttpPost]
        [Authorize(Roles = "Colab,Admin")]
        public IActionResult Edit(int id, Product requestProduct)
        {

            var sanitizer = new HtmlSanitizer();


            Product product = db.Products.Find(id);


            if (ModelState.IsValid)
            {
                if (product.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
                {
                    product.Title = requestProduct.Title;

                    requestProduct.Content = sanitizer.Sanitize(requestProduct.Content);

                    product.Content = requestProduct.Content;

                    product.CategoryId = requestProduct.CategoryId;
                    TempData["message"] = "Produsul a fost modificat";
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui produs care nu va apartine";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                requestProduct.Categ = GetAllCategories();
                return View(requestProduct);
            }
        }


        // Se sterge un produs din baza de date 
        [HttpPost]
        [Authorize(Roles = "Colab,Admin")]
        public ActionResult Delete(int id)
        {

            Product product = db.Products.Include("Comments")
                                         .Where(prod => prod.Id == id)
                                         .First(); 

            if (product.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                db.Products.Remove(product);
                db.SaveChanges();
                TempData["message"] = "Produsul a fost sters";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["message"] = "Nu aveti dreptul sa stergeti un produs care nu va apartine";
                return RedirectToAction("Index");
            }
        }


        [NonAction]
        public IEnumerable<SelectListItem> GetAllCategories()
        {
            // generam o lista de tipul SelectListItem fara elemente
            var selectList = new List<SelectListItem>();

            // extragem toate categoriile din baza de date
            var categories = from cat in db.Categories
                             select cat;

            // iteram prin categorii
            foreach (var category in categories)
            {
                // adaugam in lista elementele necesare pentru dropdown
                // id-ul categoriei si denumirea acesteia
                selectList.Add(new SelectListItem
                {
                    Value = category.Id.ToString(),
                    Text = category.CategoryName.ToString()
                });
            }
            /* Sau se poate implementa astfel: 
             * 
            foreach (var category in categories)
            {
                var listItem = new SelectListItem();
                listItem.Value = category.Id.ToString();
                listItem.Text = category.CategoryName.ToString();

                selectList.Add(listItem);
             }*/



            // returnam lista de categorii
            return selectList;
        }

        // Conditiile de afisare a butoanelor de editare si stergere
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



        /* IMAGINEA */

        /*// Afisam view-ul cu form-ul
        public IActionResult UploadImage()
        {
            return View();
        }*/

        // Facem upload la fisier si salvam modelul in baza de date
        [HttpPost]
        [Authorize(Roles = "Colab,Admin")]
        public async Task<IActionResult> New(Product product, IFormFile Image)
        {

            product.UserId = _userManager.GetUserId(User);

            // Verificam daca exista imaginea in request (daca a fost incarcata o imagine)
            if (Image.Length > 0)
            {
                // Generam calea de stocare a fisierului
                var storagePath = Path.Combine(
                _env.WebRootPath, // Luam calea folderului wwwroot
                "images", // Adaugam calea folderului images
                Image.FileName // Numele fisierului
                );

                // General calea de afisare a fisierului care va fi stocata in baza de date
                var databaseFileName = "/images/" + Image.FileName;

                // Uploadam fisierul la calea de storage
                using (var fileStream = new FileStream(storagePath, FileMode.Create))
                {
                    await Image.CopyToAsync(fileStream);
                }


                // Salvam storagePath-ul in baza de date
                product.Image = databaseFileName;
               // _context.Products.Add(product);
               // _context.SaveChanges();
            }

            if (ModelState.IsValid)
            {
                
                db.Products.Add(product);
                db.SaveChanges();
                TempData["message"] = "Produsul a fost adaugat";
                return RedirectToAction("Index");
            }
            else
            {
                product.Categ = GetAllCategories();
                return View(product);
            }
            
            
        }


        [NonAction]
        public float UpdateRating(Comment comment, bool add)
        {
            var comments = from comm in db.Comments.Where(comm => comm.ProductId == comment.ProductId)
                           select comm;     // Cautam comentariile corespunzatore produsului la care s-a adaugat sau sters un comentariu

            int commentsSize;
            float total;

            if (add)        // cazul in care adaugam
            {
                commentsSize = 1;
                total = (int)comment.Rating;
            }
            else           // cazul in care stergem
            {
                commentsSize = 0;
                total = 0;
            }

            foreach (var comm in comments)          //calculam rating-ul total si numarul de comentarii
            {
                if (comm.Rating is not null)
                {
                    total += (int)comm.Rating;
                    commentsSize++;
                }
            }

            if (commentsSize != 0)                  //daca nu am ramas fara comentarii intoarcem raspunsul, altfel 0
                return (float)System.Math.Round(total / commentsSize, 2);
            else
                return 0;
        }


    }
}


