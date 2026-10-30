using System.ComponentModel.DataAnnotations;

namespace LoginCheck.Application.DTOs;

public class RegisterRequestDto
{
    private string _userName = string.Empty;

    [Required, StringLength(100)]
    public string UserName
    {
        get => !string.IsNullOrWhiteSpace(_userName) ? _userName : Name;
        set => _userName = value;
    }

    public string Name { get; set; } = string.Empty;
    [EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;
    [Required, RegularExpression("^\\d{10}$", ErrorMessage = "Enter a 10-digit contact number.")]
    public string Phone { get; set; } = string.Empty;
    [Required, MinLength(8), StringLength(1000)]
    public string Address { get; set; } = string.Empty;
    [Required, MinLength(8), StringLength(100)]
    public string Password { get; set; } = string.Empty;
    [StringLength(32)] public string AvatarKey { get; set; } = "avatar-1";
}
