using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OutdoorsShop.Api;
using OutdoorsShop.Api.Controllers;
using OutdoorsShop.Core.DTOs.Auth;
using OutdoorsShop.Core.DTOs.Common;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Identity;
using System.Security.Claims;

namespace OutdoorsShop.Api.Tests.Controllers;

public class AuthControllerTests
{
    private static Mock<UserManager<ApplicationUser>> BuildUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<ApplicationUser>>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<ApplicationUser>>>());
    }

    private static Mock<SignInManager<ApplicationUser>> BuildSignInManagerMock(
        Mock<UserManager<ApplicationUser>> userManagerMock)
    {
        return new Mock<SignInManager<ApplicationUser>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
            null!,
            null!);
    }

    private static JwtSettings MakeJwtSettings() => new()
    {
        Secret = "TestSecretKeyThatIsLongEnoughForHmacSha256Algorithm!",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        AccessTokenExpirationMinutes = 15,
        RefreshTokenExpirationDays = 7
    };

    private static ApplicationUser MakeUser(string id = "user-1", string email = "test@test.com") =>
        new() { Id = id, UserName = email, Email = email };

    [Fact]
    public async Task Register_ReturnsOkWithToken_WhenValidRequest()
    {
        var userMgr = BuildUserManagerMock();
        var signInMgr = BuildSignInManagerMock(userMgr);
        var customerRepo = new Mock<ICustomerRepository>();

        var user = MakeUser();
        userMgr.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userMgr.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"))
            .ReturnsAsync(IdentityResult.Success);
        userMgr.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(["Customer"]);
        userMgr.Setup(m => m.SetAuthenticationTokenAsync(
            It.IsAny<ApplicationUser>(), "OutdoorsShop", "RefreshTokenHash", It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        customerRepo.Setup(r => r.AddAsync(It.IsAny<Customer>())).Returns(Task.CompletedTask);
        customerRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        customerRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new Customer { CustomerID = 1, UserId = "user-1", Name = "Test", Email = "test@test.com" });

        var controller = new AuthController(
            userMgr.Object,
            signInMgr.Object,
            customerRepo.Object,
            Mock.Of<ICustomerService>(),
            Options.Create(MakeJwtSettings()))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var dto = new RegisterDto
        {
            Name = "Test User",
            Email = "test@test.com",
            Password = "Test1234!",
            ConfirmPassword = "Test1234!"
        };

        var result = await controller.Register(dto);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<TokenDto>()
            .Which.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_Returns400_WhenIdentityCreateFails()
    {
        var userMgr = BuildUserManagerMock();
        var signInMgr = BuildSignInManagerMock(userMgr);
        var customerRepo = new Mock<ICustomerRepository>();

        userMgr.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail", Description = "Email already taken." }));

        var controller = new AuthController(
            userMgr.Object,
            signInMgr.Object,
            customerRepo.Object,
            Mock.Of<ICustomerService>(),
            Options.Create(MakeJwtSettings()))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var dto = new RegisterDto
        {
            Name = "Test",
            Email = "existing@test.com",
            Password = "Test1234!",
            ConfirmPassword = "Test1234!"
        };

        var result = await controller.Register(dto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Login_ReturnsOkWithToken_WhenValidCredentials()
    {
        var userMgr = BuildUserManagerMock();
        var signInMgr = BuildSignInManagerMock(userMgr);
        var customerRepo = new Mock<ICustomerRepository>();

        var user = MakeUser();
        userMgr.Setup(m => m.FindByEmailAsync("valid@test.com")).ReturnsAsync(user);
        signInMgr.Setup(m => m.CheckPasswordSignInAsync(user, "Correct1!", false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        userMgr.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Customer"]);
        userMgr.Setup(m => m.SetAuthenticationTokenAsync(
            user, "OutdoorsShop", "RefreshTokenHash", It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        customerRepo.Setup(r => r.GetByUserIdAsync(user.Id))
            .ReturnsAsync(new Customer { CustomerID = 5, UserId = user.Id, Name = "Test", Email = user.Email! });

        var controller = new AuthController(
            userMgr.Object,
            signInMgr.Object,
            customerRepo.Object,
            Mock.Of<ICustomerService>(),
            Options.Create(MakeJwtSettings()))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Login(new LoginDto { Email = "valid@test.com", Password = "Correct1!" });

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<TokenDto>()
            .Which.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_Returns401_WhenUserNotFound()
    {
        var userMgr = BuildUserManagerMock();
        var signInMgr = BuildSignInManagerMock(userMgr);
        userMgr.Setup(m => m.FindByEmailAsync("nobody@test.com")).ReturnsAsync((ApplicationUser?)null);

        var controller = new AuthController(
            userMgr.Object,
            signInMgr.Object,
            Mock.Of<ICustomerRepository>(),
            Mock.Of<ICustomerService>(),
            Options.Create(MakeJwtSettings()))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Login(new LoginDto { Email = "nobody@test.com", Password = "Any1234!" });

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Login_Returns401_WhenInvalidPassword()
    {
        var userMgr = BuildUserManagerMock();
        var signInMgr = BuildSignInManagerMock(userMgr);
        var user = MakeUser(email: "user@test.com");

        userMgr.Setup(m => m.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
        signInMgr.Setup(m => m.CheckPasswordSignInAsync(user, "WrongPass1!", false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var controller = new AuthController(
            userMgr.Object,
            signInMgr.Object,
            Mock.Of<ICustomerRepository>(),
            Mock.Of<ICustomerService>(),
            Options.Create(MakeJwtSettings()))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Login(new LoginDto { Email = "user@test.com", Password = "WrongPass1!" });

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Refresh_Returns401_WhenNoCookiePresent()
    {
        var userMgr = BuildUserManagerMock();
        var signInMgr = BuildSignInManagerMock(userMgr);

        // No refresh token cookie — _userManager.Users will be empty
        userMgr.Setup(m => m.Users).Returns(Enumerable.Empty<ApplicationUser>().AsQueryable());

        var controller = new AuthController(
            userMgr.Object,
            signInMgr.Object,
            Mock.Of<ICustomerRepository>(),
            Mock.Of<ICustomerService>(),
            Options.Create(MakeJwtSettings()))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Refresh();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task ChangePassword_ReturnsOk_WhenServiceSucceeds()
    {
        var userMgr = BuildUserManagerMock();
        var signInMgr = BuildSignInManagerMock(userMgr);
        var customerService = new Mock<ICustomerService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "user-42")
        ], "Test"));

        var dto = new ChangePasswordDto
        {
            CurrentPassword = "Current123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        customerService
            .Setup(s => s.ChangePasswordAsync("user-42", dto))
            .ReturnsAsync(OperationResult.Success());

        var controller = new AuthController(
            userMgr.Object,
            signInMgr.Object,
            Mock.Of<ICustomerRepository>(),
            customerService.Object,
            Options.Create(MakeJwtSettings()))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };

        var result = await controller.ChangePassword(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_Returns400_WhenCurrentPasswordIsWrong()
    {
        var userMgr = BuildUserManagerMock();
        var signInMgr = BuildSignInManagerMock(userMgr);
        var customerService = new Mock<ICustomerService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "user-42")
        ], "Test"));

        var dto = new ChangePasswordDto
        {
            CurrentPassword = "Wrong123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        customerService
            .Setup(s => s.ChangePasswordAsync("user-42", dto))
            .ReturnsAsync(OperationResult.Invalid("Current password is incorrect."));

        var controller = new AuthController(
            userMgr.Object,
            signInMgr.Object,
            Mock.Of<ICustomerRepository>(),
            customerService.Object,
            Options.Create(MakeJwtSettings()))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };

        var result = await controller.ChangePassword(dto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Me_ReturnsProfile_WhenAuthenticated()
    {
        var userMgr = BuildUserManagerMock();
        var signInMgr = BuildSignInManagerMock(userMgr);
        var customerRepo = new Mock<ICustomerRepository>();

        var user = MakeUser(id: "user-42", email: "me@test.com");
        userMgr.Setup(m => m.FindByIdAsync("user-42")).ReturnsAsync(user);
        userMgr.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Customer"]);
        customerRepo.Setup(r => r.GetByUserIdAsync("user-42"))
            .ReturnsAsync(new Customer { CustomerID = 42, UserId = "user-42", Name = "Me User", Email = "me@test.com" });

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "user-42"),
            new Claim(ClaimTypes.Role, "Customer")
        ], "Test"));

        var controller = new AuthController(
            userMgr.Object,
            signInMgr.Object,
            customerRepo.Object,
            Mock.Of<ICustomerService>(),
            Options.Create(MakeJwtSettings()))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };

        var result = await controller.Me();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var profile = ok.Value.Should().BeOfType<UserProfileDto>().Subject;
        profile.CustomerID.Should().Be(42);
    }
}
