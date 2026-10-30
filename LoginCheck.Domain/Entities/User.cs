namespace LoginCheck.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Specialties { get; set; } = string.Empty;
    public string EducationQualification { get; set; } = string.Empty;
    public string VocationalTraining { get; set; } = string.Empty;
    public string PermanentAddress { get; set; } = string.Empty;
    public string CurrentAddress { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;
    public string PastExperience { get; set; } = string.Empty;
    public string AvatarKey { get; set; } = "avatar-1";
    public string Role { get; set; } = "Customer";
    public string PasswordHash { get; set; } = string.Empty;
}
