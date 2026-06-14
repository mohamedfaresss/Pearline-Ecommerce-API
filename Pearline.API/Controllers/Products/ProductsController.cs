using Pearline.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pearline.Domain.Entities;

namespace Pearline.API.Controllers.Products
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsAsync()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .ToListAsync();

            return Ok(products);
        }

        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategoryAsync(int categoryId)
        {
            var products = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Category)
                .ToListAsync();

            return Ok(products);
        }

        [HttpGet("{barcode}")]
        public async Task<ActionResult<Product>> GetProductAsync(string barcode)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Barcode == barcode);

            if (product == null) return NotFound();

            return Ok(product);
        }

        [HttpPost]
        public async Task<ActionResult<Product>> CreateProductAsync(Product product)
        {
            if (product.Category != null)
            {
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == product.CategoryId);
                if (existingCategory != null)
                    product.Category = existingCategory;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProductAsync), new { barcode = product.Barcode }, product);
        }

        [HttpPut("{barcode}")]
        public async Task<IActionResult> UpdateProductAsync(string barcode, Product updatedProduct)
        {
            if (barcode != updatedProduct.Barcode) return BadRequest();

            _context.Entry(updatedProduct).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("category/{categoryId}")]
        public async Task<IActionResult> DeleteProductsByCategoryAsync(int categoryId)
        {
            var products = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();

            if (!products.Any())
                return NotFound(new { message = "No products found for this category." });

            _context.Products.RemoveRange(products);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("bulk-from-json-local")]
        public async Task<IActionResult> BulkAddProductsFromLocalJsonAsync([FromBody] List<ProductJsonDto> productsJson)
        {
            var categoriesDict = await _context.Categories.ToDictionaryAsync(c => c.Name);

            foreach (var productJson in productsJson)
            {
                if (!categoriesDict.TryGetValue(productJson.category, out var category))
                {
                    category = new Category { Name = productJson.category };
                    categoriesDict[productJson.category] = category;
                    _context.Categories.Add(category);
                }

                var ext = Path.GetExtension(productJson.productImage);

                var product = new Product
                {
                    Barcode = productJson.barcode,
                    ProductName = productJson.productName,
                    Brand = productJson.brand,
                    CaseSize = productJson.caseSize,
                    CasesPerLayer = productJson.casesPerLayer,
                    CasesPerPallet = productJson.casesPerPallet,
                    LeadTimeDays = productJson.leadTimeDays,
                    CasePrice = productJson.casePrice,
                    UnitPrice = productJson.unitPrice,
                    IsAvailable = productJson.isAvailable,
                    Description = productJson.description,
                    Ingredients = productJson.ingredients,
                    Usage = productJson.usage,
                    Category = category,
                    ProductImage = "/images/" + productJson.barcode + ext
                };

                _context.Products.Add(product);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "All products added successfully!" });
        }

        [HttpDelete("{barcode}")]
        public async Task<IActionResult> DeleteProductAsync(string barcode)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Barcode == barcode);

            if (product == null)
                return NotFound(new { message = "Product not found." });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Product deleted successfully." });
        }

        [HttpPost("create-with-image")]
        public async Task<IActionResult> CreateProductWithImageAsync([FromForm] ProductWithImageDto dto)
        {
            string? imagePath = null;

            if (dto.Image != null && dto.Image.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Image.FileName);
                var filePath = Path.Combine("wwwroot/images", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await dto.Image.CopyToAsync(stream);

                imagePath = "/images/" + fileName;
            }

            var product = new Product
            {
                Barcode = dto.Barcode,
                ProductName = dto.ProductName,
                Brand = dto.Brand,
                CategoryId = dto.CategoryId,
                UnitPrice = dto.UnitPrice,
                Description = dto.Description,
                ProductImage = imagePath,
                Ingredients = dto.Ingredients ?? "",
                Usage = dto.Usage ?? "",
                CaseSize = 0,
                CasesPerLayer = 0,
                CasesPerPallet = 0,
                LeadTimeDays = 0,
                CasePrice = 0,
                IsAvailable = false
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(product);
        }
        public class ProductWithImageDto
        {
            public string Barcode { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public string Brand { get; set; } = string.Empty;
            public int CategoryId { get; set; }
            public decimal UnitPrice { get; set; }
            public string? Description { get; set; }
            public string? Ingredients { get; set; }
            public string Usage { get; set; } = string.Empty;
            public IFormFile? Image { get; set; }
        }
    }
}
