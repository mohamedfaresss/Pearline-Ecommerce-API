using Pearline.Infrastructure.Data;
using Pearline.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Pearline.API.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminMessagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminMessagesController(ApplicationDbContext context)
        {
            _context = context;
        }

      
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContactMessage>>> GetMessages()
        {
            var messages = await _context.ContactMessages
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return Ok(messages);
        }

    
        [HttpGet("{id}")]
        public async Task<ActionResult<ContactMessage>> GetMessage(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);

            if (message == null)
                return NotFound(new { message = "Message not found" });

            return Ok(message);
        }

     
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);

            if (message == null)
                return NotFound(new { message = "Message not found" });

            _context.ContactMessages.Remove(message);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Message deleted successfully" });
        }

   
        [HttpDelete]
        public async Task<IActionResult> DeleteAllMessages()
        {
            var messages = await _context.ContactMessages.ToListAsync();

            if (messages.Count == 0)
                return NotFound(new { message = "No messages found to delete" });

            _context.ContactMessages.RemoveRange(messages);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{messages.Count} messages deleted successfully" });
        }
    }
}
