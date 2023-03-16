using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Online_Shop.Data;
using Online_Shop.Models;
using System.Data;

namespace Online_Shop.Controllers
{
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public CommentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;
        }

        /* 
         
        // Adaugarea unui comentariu asociat unui produs in baza de date
        [HttpPost]
        public IActionResult New(Comment comm)
        {
            comm.Date = DateTime.Now;

            try
            {
                db.Comments.Add(comm);
                db.SaveChanges();
                return Redirect("/Products/Show/" + comm.ProductId);
            }

            catch (Exception)
            {
                return Redirect("/Products/Show/" + comm.ProductId);
            }

        }

        */


        // Stergerea unui comentariu asociat unui produs din baza de date
        [HttpPost]
        [Authorize(Roles = "User,Colab,Admin")]
        public IActionResult Delete(int id)
        {
            Comment comm = db.Comments.Find(id);

            if (comm.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                db.Comments.Remove(comm);
                db.SaveChanges();

                if (comm.Rating != null)         // Daca stergem un comentariu cu rating updatam in baza de date produsul
                {
                    var product = db.Products.Where(pd => pd.Id == comm.ProductId).First();     //Obtinem produsul caruia i-a fost sters comentariul
                    product.Stars = UpdateRating(comm, false);         //Updatam rating-ul
                }

                db.SaveChanges();
                /*TempData["Message"] = "Comentariul a fost sters";*/


                return Redirect("/Products/Show/" + comm.ProductId);
            }

            else
            {
                TempData["message"] = "Nu aveti dreptul sa stergeti comentariul";
                return RedirectToAction("Index", "Products");
            }
        
        }

        // In acest moment vom implementa editarea intr-o pagina View separata
        // Se editeaza un comentariu existent

        public IActionResult Edit(int id)
        {
            Comment comm = db.Comments.Find(id);

            if (comm.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                return View(comm);
            }

            else
            {
                TempData["message"] = "Nu aveti dreptul sa editati comentariul";
                return RedirectToAction("Index", "Products");
            }
        }

        [HttpPost]
        [Authorize(Roles = "User,Colab,Admin")]
        public IActionResult Edit(int id, Comment requestComment)
        {
            Comment comm = db.Comments.Find(id);

            if (comm.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                if (ModelState.IsValid)
                {
                    comm.Content = requestComment.Content;
                    comm.Rating = requestComment.Rating;

                    var product = db.Products.Find(comm.ProductId);
                    product.Stars = UpdateRating(comm, false);

                    db.SaveChanges();

                    return Redirect("/Products/Show/" + comm.ProductId);
                }
                else
                {
                    return View(requestComment);
                }
            }
            else
            {
                TempData["message"] = "Nu aveti dreptul sa faceti modificari";
                return RedirectToAction("Index", "Products");
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
