using ClientSphere.Models;
using ClientSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientSphere.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICloudinaryService _cloudinaryService;

        public ProductsController(IProductService productService, ICloudinaryService cloudinaryService)
        {
            _productService = productService;
            _cloudinaryService = cloudinaryService;
        }

        // GET: Products — supports ?archived=true
        public async Task<IActionResult> Index(bool archived = false)
        {
            var products = await _productService.GetAllProductsAsync();
            var filtered = archived
                ? products.Where(p => p.IsArchived)
                : products.Where(p => !p.IsArchived);
            ViewData["IsArchived"] = archived;
            return View(filtered);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var product = await _productService.GetProductByIdAsync(id.Value);
            if (product == null) return NotFound();
            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,StockQuantity")] Product product, IFormFile? productImage)
        {
            if (ModelState.IsValid)
            {
                if (productImage != null && productImage.Length > 0)
                {
                    try
                    {
                        string? url = await _cloudinaryService.UploadImageAsync(productImage, "products");
                        if (!string.IsNullOrEmpty(url)) product.ImageUrl = url;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Failed to upload product image: " + ex.Message);
                        return View(product);
                    }
                }

                await _productService.CreateProductAsync(product);
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var product = await _productService.GetProductByIdAsync(id.Value);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,StockQuantity,CreatedAt")] Product product, IFormFile? productImage)
        {
            if (id != product.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try 
                { 
                    if (productImage != null && productImage.Length > 0)
                    {
                        string? url = await _cloudinaryService.UploadImageAsync(productImage, "products");
                        if (!string.IsNullOrEmpty(url)) product.ImageUrl = url;
                    }
                    else
                    {
                        // Preserve existing image if not uploading a new one
                        var existingProduct = await _productService.GetProductByIdAsync(id);
                        if (existingProduct != null) product.ImageUrl = existingProduct.ImageUrl;
                    }

                    await _productService.UpdateProductAsync(product); 
                }
                catch (Exception)
                {
                    if (await _productService.GetProductByIdAsync(id) == null) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // POST: Products/Archive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            product.IsArchived = true;
            product.ArchivedAt = DateTime.UtcNow;
            await _productService.UpdateProductAsync(product);
            TempData["Success"] = $"\"{product.Name}\" has been archived.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Products/UnarchiveProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnarchiveProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            product.IsArchived = false;
            product.ArchivedAt = null;
            await _productService.UpdateProductAsync(product);
            TempData["Success"] = $"\"{product.Name}\" has been restored.";
            return RedirectToAction(nameof(Index), new { archived = true });
        }
    }
}
