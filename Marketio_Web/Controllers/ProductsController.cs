using Marketio_Shared.DTOs;
using Marketio_Shared.Enums;
using Marketio_Shared.Interfaces;
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

        // GET: Products
        public async Task<IActionResult> Index(ProductCategory? category = null)
        {
            try
            {
                IEnumerable<ProductDto> products;

                if (category.HasValue)
                {
                    products = await _productService.GetProductsByCategoryAsync(category.Value);
                    ViewBag.SelectedCategory = category.Value;
                }
                else
                {
                    products = await _productService.GetAllProductsAsync();
                }

                ViewBag.Categories = Enum.GetValues<ProductCategory>();
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

        // GET: Products/Create
        public IActionResult Create()
        {
            ViewBag.Categories = Enum.GetValues<ProductCategory>();
            return View();
        }

        // POST: Products/Create
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