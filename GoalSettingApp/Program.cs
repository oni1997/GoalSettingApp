using GoalSettingApp;
using GoalSettingApp.Components;
using GoalSettingApp.Services;
using GoalSettingApp.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Supabase Database Connection
var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
var key = Environment.GetEnvironmentVariable("SUPABASE_KEY");
var serviceKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY");
var options = new Supabase.SupabaseOptions
{
    AutoConnectRealtime = true
};
// Regular client for user-scoped operations (respects RLS)
var supabase = new Supabase.Client(url, key, options);
await supabase.InitializeAsync();

// Service role client for admin operations (bypasses RLS)
var supabaseAdmin = new Supabase.Client(url, serviceKey, options);
await supabaseAdmin.InitializeAsync();

// Register both clients in DI container
builder.Services.AddSingleton(supabase);
builder.Services.AddKeyedSingleton("admin", supabaseAdmin);

// Register HttpContextAccessor (needed for cookie auth in components)
builder.Services.AddHttpContextAccessor();

// Add authentication/authorization services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
    });
builder.Services.AddAuthorizationCore(); // Enables [Authorize] attribute
builder.Services.AddScoped<AuthenticationStateProvider, SupabaseAuthenticationStateProvider>(); // Registers custom provider
builder.Services.AddCascadingAuthenticationState(); // Makes auth state available everywhere in app

// Register HttpClient
builder.Services.AddHttpClient();

// Register GoalService as scoped
builder.Services.AddScoped<GoalService>();

// Register WeatherService
builder.Services.AddScoped<WeatherService>();

// Register UserSettingsService
builder.Services.AddScoped<UserSettingsService>();

// Email Settings - configure via environment variables
var emailSettings = new EmailSettings
{
    SmtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com",
    SmtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587,
    SmtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? "",
    SmtpPass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? "",
    FromEmail = Environment.GetEnvironmentVariable("FROM_EMAIL") ?? "",
    FromName = Environment.GetEnvironmentVariable("FROM_NAME") ?? "Goal Setting App"
};
builder.Services.AddSingleton(emailSettings);

// Register EmailService
builder.Services.AddScoped<EmailService>();

// Register UserInfoCache (singleton for caching user data)
builder.Services.AddSingleton<UserInfoCache>();

