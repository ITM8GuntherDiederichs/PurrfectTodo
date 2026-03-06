using Microsoft.Playwright;

namespace PurrfectTodo.E2E.Pages;

/// <summary>
/// Page Object Model for /Account/Login.
/// </summary>
public class LoginPage(IPage page)
{
    public const string Path = "/Account/Login";

    // ── Locators ─────────────────────────────────────────────────────────────

    public ILocator EmailInput      => page.Locator("#Input\\.Email");
    public ILocator PasswordInput   => page.Locator("#Input\\.Password");
    public ILocator SubmitButton    => page.Locator("button.fw-auth-btn");

    /// <summary>StatusMessage rendered by the Identity Razor component (error text).</summary>
    public ILocator StatusMessage   => page.Locator(".text-danger");

    /// <summary>ValidationSummary messages when required fields are left empty.</summary>
    public ILocator ValidationSummary => page.Locator("[role='alert']");

    // ── Actions ───────────────────────────────────────────────────────────────

    public async Task GotoAsync(string baseUrl)
    {
        await page.GotoAsync($"{baseUrl}{Path}");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task FillAsync(string email, string password)
    {
        await EmailInput.FillAsync(email);
        await PasswordInput.FillAsync(password);
    }

    public async Task SubmitAsync()
    {
        await SubmitButton.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task LoginAsync(string email, string password)
    {
        await FillAsync(email, password);
        await SubmitAsync();
    }
}
