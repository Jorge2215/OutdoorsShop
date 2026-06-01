using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OutdoorsShop.Api.Controllers;
using OutdoorsShop.Core.DTOs.Common;
using OutdoorsShop.Core.DTOs.Customers;
using OutdoorsShop.Core.Interfaces;
using System.Security.Claims;

namespace OutdoorsShop.Api.Tests.Controllers;

public class CustomersControllerTests
{
    private readonly Mock<ICustomerService> _customerService = new();

    private CustomersController CreateController(string role, int customerId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-1"),
            new(ClaimTypes.Role, role),
            new("customer_id", customerId.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        return new CustomersController(_customerService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    private static CustomerDto MakeCustomerDto(int id) => new()
    {
        CustomerID = id,
        UserId = "user-1",
        Email = $"customer{id}@test.com",
        FirstName = "Jane",
        LastName = "Doe",
        IsActive = true
    };

    [Fact]
    public async Task GetById_ReturnsOwnProfile_ForCustomerRole()
    {
        var dto = MakeCustomerDto(42);
        _customerService
            .Setup(s => s.GetByIdAsync(42, false, 42))
            .ReturnsAsync(OperationResult<CustomerDto>.Success(dto));

        var result = await CreateController("Customer", customerId: 42).GetById(42);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<CustomerDto>()
            .Which.CustomerID.Should().Be(42);
    }

    [Fact]
    public async Task GetById_Returns403_WhenCustomerAccessesOtherProfile()
    {
        _customerService
            .Setup(s => s.GetByIdAsync(99, false, 42))
            .ReturnsAsync(OperationResult<CustomerDto>.ForbiddenResult());

        var result = await CreateController("Customer", customerId: 42).GetById(99);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetById_ReturnsAnyProfile_ForAdminRole()
    {
        var dto = MakeCustomerDto(99);
        _customerService
            .Setup(s => s.GetByIdAsync(99, true, 1))
            .ReturnsAsync(OperationResult<CustomerDto>.Success(dto));

        var result = await CreateController("Administrator", customerId: 1).GetById(99);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<CustomerDto>()
            .Which.CustomerID.Should().Be(99);
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotFound()
    {
        _customerService
            .Setup(s => s.GetByIdAsync(999, true, 1))
            .ReturnsAsync(OperationResult<CustomerDto>.NotFoundResult("Customer not found."));

        var result = await CreateController("Administrator", customerId: 1).GetById(999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_UpdatesOwnData_ForCustomerRole()
    {
        var request = new UpdateCustomerDto { FirstName = "John", LastName = "Smith" };
        var dto = MakeCustomerDto(42);
        _customerService
            .Setup(s => s.UpdateAsync(42, request, false, 42))
            .ReturnsAsync(OperationResult<CustomerDto>.Success(dto));

        var result = await CreateController("Customer", customerId: 42).Update(42, request);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_Returns403_WhenCustomerUpdatesOtherProfile()
    {
        var request = new UpdateCustomerDto { FirstName = "Hacker", LastName = "McHack" };
        _customerService
            .Setup(s => s.UpdateAsync(99, request, false, 42))
            .ReturnsAsync(OperationResult<CustomerDto>.ForbiddenResult());

        var result = await CreateController("Customer", customerId: 42).Update(99, request);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Update_Returns404_WhenCustomerNotFound()
    {
        var request = new UpdateCustomerDto { FirstName = "X", LastName = "Y" };
        _customerService
            .Setup(s => s.UpdateAsync(999, request, true, 1))
            .ReturnsAsync(OperationResult<CustomerDto>.NotFoundResult("Not found."));

        var result = await CreateController("Administrator", customerId: 1).Update(999, request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UploadAvatar_ReturnsBadRequest_WhenFileMissing()
    {
        var result = await CreateController("Customer", customerId: 42).UploadAvatar(42, null!);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UploadAvatar_ReturnsBadRequest_WhenFileTypeIsInvalid()
    {
        var file = new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "file", "avatar.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var result = await CreateController("Customer", customerId: 42).UploadAvatar(42, file);

        result.Should().BeOfType<BadRequestObjectResult>();
        _customerService.Verify(
            s => s.UploadAvatarAsync(It.IsAny<int>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int?>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadAvatar_ReturnsBadRequest_WhenFileIsTooLarge()
    {
        var bytes = new byte[(2 * 1024 * 1024) + 1];
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "avatar.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var result = await CreateController("Customer", customerId: 42).UploadAvatar(42, file);

        result.Should().BeOfType<BadRequestObjectResult>();
        _customerService.Verify(
            s => s.UploadAvatarAsync(It.IsAny<int>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int?>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadAvatar_ReturnsUpdatedProfile_WhenRequestIsValid()
    {
        var dto = MakeCustomerDto(42);
        dto.AvatarPath = "customers/42/avatar.png";
        dto.AvatarContentType = "image/png";
        dto.AvatarUrl = "https://test.blob.core.windows.net/customer-avatars/customers/42/avatar.png";

        var file = new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "file", "avatar.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        _customerService
            .Setup(s => s.UploadAvatarAsync(42, It.IsAny<Stream>(), "avatar.png", "image/png", false, 42))
            .ReturnsAsync(OperationResult<CustomerDto>.Success(dto));

        var result = await CreateController("Customer", customerId: 42).UploadAvatar(42, file);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task RemoveAvatar_ReturnsUpdatedProfile_WhenAvatarIsRemoved()
    {
        var dto = MakeCustomerDto(42);

        _customerService
            .Setup(s => s.RemoveAvatarAsync(42, false, 42))
            .ReturnsAsync(OperationResult<CustomerDto>.Success(dto));

        var result = await CreateController("Customer", customerId: 42).RemoveAvatar(42);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(dto);
    }
}
