using Microsoft.AspNetCore.Authorization;
using Server.Controllers;

namespace Server.Tests;

public class AuthorizationTests
{
    [Theory]
    [InlineData(typeof(NguoiDungController))]
    [InlineData(typeof(BaoCaoController))]
    [InlineData(typeof(AdminController))]
    public void AdministrativeControllersRequireAdministrator(Type controller)
    {
        var requirement = Assert.Single(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal("QuanTriVien", requirement.Roles);
    }
}
