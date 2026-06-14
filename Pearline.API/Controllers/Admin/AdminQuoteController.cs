using Pearline.Domain.Enums;
using Pearline.Infrastructure.Data;
using Pearline.Application.DTOs.Quote;
using Pearline.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Pearline.API.Controllers.Admin
{
    [Route("api/admin/quotes")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminQuoteController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminQuoteController> _logger;

        public AdminQuoteController(ApplicationDbContext context, ILogger<AdminQuoteController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllQuotesNoPagingAsync()
        {
            var quotes = await _context.Quotes
                .Include(q => q.Items)
                .AsNoTracking()
                .OrderByDescending(q => q.DateCreated)
                .ToListAsync();

            if (!quotes.Any())
            {
                _logger.LogInformation("GetAllQuotesNoPaging: no quotes found.");
                return Ok(new List<QuoteAdminDetailDto>());
            }

            var dtos = quotes.Select(q => new QuoteAdminDetailDto
            {
                Id = q.Id,
                Email = q.Email ?? string.Empty,
                Comments = string.IsNullOrWhiteSpace(q.Comments) ? null : q.Comments,
                TotalPrice = q.TotalPrice,
                DateCreated = q.DateCreated,
                UserId = string.IsNullOrWhiteSpace(q.UserId) ? null : q.UserId,
                Status = q.Status,
                Items = (q.Items ?? Enumerable.Empty<QuoteItem>()).Select(i => new QuoteItemResponseDto
                {
                    Id = i.Id,
                    Barcode = i.Barcode,
                    ProductName = i.ProductName,
                    Brand = i.Brand,
                    ProductImage = i.ProductImage,
                    CaseSize = i.CaseSize,
                    CasesPerLayer = i.CasesPerLayer,
                    CasesPerPallet = i.CasesPerPallet,
                    LeadTimeDays = i.LeadTimeDays,
                    CasePrice = i.CasePrice,
                    UnitPrice = i.UnitPrice,
                    IsAvailable = i.IsAvailable,
                    Description = i.Description,
                    Ingredients = i.Ingredients,
                    Usage = i.Usage,
                    CategoryId = i.CategoryId,
                    CategoryName = i.CategoryName,
                    Quantity = i.Quantity,
                    IsCase = i.IsCase,
                    Subtotal = i.Subtotal,
                }).ToList()
            }).ToList();

            return Ok(dtos);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllQuotesAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? email = null)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 500) pageSize = 25;

            var query = _context.Quotes.Include(q => q.Items).AsNoTracking().AsQueryable();

            if (from.HasValue)
                query = query.Where(q => q.DateCreated >= from.Value);

            if (to.HasValue)
                query = query.Where(q => q.DateCreated <= to.Value);

            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(q => q.Email != null && q.Email.Contains(email));

            var total = await query.CountAsync();

            var quotes = await query
                .OrderByDescending(q => q.DateCreated)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = quotes.Select(q => new QuoteAdminListDto
            {
                Id = q.Id,
                Email = q.Email ?? string.Empty,
                TotalPrice = q.TotalPrice,
                DateCreated = q.DateCreated,
                ItemCount = q.Items?.Count ?? 0,
                Status = q.Status
            }).ToList();

            var response = new PagedResponseDto<QuoteAdminListDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                Items = items
            };

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuoteByIdAsync(int id)
        {
            var quote = await _context.Quotes
                .Include(q => q.Items)
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quote == null)
            {
                _logger.LogInformation("GetQuoteById: quote {Id} not found.", id);
                return NotFound(new { message = "Quote not found" });
            }

            var dto = new QuoteAdminDetailDto
            {
                Id = quote.Id,
                Email = quote.Email ?? string.Empty,
                Comments = string.IsNullOrWhiteSpace(quote.Comments) ? null : quote.Comments,
                TotalPrice = quote.TotalPrice,
                DateCreated = quote.DateCreated,
                UserId = string.IsNullOrWhiteSpace(quote.UserId) ? null : quote.UserId,
                Status = quote.Status,
                Items = (quote.Items ?? Enumerable.Empty<QuoteItem>()).Select(i => new QuoteItemResponseDto
                {
                    Id = i.Id,
                    Barcode = i.Barcode,
                    ProductName = i.ProductName,
                    Brand = i.Brand,
                    ProductImage = i.ProductImage,
                    CaseSize = i.CaseSize,
                    CasesPerLayer = i.CasesPerLayer,
                    CasesPerPallet = i.CasesPerPallet,
                    LeadTimeDays = i.LeadTimeDays,
                    CasePrice = i.CasePrice,
                    UnitPrice = i.UnitPrice,
                    IsAvailable = i.IsAvailable,
                    Description = i.Description,
                    Ingredients = i.Ingredients,
                    Usage = i.Usage,
                    CategoryId = i.CategoryId,
                    CategoryName = i.CategoryName,
                    Quantity = i.Quantity,
                    IsCase = i.IsCase,
                    Subtotal = i.Subtotal
                }).ToList()
            };

            return Ok(dto);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetQuotesByUserAsync(string userId,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { message = "userId is required" });

            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 500) pageSize = 50;

            var query = _context.Quotes
                .Include(q => q.Items)
                .AsNoTracking()
                .Where(q => q.UserId == userId);

            var total = await query.CountAsync();

            var quotes = await query
                .OrderByDescending(q => q.DateCreated)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!quotes.Any())
            {
                _logger.LogInformation("GetQuotesByUser: no quotes for user {UserId}.", userId);
                return NotFound(new { message = "No quotes found for this user" });
            }

            var items = quotes.Select(q => new QuoteAdminListDto
            {
                Id = q.Id,
                Email = q.Email ?? string.Empty,
                TotalPrice = q.TotalPrice,
                DateCreated = q.DateCreated,
                ItemCount = q.Items?.Count ?? 0,
                Status = q.Status
            }).ToList();

            var response = new PagedResponseDto<QuoteAdminListDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                Items = items
            };

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuoteAsync(int id)
        {
            var quote = await _context.Quotes
                .Include(q => q.Items)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quote == null)
            {
                _logger.LogWarning("DeleteQuote: quote {Id} not found.", id);
                return NotFound(new { message = "Quote not found" });
            }

            if (quote.Items != null && quote.Items.Any())
                _context.QuoteItems.RemoveRange(quote.Items);

            _context.Quotes.Remove(quote);
            await _context.SaveChangesAsync();

            _logger.LogInformation("DeleteQuote: quote {Id} deleted.", id);
            return Ok(new { message = "Quote deleted successfully" });
        }

        [HttpDelete("all")]
        public async Task<IActionResult> DeleteAllQuotesAsync()
        {
            var allQuotes = await _context.Quotes.Include(q => q.Items).ToListAsync();

            if (!allQuotes.Any())
            {
                _logger.LogInformation("DeleteAllQuotes: no quotes to delete.");
                return NotFound(new { message = "No quotes found" });
            }

            var allItems = allQuotes.SelectMany(q => q.Items).ToList();
            if (allItems.Any())
                _context.QuoteItems.RemoveRange(allItems);

            _context.Quotes.RemoveRange(allQuotes);
            await _context.SaveChangesAsync();

            _logger.LogInformation("DeleteAllQuotes: removed all quotes.");
            return Ok(new { message = "All quotes deleted successfully" });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateQuoteStatusAsync(int id, [FromBody] UpdateQuoteStatusDto dto)
        {
            var quote = await _context.Quotes.FindAsync(id);
            if (quote == null)
            {
                _logger.LogWarning("UpdateQuoteStatus: quote {Id} not found.", id);
                return NotFound("Quote not found.");
            }

            if (!Enum.IsDefined(typeof(QuoteStatus), dto.Status))
            {
                var allowed = string.Join(", ", Enum.GetNames(typeof(QuoteStatus)));
                return BadRequest($"Invalid status. Allowed: {allowed}");
            }

            quote.Status = dto.Status;
            await _context.SaveChangesAsync();

            _logger.LogInformation("UpdateQuoteStatus: quote {Id} status updated to {Status}.", id, dto.Status);
            return Ok(new { message = "Quote status updated successfully", status = quote.Status });
        }
        [AllowAnonymous]
        [HttpGet("api/admin/diagnostic")]
        public IActionResult Diagnostic()
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            return Ok(new
            {
                Assembly = asm.GetName().Name,
                Version = asm.GetName().Version?.ToString(),
                BuildTimeUtc = System.IO.File.GetLastWriteTimeUtc(asm.Location).ToString("o"),
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "NotSet"
            });
        }

    }
}
