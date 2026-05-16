using System.Security.Claims;
using ECommerce.Api.Controllers;
using ECommerce.Application.Admin;
using ECommerce.Application.Admin.DTOs;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders.Interfaces;
using ECommerce.Application.Products.Interfaces;
using ECommerce.Application.Products.DTOs;
using ECommerce.Application.Users.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using FluentAssertions;

namespace ECommerce.Api.Tests.Controllers;

public class AdminAuthorizationTests
{
    [Fact]
    public void AdminController_HasAuthorizeRoleAttribute()
    {
        var attribute = typeof(AdminController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .FirstOrDefault();

        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Be("Admin");
    }

    [Fact]
    public void ProductsController_Create_HasAuthorizeAdminSellerAttribute()
    {
        var method = typeof(ECommerce.Api.Controllers.ProductsController).GetMethod("Create");
        var attribute = method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .FirstOrDefault();

        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Be("Admin,Seller");
    }

    [Fact]
    public void ProductsController_Update_HasAuthorizeAdminSellerAttribute()
    {
        var method = typeof(ECommerce.Api.Controllers.ProductsController).GetMethod("Update");
        var attribute = method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .FirstOrDefault();

        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Be("Admin,Seller");
    }

    [Fact]
    public void ProductsController_Delete_HasAuthorizeAdminSellerAttribute()
    {
        var method = typeof(ECommerce.Api.Controllers.ProductsController).GetMethod("Delete");
        var attribute = method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .FirstOrDefault();

        attribute.Should().NotBeNull();
        attribute!.Roles.Should().Be("Admin,Seller");
    }

    [Fact]
    public async Task AdminDashboard_WithAdminClaims_ReturnsOk()
    {
        var mockOrderService = new Mock<IOrderService>();
        var mockProductService = new Mock<IProductService>();
        var mockUserService = new Mock<IUserService>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);

        var controller = new AdminController(
            mockOrderService.Object,
            mockProductService.Object,
            mockUserService.Object,
            context
        );

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, "admin@test.com"),
            new(ClaimTypes.Role, "Admin")
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims)) }
        };

        var result = await controller.GetDashboardSummary(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
