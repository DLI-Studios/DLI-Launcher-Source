using DLI.Connect.Utilities;
using Xunit;

namespace DLI.Connect.Tests;

public class ValidatorsTests
{
    [Theory]
    [InlineData("ahmet@ornek.com", true)]
    [InlineData("a.b@sub.ornek.com", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("ahmet@", false)]
    [InlineData("@ornek.com", false)]
    [InlineData("ahmet@ornek", false)]
    [InlineData("ahmetornek.com", false)]
    [InlineData("a@b.c", true)]
    public void IsValidEmail_CorrectlyValidates(string email, bool expected)
    {
        Assert.Equal(expected, Validators.IsValidEmail(email));
    }

    [Theory]
    [InlineData("ahmet_42", true)]
    [InlineData("abc", true)]
    [InlineData("a", false)]
    [InlineData("ab", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("has space", false)]
    [InlineData("has-dash", false)]
    [InlineData("Türkçe", true)]
    [InlineData("xxxxxxxxxxxxxxxxxxxxx", false)]
    public void IsValidUsername_CorrectlyValidates(string username, bool expected)
    {
        Assert.Equal(expected, Validators.IsValidUsername(username));
    }

    [Theory]
    [InlineData("123456", true)]
    [InlineData("abc123", true)]
    [InlineData("", false)]
    [InlineData("12345", false)]
    public void IsValidPassword_CorrectlyValidates(string password, bool expected)
    {
        Assert.Equal(expected, Validators.IsValidPassword(password));
    }

    [Theory]
    [InlineData("EMAIL_EXISTS", "Bu e-posta adresi zaten kayıtlı.")]
    [InlineData("INVALID_PASSWORD", "Şifre hatalı.")]
    [InlineData("NETWORK_ERROR", "Ağ bağlantısı kurulamadı.")]
    [InlineData("BILINMEYEN_KOD", "Bir hata oluştu. Lütfen tekrar dene.")]
    [InlineData("", "Bir hata oluştu. Lütfen tekrar dene.")]
    public void ToTurkishErrorMessage_MapsKnownAndUnknownCodes(string code, string expected)
    {
        Assert.Equal(expected, Validators.ToTurkishErrorMessage(code));
    }
}
