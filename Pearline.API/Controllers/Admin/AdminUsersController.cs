using Pearline.Infrastructure.Identity;
using Pearline.Application.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Pearline.API.Controllers.Admin
{
    [Route("api/admin/users")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminUsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();

            var userDtos = new List<UserWithRolesDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserWithRolesDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? string.Empty,
                    Roles = roles.ToList()
                });
            }

            return Ok(new ApiResponse<List<UserWithRolesDto>>
            {
                Success = true,
                Message = "Users retrieved successfully",
                Data = userDtos
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            var roles = await _userManager.GetRolesAsync(user);

            var userDto = new UserWithRolesDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList()
            };

            return Ok(new ApiResponse<UserWithRolesDto>
            {
                Success = true,
                Message = "User retrieved successfully",
                Data = userDto
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            var isTargetAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var currentUser = await _userManager.GetUserAsync(User);
            var isCurrentSuper = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "SuperAdmin");

            if (isTargetAdmin && !isCurrentSuper)
            {
                return Forbid();
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Failed to delete user"
                });
            }

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "User deleted successfully"
            });
        }

        [HttpPost("{id}/assign-admin")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> AssignAdminRoleAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new ApiResponse<string> { Success = false, Message = "User not found" });

            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
                return BadRequest(new ApiResponse<string> { Success = false, Message = "User already has Admin role" });

            var result = await _userManager.AddToRoleAsync(user, "Admin");
            if (!result.Succeeded)
                return BadRequest(new ApiResponse<string> { Success = false, Message = "Failed to assign Admin role" });

            return Ok(new ApiResponse<string> { Success = true, Message = "Admin role assigned" });
        }

        [HttpPost("{id}/revoke-admin")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> RevokeAdminRoleAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new ApiResponse<string> { Success = false, Message = "User not found" });

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Admin"))
                return BadRequest(new ApiResponse<string> { Success = false, Message = "User is not an Admin" });

            var result = await _userManager.RemoveFromRoleAsync(user, "Admin");
            if (!result.Succeeded)
                return BadRequest(new ApiResponse<string> { Success = false, Message = "Failed to revoke Admin role" });

            return Ok(new ApiResponse<string> { Success = true, Message = "Admin role revoked" });
        }
    }

    public class UserWithRolesDto
    {
        public string Id { get; set; } = default!;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }
}
