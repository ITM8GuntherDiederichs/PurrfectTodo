using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using PurrfectTodo.E2E.Pages;

namespace PurrfectTodo.E2E;

/// <summary>
/// End-to-end login / logout tests for PurrfectTodo.
/// All tests run against the live app at http://localhost:5059.
/// The <see cref="AppFixture"/> SetUpFixture ensures the app is up before
/// this suite runs.
/// </summary>
[Category("E2E")]
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class LoginTests : PageTest
{
    // ── Constants ─────────────────────────────────────────────────────────────

    private const string BaseUrl       = "http://localhost:5059";
    private const string AdminEmail    = "test@purrfecttodo.local";
    private const string AdminPassword = "Test1234!";
    private const string WrongPassword = "WrongPass1!";

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Opens the login page, asserts we are on /Account/Login, and returns
    /// a LoginPage POM instance ready for interaction.
    /// </summary>
    private async Task<LoginPage> OpenLoginPageAsync()
    {
        var loginPage = new LoginPage(Page);
        await loginPage.GotoAsync(BaseUrl);
        Assert.That(Page.Url, Does.Contain("Account/Login"),
            "Navigating to login page should land on /Account/Login");
        return loginPage;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Test 1 — Navigating to / while unauthenticated redirects to the login page.
    /// Then submitting valid credentials lands the user on the dashboard (/).
    /// </summary>
    [Test]
    public async Task Login_WithValidCredentials_RedirectsToDashboard()
    {
        // Arrange – navigate to root; expect redirect to login
        await Page.GotoAsync(BaseUrl + "/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.That(Page.Url, Does.Contain("Account/Login"),
            "Unauthenticated request to / should redirect to login");

        var loginPage = new LoginPage(Page);

        // Act – sign in with valid credentials
        await loginPage.LoginAsync(AdminEmail, AdminPassword);

        // Assert – landed on dashboard (/)
        var finalUrl = Page.Url;
        Assert.That(
            finalUrl.TrimEnd('/').EndsWith(BaseUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)
            || finalUrl.Contains("/home", StringComparison.OrdinalIgnoreCase),
            Is.True,
            $"After valid login expected to be at / or /home but was: {finalUrl}");

        // Dashboard content should be visible
        await Expect(Page.GetByText("Welcome back")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Test 2 — Submitting a valid email with a wrong password shows the
    /// "Invalid login attempt" error message.
    /// </summary>
    [Test]
    public async Task Login_WithWrongPassword_ShowsError()
    {
        // Arrange
        var loginPage = await OpenLoginPageAsync();

        // Act – enter correct email but wrong password
        await loginPage.LoginAsync(AdminEmail, WrongPassword);

        // Assert – error banner visible
        await Expect(
            Page.GetByText("Invalid login attempt", new PageGetByTextOptions { Exact = false })
        ).ToBeVisibleAsync();
    }

    /// <summary>
    /// Test 3 — Clicking Login without entering any credentials shows
    /// client-side / server-side validation messages.
    /// </summary>
    [Test]
    public async Task Login_WithEmptyFields_ShowsValidation()
    {
        // Arrange
        var loginPage = await OpenLoginPageAsync();

        // Act – click submit without filling anything
        await loginPage.SubmitAsync();

        // Assert – at least one validation/error message must be present.
        // The ValidationSummary has role="alert"; individual ValidationMessage
        // elements carry class="text-danger".
        var errorMessages = Page.Locator(".text-danger, [role='alert']");
        await Expect(errorMessages.First).ToBeVisibleAsync();
    }

    /// <summary>
    /// Test 4 — After a successful login the user can click Logout and is
    /// redirected back to the login page.
    /// </summary>
    [Test]
    public async Task Login_ThenLogout_RedirectsToLogin()
    {
        // Arrange – log in first
        var loginPage = await OpenLoginPageAsync();
        await loginPage.LoginAsync(AdminEmail, AdminPassword);

        // Confirm we are on the dashboard
        await Expect(Page.GetByText("Welcome back")).ToBeVisibleAsync();

        // Act – submit the logout form (button text "Logout" inside a <form>)
        await Page.Locator("button.pt-logout-btn").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert – back on login page
        Assert.That(Page.Url, Does.Contain("Account/Login"),
            "After logout the user should be redirected to the login page");
    }
}
