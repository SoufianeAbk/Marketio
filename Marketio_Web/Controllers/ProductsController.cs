using Marketio_Shared.DTOs;
using Marketio_Shared.Enums;
using Marketio_Shared.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketio_Web.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        // GET: Products - Met Search & Filter
        public async Task<IActionResult> Index(
            string? searchTerm = null,
            ProductCategory? category = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string sortBy = "name")
        {
            try
            {
                // Haal alle producten op
                var products = await _productService.GetAllProductsAsync();

                // Filter op categorie
                if (category.HasValue)
                {
                    products = products.Where(p => p.Category == category.Value);
                }

                // Zoeken op naam of beschrijving
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    products = products.Where(p =>
                        p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                }

                // Filter op prijsrange
                if (minPrice.HasValue)
                {
                    products = products.Where(p => p.Price >= minPrice.Value);
                }

                if (maxPrice.HasValue)
                {
                    products = products.Where(p => p.Price <= maxPrice.Value);
                }

                // Sorteer producten
                products = sortBy?.ToLower() switch
                {
                    "price-asc" => products.OrderBy(p => p.Price),
                    "price-desc" => products.OrderByDescending(p => p.Price),
                    "name-desc" => products.OrderByDescending(p => p.Name),
                    "newest" => products.OrderByDescending(p => p.Id), // Nieuwste eerst
                    _ => products.OrderBy(p => p.Name) // Default: naam A-Z
                };

                // ViewBag data voor filters
                ViewBag.Categories = Enum.GetValues<ProductCategory>();
                ViewBag.SelectedCategory = category;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.MinPrice = minPrice;
                ViewBag.MaxPrice = maxPrice;
                ViewBag.SortBy = sortBy;

                // Prijs statistieken voor slider
                if (products.Any())
                {
                    ViewBag.LowestPrice = products.Min(p => p.Price);
                    ViewBag.HighestPrice = products.Max(p => p.Price);
                }
                else
                {
                    ViewBag.LowestPrice = 0;
                    ViewBag.HighestPrice = 1000;
                }

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products");
                TempData["Error"] = "Er is een fout opgetreden bij het laden van de producten.";
                return View(new List<ProductDto>());
            }
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product details for ID: {ProductId}", id);
                TempData["Error"] = "Er is een fout opgetreden bij het laden van het product.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Products/Create - ✅ Alleen Admin
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.Categories = Enum.GetValues<ProductCategory>();
            return View();
        }

        // POST: Products/Create
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductDto productDto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = Enum.GetValues<ProductCategory>();
                return View(productDto);
            }

            try
            {
                await _productService.CreateProductAsync(productDto);
                TempData["Success"] = "Product succesvol aangemaakt!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                ModelState.AddModelError("", "Er is een fout opgetreden bij het aanmaken van het product.");
                ViewBag.Categories = Enum.GetValues<ProductCategory>();
                return View(productDto);
            }
        }

        // GET: Products/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                ViewBag.Categories = Enum.GetValues<ProductCategory>();
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product for edit, ID: {ProductId}", id);
                TempData["Error"] = "Er is een fout opgetreden bij het laden van het product.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Products/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductDto productDto)
        {
            if (id != productDto.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = Enum.GetValues<ProductCategory>();
                return View(productDto);
            }

            try
            {
                await _productService.UpdateProductAsync(productDto);
                TempData["Success"] = "Product succesvol bijgewerkt!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product, ID: {ProductId}", id);
                ModelState.AddModelError("", "Er is een fout opgetreden bij het bijwerken van het product.");
                ViewBag.Categories = Enum.GetValues<ProductCategory>();
                return View(productDto);
            }
        }

        // GET: Products/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product for delete, ID: {ProductId}", id);
                TempData["Error"] = "Er is een fout opgetreden bij het laden van het product.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Products/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _productService.DeleteProductAsync(id);
                TempData["Success"] = "Product succesvol verwijderd!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product, ID: {ProductId}", id);
                TempData["Error"] = "Er is een fout opgetreden bij het verwijderen van het product.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}