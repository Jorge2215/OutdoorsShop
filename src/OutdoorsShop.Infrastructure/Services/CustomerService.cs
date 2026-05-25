using Microsoft.AspNetCore.Identity;
using OutdoorsShop.Core.DTOs.Auth;
using OutdoorsShop.Core.DTOs.Common;
using OutdoorsShop.Core.DTOs.Customers;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Identity;

namespace OutdoorsShop.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomerService(ICustomerRepository customerRepository, UserManager<ApplicationUser> userManager)
    {
        _customerRepository = customerRepository;
        _userManager = userManager;
    }

    public async Task<PagedResult<CustomerDto>> GetPagedAsync(int pageNumber, int pageSize)
    {
        var normalizedPageNumber = Math.Max(1, pageNumber);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var (items, totalCount) = await _customerRepository.GetPagedAsync(normalizedPageNumber, normalizedPageSize);

        return new PagedResult<CustomerDto>
        {
            Items = items.Select(MapToDto).ToList(),
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize,
            TotalCount = totalCount
        };
    }

    public async Task<OperationResult<CustomerDto>> GetByIdAsync(int id, bool isAdministrator, int? currentCustomerId)
    {
        if (!isAdministrator && currentCustomerId != id)
            return OperationResult<CustomerDto>.ForbiddenResult("You can only access your own customer profile.");

        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer is null)
            return OperationResult<CustomerDto>.NotFoundResult($"Customer {id} not found.");

        return OperationResult<CustomerDto>.Success(MapToDto(customer));
    }

    public async Task<OperationResult<CustomerDto>> UpdateAsync(int id, UpdateCustomerDto request, bool isAdministrator, int? currentCustomerId)
    {
        if (!isAdministrator && currentCustomerId != id)
            return OperationResult<CustomerDto>.ForbiddenResult("You can only update your own customer profile.");

        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer is null)
            return OperationResult<CustomerDto>.NotFoundResult($"Customer {id} not found.");

        customer.FirstName = request.FirstName.Trim();
        customer.LastName = request.LastName.Trim();
        customer.Name = string.Join(' ', new[] { customer.FirstName, customer.LastName }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
        customer.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        customer.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();

        await _customerRepository.UpdateAsync(customer);
        await _customerRepository.SaveChangesAsync();

        return OperationResult<CustomerDto>.Success(MapToDto(customer));
    }

    public async Task<OperationResult> ChangePasswordAsync(string userId, ChangePasswordDto request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return OperationResult.NotFoundResult("Authenticated user was not found.");

        var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
        if (!isCurrentPasswordValid)
            return OperationResult.Invalid("Current password is incorrect.");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return OperationResult.Invalid(string.Join(" ", result.Errors.Select(error => error.Description)));

        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(int id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer is null)
            return OperationResult.NotFoundResult($"Customer {id} not found.");

        customer.IsActive = false;
        await _customerRepository.UpdateAsync(customer);
        await _customerRepository.SaveChangesAsync();

        return OperationResult.Success();
    }

    private static CustomerDto MapToDto(Customer customer)
    {
        var (firstName, lastName) = ResolveName(customer);

        return new CustomerDto
        {
            CustomerID = customer.CustomerID,
            UserId = customer.UserId,
            Email = customer.Email,
            FirstName = firstName,
            LastName = lastName,
            Phone = customer.Phone,
            Address = customer.Address,
            IsActive = customer.IsActive
        };
    }

    private static (string FirstName, string LastName) ResolveName(Customer customer)
    {
        if (!string.IsNullOrWhiteSpace(customer.FirstName) || !string.IsNullOrWhiteSpace(customer.LastName))
            return (customer.FirstName ?? string.Empty, customer.LastName ?? string.Empty);

        if (string.IsNullOrWhiteSpace(customer.Name))
            return (string.Empty, string.Empty);

        var parts = customer.Name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            0 => (string.Empty, string.Empty),
            1 => (parts[0], string.Empty),
            _ => (parts[0], parts[1])
        };
    }
}
