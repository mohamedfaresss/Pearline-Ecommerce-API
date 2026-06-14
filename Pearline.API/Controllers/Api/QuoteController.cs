using Pearline.Infrastructure.Data;
using Pearline.Application.DTOs.Quote;
using Pearline.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Pearline.API.Controllers.Api
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class QuoteController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private const decimal TaxRate = 0.00m;

        public QuoteController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyQuotesAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var quotes = await _context.Quotes
                .Include(q => q.Items)
                .Where(q => q.UserId == userId)
                .OrderByDescending(q => q.DateCreated)
                .ToListAsync();

            if (!quotes.Any())
                return NotFound(new { message = "No quotes found for this user" });

            var response = quotes.Select(q => new
            {
                q.Id,
                q.Email,
                q.Comments,
                q.TotalPrice,
                q.DateCreated,
                Items = q.Items.Select(i => new
                {
                    i.Id,
                    i.Barcode,
                    i.ProductName,
                    i.Brand,
                    i.ProductImage,
                    i.CaseSize,
                    i.CasesPerLayer,
                    i.CasesPerPallet,
                    i.LeadTimeDays,
                    i.CasePrice,
                    i.UnitPrice,
                    i.IsAvailable,
                    i.Description,
                    i.Ingredients,
                    i.Usage,
                    i.CategoryId,
                    i.CategoryName,
                    i.Quantity,
                    i.IsCase,
                    i.Subtotal
                })
            });

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuoteByIdAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var quote = await _context.Quotes
                .Include(q => q.Items)
                .FirstOrDefaultAsync(q => q.Id == id && q.UserId == userId);

            if (quote == null)
                return NotFound(new { message = "Quote not found" });

            var response = new
            {
                quote.Id,
                quote.Email,
                quote.Comments,
                quote.TotalPrice,
                quote.DateCreated,
                Items = quote.Items.Select(i => new
                {
                    i.Id,
                    i.Barcode,
                    i.ProductName,
                    i.Brand,
                    i.ProductImage,
                    i.CaseSize,
                    i.CasesPerLayer,
                    i.CasesPerPallet,
                    i.LeadTimeDays,
                    i.CasePrice,
                    i.UnitPrice,
                    i.IsAvailable,
                    i.Description,
                    i.Ingredients,
                    i.Usage,
                    i.CategoryId,
                    i.CategoryName,
                    i.Quantity,
                    i.IsCase,
                    i.Subtotal
                })
            };

            return Ok(response);
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitQuoteAsync([FromBody] QuoteRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
                return BadRequest("Cart is empty.");

            var subtotal = 0m;
            var quote = new Quote
            {
                Email = string.IsNullOrWhiteSpace(request.Email) ? userEmail ?? string.Empty : request.Email,
                Comments = request.Comments,
                CartId = cart.Id,
                UserId = userId
            };

            foreach (var cartItem in cart.Items)
            {
                var product = cartItem.Product;
                var price = cartItem.IsCase ? product?.CasePrice ?? 0m : product?.UnitPrice ?? 0m;
                var lineSubtotal = price * cartItem.Quantity;
                subtotal += lineSubtotal;

                var quoteItem = new QuoteItem
                {
                    Barcode = product?.Barcode ?? cartItem.ProductBarcode,
                    ProductName = product?.ProductName ?? string.Empty,
                    Brand = product?.Brand ?? string.Empty,
                    ProductImage = product?.ProductImage ?? string.Empty,
                    CaseSize = product?.CaseSize ?? 1,
                    CasesPerLayer = product?.CasesPerLayer ?? 0,
                    CasesPerPallet = product?.CasesPerPallet ?? 0,
                    LeadTimeDays = product?.LeadTimeDays ?? 0,
                    CasePrice = product?.CasePrice ?? 0m,
                    UnitPrice = product?.UnitPrice ?? 0m,
                    IsAvailable = product?.IsAvailable ?? false,
                    Description = product?.Description ?? string.Empty,
                    Ingredients = product?.Ingredients ?? string.Empty,
                    Usage = product?.Usage ?? string.Empty,
                    CategoryId = product?.CategoryId ?? 0,
                    CategoryName = product?.Category?.Name ?? string.Empty,
                    Quantity = cartItem.Quantity,
                    IsCase = cartItem.IsCase,
                    Subtotal = lineSubtotal
                };

                quote.Items.Add(quoteItem);
            }

            var tax = Math.Round(subtotal * TaxRate, 2);
            quote.TotalPrice = Math.Round(subtotal + tax, 2);

            _context.Quotes.Add(quote);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Quote submitted successfully",
                quoteId = quote.Id,
                total = quote.TotalPrice
            });
        }
    }
}
