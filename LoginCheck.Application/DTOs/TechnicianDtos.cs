using System.ComponentModel.DataAnnotations;

namespace LoginCheck.Application.DTOs;

public sealed class CreateTechnicianDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string Specialties { get; set; } = string.Empty;

    [StringLength(200)] public string EducationQualification { get; set; } = string.Empty;
    [StringLength(500)] public string VocationalTraining { get; set; } = string.Empty;
    [StringLength(1000)] public string PermanentAddress { get; set; } = string.Empty;
    [Required, StringLength(1000)] public string CurrentAddress { get; set; } = string.Empty;
    [StringLength(10)] public string BloodGroup { get; set; } = string.Empty;
    [StringLength(2000)] public string PastExperience { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public sealed class UpdateTechnicianDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string Specialties { get; set; } = string.Empty;

    [StringLength(200)] public string EducationQualification { get; set; } = string.Empty;
    [StringLength(500)] public string VocationalTraining { get; set; } = string.Empty;
    [StringLength(1000)] public string PermanentAddress { get; set; } = string.Empty;
    [Required, StringLength(1000)] public string CurrentAddress { get; set; } = string.Empty;
    [StringLength(10)] public string BloodGroup { get; set; } = string.Empty;
    [StringLength(2000)] public string PastExperience { get; set; } = string.Empty;

    [MinLength(8)]
    public string? Password { get; set; }
}

public sealed class TechnicianResponseDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Specialties { get; init; } = string.Empty;
    public string EducationQualification { get; init; } = string.Empty;
    public string VocationalTraining { get; init; } = string.Empty;
    public string PermanentAddress { get; init; } = string.Empty;
    public string CurrentAddress { get; init; } = string.Empty;
    public string BloodGroup { get; init; } = string.Empty;
    public string PastExperience { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}
