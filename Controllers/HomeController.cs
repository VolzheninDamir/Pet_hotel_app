using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using IDZ_WEB.Models;
using IDZ_WEB.Models.ViewModels;

namespace IDZ_WEB.Controllers
{
    public class HomeController : Controller
    {
        [Authorize(Roles = "Admin, Participant")]
        public ActionResult Owners()
        {
            List<Owner> owners = new List<Owner>();
            using (var db = new IDZEntities1())
            {
                owners = db.Owners.OrderBy(x => x.FirstName).ThenBy(x => x.LastName).ThenBy(x => x.OwnerID).ThenBy(x => x.PhoneNumber).ToList();
            }
            return View(owners);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult CreateOwner()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult CreateOwner(Owner newOwner)
        {
            if (ModelState.IsValid)
            {
                using (var context = new IDZEntities1())
                {
                    Owner owner = new Owner()
                    {
                        OwnerID = Guid.NewGuid(),
                        FirstName = newOwner.FirstName,
                        LastName = newOwner.LastName,
                        PhoneNumber = newOwner.PhoneNumber,
                    };
                    context.Owners.Add(owner);
                    context.SaveChanges();
                }
                return RedirectToAction("Owners");
            }
            return View(newOwner);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult EditOwner(Guid OwnerID)
        {
            Owner model;
            using (var context = new IDZEntities1())
            {
                Owner owner = context.Owners.Find(OwnerID);
                model = new Owner
                {
                    OwnerID = owner.OwnerID,
                    FirstName = owner.FirstName,
                    LastName = owner.LastName,
                    PhoneNumber = owner.PhoneNumber,
                };
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult EditOwner(Owner model)
        {
            if (ModelState.IsValid)
            {
                using (var context = new IDZEntities1())
                {
                    Owner editedStudent = new Owner
                    {
                        OwnerID = model.OwnerID,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        PhoneNumber = model.PhoneNumber,
                    };
                    context.Owners.Attach(editedStudent);
                    context.Entry(editedStudent).State = System.Data.Entity.EntityState.Modified;
                    context.SaveChanges();
                }
                return RedirectToAction("Owners");
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult DeleteOwner(Guid OwnerID)
        {
            Owner ownerToDelete;
            using (var context = new IDZEntities1())
            {
                ownerToDelete = context.Owners.Find(OwnerID);
            }
            return View(ownerToDelete);
        }

        [HttpPost, ActionName("DeleteOwner")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(Guid ownerID)
        {
            using (var context = new IDZEntities1())
            {
                var owner = context.Owners
                                   .Include(o => o.Pets.Select(p => p.Bookings.Select(b => b.Payments)))
                                   .FirstOrDefault(o => o.OwnerID == ownerID);
                if (owner != null)
                {
                    foreach (var pet in owner.Pets.ToList())
                    {
                        foreach (var booking in pet.Bookings.ToList())
                        {
                            context.Payments.RemoveRange(booking.Payments);
                            context.Bookings.Remove(booking);
                        }
                        context.Pets.Remove(pet);
                    }

                    context.Owners.Remove(owner);
                    context.SaveChanges();
                }
                else
                {
                    return HttpNotFound();
                }
            }
            return RedirectToAction("Owners");
        }

        [Authorize(Roles = "Admin, Participant")]
        public ActionResult Pets(Guid ownerID)
        {
            using (var db = new IDZEntities1())
            {
                var owner = db.Owners.Include("Pets").FirstOrDefault(x => x.OwnerID == ownerID);
                if (owner != null)
                {
                    ViewBag.OwnerID = ownerID;
                    return View(owner.Pets.ToList());
                }
                return HttpNotFound();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult CreatePet(Guid ownerID)
        {
            ViewBag.OwnerID = ownerID;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreatePet(Pet newPet, Guid ownerID)
        {
            if (ModelState.IsValid)
            {
                using (var context = new IDZEntities1())
                {
                    newPet.PetID = Guid.NewGuid();
                    newPet.OwnerID = ownerID;
                    context.Pets.Add(newPet);
                    context.SaveChanges();
                }
                return RedirectToAction("Pets", new { ownerID });
            }
            ViewBag.OwnerID = ownerID;
            return View(newPet);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult DeletePet(Guid petID)
        {
            Pet pet;
            using (var context = new IDZEntities1())
            {
                pet = context.Pets
                             .Include(p => p.Bookings.Select(b => b.Payments))
                             .Where(p => p.PetID == petID)
                             .FirstOrDefault();
            }
            if (pet == null)
            {
                return HttpNotFound();
            }
            return View(pet);
        }

        [HttpPost, ActionName("DeletePet")]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePetConfirmed(Guid petID)
        {
            Guid ownerID;
            using (var context = new IDZEntities1())
            {
                var pet = context.Pets
                                 .Include(p => p.Bookings.Select(b => b.Payments))
                                 .Where(p => p.PetID == petID)
                                 .FirstOrDefault();
                if (pet != null)
                {
                    ownerID = pet.OwnerID; 
                    foreach (var booking in pet.Bookings.ToList())
                    {
                        context.Payments.RemoveRange(booking.Payments);
                        context.Bookings.Remove(booking);
                    }
                    context.Pets.Remove(pet);
                    context.SaveChanges();
                }
                else
                {
                    return HttpNotFound();
                }
            }
            return RedirectToAction("Pets", new { ownerID = ownerID });
        }

        [Authorize(Roles = "Admin, Participant")]
        public ActionResult Bookings(string ownerFirstName, string ownerLastName, string petName)
        {
            List<Booking1> bookings;
            List<string> ownerFirstNames;
            List<string> ownerLastNames;
            List<string> petNames;

            using (var context = new IDZEntities1())
            {
                bookings = context.Bookings1
                                    .Where(b => (string.IsNullOrEmpty(ownerFirstName) || b.Имя_хозяина.Contains(ownerFirstName))
                                            && (string.IsNullOrEmpty(ownerLastName) || b.Фамилия_хозяина.Contains(ownerLastName))
                                            && (string.IsNullOrEmpty(petName) || b.Кличка.Contains(petName)))
                                    .ToList();

                ownerFirstNames = context.Owners.Select(o => o.FirstName).Distinct().ToList();
                ownerLastNames = context.Owners.Select(o => o.LastName).Distinct().ToList();
                petNames = context.Pets.Select(p => p.Name).Distinct().ToList();
            }

            ViewBag.OwnerFirstNames = ownerFirstNames;
            ViewBag.OwnerLastNames = ownerLastNames;
            ViewBag.PetNames = petNames;

            ViewBag.SelectedOwnerFirstName = ownerFirstName;
            ViewBag.SelectedOwnerLastName = ownerLastName;
            ViewBag.SelectedPetName = petName;

            return View(bookings);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult CreateBooking()
        {
            using (var context = new IDZEntities1())
            {
                ViewBag.OwnerFirstNames = context.Owners.Select(o => o.FirstName).Distinct().ToList();
                ViewBag.OwnerLastNames = context.Owners.Select(o => o.LastName).Distinct().ToList();
                ViewBag.PetNames = context.Pets.Select(p => p.Name).Distinct().ToList();
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateBooking(string ownerFirstName, string ownerLastName, string petName, DateTime checkinDate, DateTime checkoutDate, int roomID)
        {
            using (var context = new IDZEntities1())
            {
                var result = context.CreateBooking(ownerFirstName, ownerLastName, petName, checkinDate, checkoutDate, roomID);
            }
            return RedirectToAction("Bookings");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult DeleteBooking(Guid bookingID)
        {
            Booking1 booking;
            using (var context = new IDZEntities1())
            {
                booking = context.Bookings1
                                 .Where(b => b.BookingID == bookingID)
                                 .FirstOrDefault();
            }
            if (booking == null)
            {
                return HttpNotFound();
            }
            return View(booking);
        }

        [HttpPost, ActionName("DeleteBooking")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteBookingConfirmed(Guid bookingID)
        {
            using (var context = new IDZEntities1())
            {
                context.Database.ExecuteSqlCommand("DELETE FROM Payments WHERE BookingID = @p0", bookingID);
                context.Database.ExecuteSqlCommand("DELETE FROM Bookings WHERE BookingID = @p0", bookingID);
            }
            return RedirectToAction("Bookings");
        }

        [AllowAnonymous]
        public ActionResult Price()
        {
            List<Room> rooms;

            using (var context = new IDZEntities1())
            {
                rooms = context.Rooms.ToList();
            }

            return View(rooms);
        }

        [Authorize(Roles = "Admin, Participant")]
        public ActionResult Payments(Guid bookingID)
        {
            List<Payment> payments;

            using (var context = new IDZEntities1())
            {
                payments = context.Payments.Where(p => p.BookingID == bookingID).ToList();
            }

            ViewBag.BookingID = bookingID;
            return View(payments);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult AddPayment(Guid bookingID)
        {
            var payment = new Payment
            {
                PaymentID = Guid.NewGuid(),
                BookingID = bookingID,
                PaymentDate = DateTime.Now
            };
            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPayment(Payment payment)
        {
            if (ModelState.IsValid)
            {
                payment.PaymentID = Guid.NewGuid();

                using (var context = new IDZEntities1())
                {
                    context.Payments.Add(payment);
                    context.SaveChanges();
                }
                return RedirectToAction("Payments", new { bookingID = payment.BookingID });
            }
            return View(payment);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult EditPayment(Guid paymentID)
        {
            Payment payment;
            using (var context = new IDZEntities1())
            {
                payment = context.Payments
                                 .Where(p => p.PaymentID == paymentID)
                                 .FirstOrDefault();
            }
            if (payment == null)
            {
                return HttpNotFound();
            }
            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPayment(Payment payment)
        {
            if (ModelState.IsValid)
            {
                using (var context = new IDZEntities1())
                {
                    context.Entry(payment).State = EntityState.Modified;
                    context.SaveChanges();
                }
                return RedirectToAction("Payments", new { bookingID = payment.BookingID });
            }
            return View(payment);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult DeletePayment(Guid paymentID)
        {
            Payment payment;
            using (var context = new IDZEntities1())
            {
                payment = context.Payments
                                 .Where(p => p.PaymentID == paymentID)
                                 .FirstOrDefault();
            }
            if (payment == null)
            {
                return HttpNotFound();
            }
            return View(payment);
        }

        [HttpPost, ActionName("DeletePayment")]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePaymentConfirmed(Guid paymentID)
        {
            Guid bookingID;
            using (var context = new IDZEntities1())
            {
                var payment = context.Payments
                                     .Where(p => p.PaymentID == paymentID)
                                     .FirstOrDefault();
                if (payment != null)
                {
                    bookingID = payment.BookingID;
                    context.Payments.Remove(payment);
                    context.SaveChanges();
                }
                else
                {
                    return HttpNotFound();
                }
            }
            return RedirectToAction("Payments", new { bookingID = bookingID });
        }

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Login(UserVM webUser, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                using (IDZEntities1 context = new IDZEntities1())
                {
                    User user = null;
                    user = context.Users.Where(u => u.Login == webUser.Login).FirstOrDefault();
                    if (user != null)
                    {
                        string passwordHash = ReturnHashCode(webUser.Password + user.Salt.ToString().ToUpper());
                        if (passwordHash == user.PasswordHash)
                        {
                            string userRole = "";
                            switch (user.UserRole)
                            {
                                case 1:
                                    userRole = "Admin";
                                    break;
                                case 2:
                                    userRole = "Participant";
                                    break;
                            }
                            FormsAuthenticationTicket authTicket = new FormsAuthenticationTicket(
                                                            1,
                                                            user.Login,
                                                            DateTime.Now,
                                                            DateTime.Now.AddDays(1),
                                                            false,
                                                            userRole
                                                            );
                            string encryptedTicket = FormsAuthentication.Encrypt(authTicket);
                            HttpContext.Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket));
                            return RedirectToLocal(returnUrl);
                        }
                    }
                }
            }
            ViewBag.Error = "Пользователя с таким логином и паролем не существует, попробуйте еще раз.";
            return View(webUser);
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Price", "Home");
        }

        string ReturnHashCode(string loginAndSalt)
        {
            string hash = "";
            using (SHA1 sha1Hash = SHA1.Create())
            {
                byte[] data = sha1Hash.ComputeHash(Encoding.UTF8.GetBytes(loginAndSalt));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                hash = sBuilder.ToString().ToUpper();
            }
            return hash;
        }
        public ActionResult LogOut()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Price", "Home");
        }
    }
}