using WebApp.Routes;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddControllersWithViews()
    .AddRazorRuntimeCompilation();

var WebApp = builder.Build();

if (WebApp.Environment.IsDevelopment())
{
    WebApp.UseDeveloperExceptionPage();
}
else
{
    WebApp.UseExceptionHandler("/Home/Error");
    WebApp.UseHsts();
}

WebApp.UseHttpsRedirection();
WebApp.UseStaticFiles();
WebApp.MapRoutes();

WebApp.Run();