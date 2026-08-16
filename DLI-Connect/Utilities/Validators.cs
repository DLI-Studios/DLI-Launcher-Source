namespace DLI.Connect.Utilities;

public static class Validators
{
    public static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) &&
        email.Contains('@') &&
        email.IndexOf('@') > 0 &&
        email.LastIndexOf('.') > email.IndexOf('@') + 1;

    public static bool IsValidUsername(string username) =>
        !string.IsNullOrWhiteSpace(username) &&
        username.Length is >= 3 and <= 20 &&
        username.All(c => char.IsLetterOrDigit(c) || c == '_');

    public static bool IsValidPassword(string password) =>
        password.Length >= 6;

    public static string ToTurkishErrorMessage(string firebaseCode) => firebaseCode switch
    {
        "EMAIL_EXISTS" => "Bu e-posta adresi zaten kayıtlı.",
        "INVALID_EMAIL" => "Geçersiz e-posta adresi.",
        "WEAK_PASSWORD" => "Şifre çok zayıf. En az 6 karakter olmalı.",
        "EMAIL_NOT_FOUND" => "Bu e-posta ile kayıtlı hesap bulunamadı.",
        "INVALID_PASSWORD" => "Şifre hatalı.",
        "INVALID_LOGIN_CREDENTIALS" => "E-posta veya şifre hatalı.",
        "USER_DISABLED" => "Bu hesap devre dışı bırakıldı.",
        "TOO_MANY_ATTEMPTS_TRY_LATER" => "Çok fazla deneme yapıldı. Lütfen biraz sonra tekrar dene.",
        "NOT_FOUND" => "Kullanıcı profili bulunamadı.",
        "PERMISSION_DENIED" => "Bu işlem için yetkin yok.",
        "UNAUTHENTICATED" => "Oturumun geçerliliği sona erdi. Tekrar giriş yap.",
        "INVALID_ARGUMENT" => "Geçersiz istek gönderildi.",
        "FAILED_PRECONDITION" => "İşlem yapılamadı. Birazdan tekrar dene.",
        "RESOURCE_EXHAUSTED" => "Sunucu yoğun. Biraz sonra tekrar dene.",
        "NETWORK_ERROR" => "Ağ bağlantısı kurulamadı.",
        _ => "Bir hata oluştu. Lütfen tekrar dene."
    };
}
