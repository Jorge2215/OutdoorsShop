using System.ComponentModel.DataAnnotations;

namespace OutdoorsShop.Core.DTOs.Customers;

public class CustomerDto
{
    public int CustomerID { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => string.Join(' ', new[] { FirstName, LastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? AvatarPath { get; set; }
    public string? AvatarContentType { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateCustomerDto
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }
}
