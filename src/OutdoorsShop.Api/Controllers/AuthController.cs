using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OutdoorsShop.Core.DTOs.Auth;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace OutdoorsShop.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ICustomerRepository _customerRepository;
    private readonly JwtSettings _jwtSettings;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ICustomerRepository customerRepository,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _customerRepository = customerRepository;
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>Register a new customer account.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _userManager.AddToRoleAsync(user, "Customer");

        var customer = new Core.Entities.Customer
        {
            UserId = user.Id,
            Name = dto.Name,
            Email = dto.Email
        };
        await _customerRepository.AddAsync(customer);
        await _customerRepository.SaveChangesAsync();

        var token = await GenerateTokenAsync(user);
        SetRefreshTokenCookie(token.RefreshToken, _jwtSettings.RefreshTokenExpirationDays);
        return Ok(token);
    }

    /// <summary>Login with email and password.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return Unauthorized();

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Unauthorized();

        var token = await GenerateTokenAsync(user);
        SetRefreshTokenCookie(token.RefreshToken, _jwtSettings.RefreshTokenExpirationDays);
        return Ok(token);
    }

    /// <summary>Refresh access token using the HttpOnly refresh token cookie.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized();

        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        // Load users into memory first, then match the refresh token hash asynchronously
        var users = _userManager.Users.ToList();
        ApplicationUser? matchedUser = null;
        foreach (var u in users)
        {
            var stored = await _userManager.GetAuthenticationTokenAsync(u, "OutdoorsShop", "RefreshTokenHash");
            if (stored == tokenHash)
            {
                matchedUser = u;
                break;
            }
        }

        if (matchedUser is null)
            return Unauthorized();

        var token = await GenerateTokenAsync(matchedUser);
        SetRefreshTokenCookie(token.RefreshToken, _jwtSettings.RefreshTokenExpirationDays);
        return Ok(token);
    }

    /// <summary>Logout — revokes the refresh token and clears the cookie.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (userId is not null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is not null)
                await _userManager.RemoveAuthenticationTokenAsync(user, "OutdoorsShop", "RefreshTokenHash");
        }

        // Expire the cookie immediately
        Response.Cookies.Append("refreshToken", string.Empty, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(-1)
        });

        return NoContent();
    }

    /// <summary>Returns the authenticated user's profile.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var customer = await _customerRepository.GetByUserIdAsync(userId);

        return Ok(new UserProfileDto
        {
            UserId = userId,
            Email = user.Email!,
            Name = customer?.Name ?? user.UserName ?? string.Empty,
            CustomerID = customer?.CustomerID,
            Roles = [.. roles]
        });
    }

    private async Task<TokenDto> GenerateTokenAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var customer = await _customerRepository.GetByUserIdAsync(user.Id);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.GivenName, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (customer is not null)
            claims.Add(new Claim("customer_id", customer.CustomerID.ToString()));

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // Generate and store refresh token hash
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        await _userManager.SetAuthenticationTokenAsync(user, "OutdoorsShop", "RefreshTokenHash", refreshTokenHash);

        return new TokenDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };
    }

    private void SetRefreshTokenCookie(string refreshToken, int expirationDays)
    {
        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(expirationDays)
        });
    }
}
