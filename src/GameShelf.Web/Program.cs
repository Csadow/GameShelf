using GameShelf.Web.Components;
using GameShelf.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
var dataFile = Environment.GetEnvironmentVariable("GAMESHELF_DATA_FILE");
if (string.IsNullOrWhiteSpace(dataFile))
    dataFile = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "games.json");
builder.Services.AddSingleton<IGameRepository>(new JsonGameRepository(dataFile));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
