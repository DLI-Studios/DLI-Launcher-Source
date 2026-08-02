namespace DLI.Connect.DesignData;

public class DesignLoginViewModel
{
    public string Email => "ahmet@ornek.com";
    public string Password => "sifre123";
    public bool RememberMe => true;
    public string? ErrorMessage => null;
    public bool IsBusy => false;
    public bool IsIdle => true;
    public object? LoginCommand => null;
    public object? GoToForgotPasswordCommand => null;
    public object? GoToRegisterCommand => null;
}
