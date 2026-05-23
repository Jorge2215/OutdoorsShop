namespace OutdoorsShop.Core.DTOs.Auth;

public class UserProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? CustomerID { get; set; }
    public List<string> Roles { get; set; } = [];
}
