using Pearline.Infrastructure.Data;
using Pearline.Application.DTOs.Cart;
using CartEntity = Pearline.Domain.Entities.Cart;
using CartItemEntity = Pearline.Domain.Entities.CartItem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Pearline.API.Controllers.Cart
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCartAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return Ok(new CartDto { Id = 0, Items = new List<CartItemDto>(), Total = 0 });

            var response = new CartDto
            {
                Id = cart.Id,
                Items = cart.Items.Select(i =>
                {
                    var product = i.Product;
                    var pricePerItem = i.IsCase ? (product?.CasePrice ?? 0m) : (product?.UnitPrice ?? 0m);

                    return new CartItemDto
                    {
                        Id = i.Id,
                        ProductBarcode = i.ProductBarcode,
                        ProductName = product?.ProductName,
                        ProductImage = product?.ProductImage,
                        Quantity = i.Quantity,
                        IsCase = i.IsCase,
                        PricePerItem = pricePerItem,
                        Subtotal = pricePerItem * i.Quantity,

                        CaseSize = product?.CaseSize ?? 1,
                        CasesPerLayer = product?.CasesPerLayer ?? 0,
                        CasesPerPallet = product?.CasesPerPallet ?? 0,
                        LeadTimeDays = product?.LeadTimeDays ?? 0
                    };
                }).ToList(),
                Total = cart.Items.Sum(i =>
                {
                    var product = i.Product;
                    var price = i.IsCase ? (product?.CasePrice ?? 0m) : (product?.UnitPrice ?? 0m);
                    return price * i.Quantity;
                })
            };
            return Ok(response);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCartAsync(string barcode, int quantity = 1, bool isCase = true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == barcode);
            if (product == null) return NotFound("Product not found");

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new CartEntity { UserId = userId };
                _context.Carts.Add(cart);
            }

            var existingItem = cart.Items.FirstOrDefault(i => i.ProductBarcode == product.Barcode && i.IsCase == isCase);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Items.Add(new CartItemEntity
                {
                    ProductBarcode = product.Barcode,
                    Quantity = quantity,
                    IsCase = isCase
                });
            }

            await _context.SaveChangesAsync();
            return await GetCartAsync();
        }

        [HttpDelete("remove/{barcode}")]
        public async Task<IActionResult> RemoveFromCartAsync(string barcode, bool isCase = true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return NotFound("Cart not found");

            var item = cart.Items.FirstOrDefault(i => i.ProductBarcode == barcode && i.IsCase == isCase);
            if (item == null) return NotFound("Item not found");

            cart.Items.Remove(item);
            await _context.SaveChangesAsync();

            return await GetCartAsync();
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCartAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return NotFound("Cart not found");

            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cart cleared successfully" });
        }
    }
}
