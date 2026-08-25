using Marketplace.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Marketplace.Web.Controllers
{
    [Authorize]
    public class WishlistCollectionController : Controller
    {
        private readonly IWishlistCollectionService _collectionService;

        public WishlistCollectionController(IWishlistCollectionService collectionService)
        {
            _collectionService = collectionService;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Користувач не авторизований");
        }

        // GET: /WishlistCollection
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var collections = await _collectionService.GetCollectionsAsync(userId);
            return View(collections);
        }

        // GET: /WishlistCollection/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var userId = GetUserId();
            var collection = await _collectionService.GetCollectionAsync(userId, id);

            if (collection == null)
                return NotFound();

            return View(collection);
        }

        // GET: /WishlistCollection/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /WishlistCollection/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Назва не може бути порожньою.";
                return View();
            }

            var userId = GetUserId();
            var result = await _collectionService.CreateCollectionAsync(userId, name);

            if (!result.Succeeded)
            {
                TempData["Error"] = result.Message;
                return View();
            }

            TempData["Success"] = "Колекцію створено!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /WishlistCollection/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var userId = GetUserId();
            var collection = await _collectionService.GetCollectionAsync(userId, id);

            if (collection == null)
                return NotFound();

            return View(collection);
        }

        // POST: /WishlistCollection/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Назва не може бути порожньою.";
                return View();
            }

            var userId = GetUserId();
            var result = await _collectionService.UpdateCollectionAsync(userId, id, name);

            if (!result.Succeeded)
            {
                TempData["Error"] = result.Message;
                return View();
            }

            TempData["Success"] = "Колекцію оновлено!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /WishlistCollection/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetUserId();
            var result = await _collectionService.DeleteCollectionAsync(userId, id);

            if (!result.Succeeded)
            {
                TempData["Error"] = result.Message;
            }
            else
            {
                TempData["Success"] = "Колекцію видалено!";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /WishlistCollection/AddItem/{collectionId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(Guid collectionId, Guid productId, string? returnUrl = null)
        {
            Console.WriteLine($"=== AddToCollectionAsync ===");
            
            Console.WriteLine($"collectionId: {collectionId}");
            Console.WriteLine($"productId: {productId}");
            var userId = GetUserId();
            var result = await _collectionService.AddToCollectionAsync(userId, collectionId, productId);

            if (!result.Succeeded)
            {
                TempData["Error"] = result.Message;
            }
            else
            {
                TempData["Success"] = "Товар додано до колекції!";
            }

            return Redirect(returnUrl ?? Url.Action(nameof(Details), new { id = collectionId })!);
        }

        // POST: /WishlistCollection/RemoveItem/{collectionId}/{productId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(Guid collectionId, Guid productId, string? returnUrl = null)
        {
            var userId = GetUserId();
            var result = await _collectionService.RemoveFromCollectionAsync(userId, collectionId, productId);

            if (!result.Succeeded)
            {
                TempData["Error"] = result.Message;
            }
            else
            {
                TempData["Success"] = "Товар видалено з колекції!";
            }

            return Redirect(returnUrl ?? Url.Action(nameof(Details), new { id = collectionId })!);
        }
    }
}