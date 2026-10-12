using System.Text;
using AcxiomCRM.Infrastructure.Data;
using AcxiomCRM.Infrastructure.Services.Implementations;
using AcxiomCRM.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace AcxiomCRM.Tests.UnitTests;

public class CsvSanitizationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly ReportService _service;

    public CsvSanitizationTests()
    {
        (_context, _connection) = TestDbContextFactory.CreateInMemoryDbContext();
        var currentUser = MockCurrentUserService.CreateAdmin();
        var userManagerMock = new Mock<UserManager<AcxiomCRM.Core.Entities.ApplicationUser>>(
            Mock.Of<IUserStore<AcxiomCRM.Core.Entities.ApplicationUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);

        _service = new ReportService(_context, currentUser, userManagerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Theory]
    [InlineData("=cmd|'/C calc'!A0", "\"'=cmd|'/C calc'!A0\"")]
    [InlineData("+123456789", "\"'+123456789\"")]
    [InlineData("-2+3*cmd", "\"'-2+3*cmd\"")]
    [InlineData("@SUM(A1:A10)", "\"'@SUM(A1:A10)\"")]
    public void ExportToCsv_FormulaInjectionCharacters_AreSafelyEscaped(string maliciousInput, string expectedEscaped)
    {
        var data = new[] { new { MaliciousText = maliciousInput } };
        var headers = new[] { "Input" };

        var csvBytes = _service.ExportToCsv(data, d => new[] { d.MaliciousText }, headers);
        var csvString = Encoding.UTF8.GetString(csvBytes);

        Assert.Contains(expectedEscaped, csvString);
    }

    [Fact]
    public void ExportToCsv_QuotesInText_AreProperlyDoubled()
    {
        var data = new[] { new { Text = "Acme \"Special\" Goods" } };
        var headers = new[] { "Text" };

        var csvBytes = _service.ExportToCsv(data, d => new[] { d.Text }, headers);
        var csvString = Encoding.UTF8.GetString(csvBytes);

        Assert.Contains("\"Acme \"\"Special\"\" Goods\"", csvString);
    }
}
