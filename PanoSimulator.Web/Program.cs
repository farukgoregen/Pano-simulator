using PanoSimulator.Engine.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Engine servisleri — Faz 2'de CircuitSolver buraya DI olarak eklenecek
// builder.Services.AddSingleton<CircuitSolver>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "simulator",
    pattern: "simulator/{id:int}",
    defaults: new { controller = "Simulator", action = "Play" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
