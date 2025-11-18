using GoalSettingApp.Components;
using GoalSettingApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Supabase Database Connection
var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
var key = Environment.GetEnvironmentVariable("SUPABASE_KEY");
var options = new Supabase.SupabaseOptions
{
    AutoConnectRealtime = true
};
var supabase = new Supabase.Client(url, key, options);
await supabase.InitializeAsync();

// Register Supabase client in DI container
builder.Services.AddSingleton(supabase);
// Register HttpClient
builder.Services.AddHttpClient();

// Register GoalService as a singleton
builder.Services.AddSingleton<GoalService>();

// Register WeatherService
builder.Services.AddScoped<WeatherService>();

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

app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
