using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace HostedSvc
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Hosted Service is configured in Program.cs
            // services.AddHostedService<MyHostedService>();
            services.AddHttpContextAccessor();
            services.AddControllersWithViews().AddRazorRuntimeCompilation();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/license", async context => { await context.Response.WriteAsync(System.IO.File.ReadAllText("license.md")); });
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("schedule works!");
                });
                endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");                
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Service was unable to handle this request.");
            });
        }
    }
}

// ------------------------------------------------------------------------------------------------------------
// The following Startup.Configure method adds security-related middleware components in the recommended order:
// ------------------------------------------------------------------------------------------------------------

//public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
//{
//    if (env.IsDevelopment()) {
//        app.UseDeveloperExceptionPage();
//        app.UseDatabaseErrorPage();
//    }
//    else {
//        app.UseExceptionHandler("/Error");
//        app.UseHsts();
//    }
//    app.UseHttpsRedirection();
//    app.UseStaticFiles();
//    app.UseCookiePolicy();
//    app.UseRouting();
//    app.UseRequestLocalization();
//    app.UseCors();
//    app.UseAuthentication();
//    app.UseAuthorization();
//    app.UseSession();
//    app.UseResponseCaching();
//    app.UseEndpoints(endpoints =>
//    {
//        endpoints.MapRazorPages();
//        endpoints.MapControllerRoute(
//            name: "default",
//            pattern: "{controller=Home}/{action=Index}/{id?}");
//    });
//}