namespace Pearline.Application.DTOs.User
{
    public class ChangeEmailDto
    {
        public string CurrentPassword { get; set; }
        public string NewEmail { get; set; }
    }
}
