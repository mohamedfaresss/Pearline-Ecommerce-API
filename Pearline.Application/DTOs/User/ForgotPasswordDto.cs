using System.ComponentModel.DataAnnotations;

namespace Pearline.Application.DTOs.User
{
    public class ForgotPasswordDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
