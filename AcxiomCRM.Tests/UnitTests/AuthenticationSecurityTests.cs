using System.Text.RegularExpressions;
using AcxiomCRM.Core.Entities;
using AcxiomCRM.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Tests.UnitTests;

public class AuthenticationSecurityTests
{
    private static readonly Regex PasswordRegex = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$");

    [Theory]
    [InlineData("Admin@12345!", true)]
    [InlineData("Manager#2026$", true)]
    [InlineData("Sales*Executive9", true)]
    [InlineData("weak", false)]                // Too short
    [InlineData("alllowercase123!", false)]    // No uppercase
    [InlineData("ALLUPPERCASE123!", false)]    // No lowercase
    [InlineData("NoDigitsHere!abc", false)]    // No numbers
    [InlineData("NoSpecialChars123", false)]   // No special characters
    public void PasswordPolicy_EnforcesComplexityRules(string password, bool shouldPass)
    {
        var isValid = PasswordRegex.IsMatch(password);
        Assert.Equal(shouldPass, isValid);
    }

    [Fact]
    public void ApplicationUser_InitialSecurityState_IsActiveAndUnlocked()
    {
        var user = new ApplicationUser
        {
            UserName = "test@domain.com",
            Email = "test@domain.com",
            FullName = "Test User",
            IsActive = true
        };

        Assert.True(user.IsActive);
        Assert.False(user.IsLockedOut);
        Assert.Equal(0, user.AccessFailedCount);
    }

    [Fact]
    public void ApplicationUser_Lockout_ReflectsStateCorrectly()
    {
        var user = new ApplicationUser
        {
            UserName = "locked@domain.com",
            Email = "locked@domain.com",
            FullName = "Locked User",
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15)
        };

        Assert.True(user.IsLockedOut);
    }
}