// Register Background Services for reminders
builder.Services.AddHostedService<TaskReminderService>();
builder.Services.AddHostedService<RecurringGoalService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Test email endpoints (development only)
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/test-email", async (EmailService emailService) =>
    {
        var result = await emailService.SendTaskReminderAsync(
            toEmail: "onesmusmaenza@gmail.com",
            toName: "Test User",
            taskTitle: "Test Task",
            taskDescription: "This is a test email to verify the email service is working correctly.",
            dueDate: DateTime.Now.AddDays(1),
            reminderType: "tomorrow"
        );

        return result
            ? Results.Ok(new { success = true, message = "Test email sent successfully!" })
            : Results.BadRequest(new { success = false, message = "Failed to send test email. Check logs for details." });
    });

    // Test daily reminder endpoint (single user - your email)
    app.MapGet("/api/test-daily-reminder/{type}", async (string type, EmailService emailService, Supabase.Client supabaseClient) =>
    {
        var isMorning = type.ToLower() == "morning";

        // Get incomplete goals to include in email
        var response = await supabaseClient
            .From<Goal>()
            .Where(g => g.IsCompleted == false)
            .Get();

        var pendingTasks = response.Models.ToList();
        var taskCount = pendingTasks.Count;

        var result = await emailService.SendDailyReminderAsync(
            toEmail: "onesmusmaenza@gmail.com",
            toName: "Test User",
            pendingTasks: pendingTasks,
            isMorning: isMorning
        );

        return result
            ? Results.Ok(new { success = true, message = $"Daily {type} reminder sent with {taskCount} tasks!" })
            : Results.BadRequest(new { success = false, message = "Failed to send daily reminder. Check logs for details." });
    });

    // List all users in Supabase Auth
    app.MapGet("/api/list-all-users", async (IHttpClientFactory httpClientFactory) =>
    {
        var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "";
        var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY") ?? "";
        var httpClient = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, $"{supabaseUrl}/auth/v1/admin/users");
        request.Headers.Add("apikey", supabaseKey);
        request.Headers.Add("Authorization", $"Bearer {supabaseKey}");

        var response = await httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return Results.BadRequest(new { error = $"Failed to fetch users: {response.StatusCode}", details = json });
        }

        var doc = System.Text.Json.JsonDocument.Parse(json);
        var users = new List<object>();

        if (doc.RootElement.TryGetProperty("users", out var usersArray))
        {
            foreach (var user in usersArray.EnumerateArray())
            {
                var email = user.TryGetProperty("email", out var e) ? e.GetString() : null;
                var id = user.TryGetProperty("id", out var i) ? i.GetString() : null;
                string? name = null;
                if (user.TryGetProperty("user_metadata", out var meta) && meta.TryGetProperty("display_name", out var n))
                    name = n.GetString();

                users.Add(new { id, email, name });
            }
        }

        return Results.Ok(new { totalUsers = users.Count, users });
    });

    // Preview which users would receive reminders (no emails sent)
    app.MapGet("/api/preview-reminder-recipients", async (Supabase.Client supabaseClient, IHttpClientFactory httpClientFactory) =>
    {
        var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "";
        var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY") ?? "";
        var httpClient = httpClientFactory.CreateClient();
        var recipients = new List<object>();

        var response = await supabaseClient
            .From<Goal>()
            .Where(g => g.IsCompleted == false)
            .Get();

        var goals = response.Models;
        var goalsByUser = goals.GroupBy(g => g.UserId);

        foreach (var userGoals in goalsByUser)
        {
            var userId = userGoals.Key;
            var taskCount = userGoals.Count();

            var request = new HttpRequestMessage(HttpMethod.Get, $"{supabaseUrl}/auth/v1/admin/users/{userId}");
            request.Headers.Add("apikey", supabaseKey);
            request.Headers.Add("Authorization", $"Bearer {supabaseKey}");

            var userResponse = await httpClient.SendAsync(request);

            if (userResponse.IsSuccessStatusCode)
            {
                var json = await userResponse.Content.ReadAsStringAsync();
                var userDoc = System.Text.Json.JsonDocument.Parse(json);
                var root = userDoc.RootElement;

                var email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
                string? displayName = null;
                if (root.TryGetProperty("user_metadata", out var metadata) &&
                    metadata.TryGetProperty("display_name", out var nameProp))
                {
                    displayName = nameProp.GetString();
                }

                recipients.Add(new { email, name = displayName, pendingTasks = taskCount });
            }
        }

        return Results.Ok(new { totalRecipients = recipients.Count, recipients });
    });

    // Test sending reminders to ALL users (like the background service does)
    app.MapGet("/api/test-all-users-reminder/{type}", async (string type, EmailService emailService, Supabase.Client supabaseClient, IHttpClientFactory httpClientFactory) =>
    {
        var isMorning = type.ToLower() == "morning";
        var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "";
        var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY") ?? "";
        var httpClient = httpClientFactory.CreateClient();
        var results = new List<object>();

        // Get all incomplete goals from ALL users
        var response = await supabaseClient
            .From<Goal>()
            .Where(g => g.IsCompleted == false)
            .Get();

        var goals = response.Models;
        var goalsByUser = goals.GroupBy(g => g.UserId);

        foreach (var userGoals in goalsByUser)
        {
            var userId = userGoals.Key;
            var userPendingTasks = userGoals.ToList();

            if (userPendingTasks.Count == 0) continue;

            // Get user info from Supabase Admin API
            var request = new HttpRequestMessage(HttpMethod.Get, $"{supabaseUrl}/auth/v1/admin/users/{userId}");
            request.Headers.Add("apikey", supabaseKey);
            request.Headers.Add("Authorization", $"Bearer {supabaseKey}");

            var userResponse = await httpClient.SendAsync(request);

            if (userResponse.IsSuccessStatusCode)
            {
                var json = await userResponse.Content.ReadAsStringAsync();
                var userDoc = System.Text.Json.JsonDocument.Parse(json);
                var root = userDoc.RootElement;

                var email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
                string? displayName = null;
                if (root.TryGetProperty("user_metadata", out var metadata) &&
                    metadata.TryGetProperty("display_name", out var nameProp))
                {
                    displayName = nameProp.GetString();
                }

                if (!string.IsNullOrEmpty(email))
                {
                    var sent = await emailService.SendDailyReminderAsync(email, displayName ?? "User", userPendingTasks, isMorning);
                    results.Add(new { email, taskCount = userPendingTasks.Count, sent });
                }
            }
        }

        return Results.Ok(new { success = true, message = $"Sent {type} reminders to {results.Count} users", details = results });
    });
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
