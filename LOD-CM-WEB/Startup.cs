using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LOD_CM
{
    public class Startup
    {
        private static ILogger log;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            
            services.AddMvc()
                .AddRazorPagesOptions(options => {options.Conventions.AddPageRoute("/ConceptualModel", "/lod-cm/ConceptualModel");}) // Not working .AddPageRoute("/Index", "/lod-cm/")
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
                
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        
// #if DEBUG
//             services.AddMvc()
//                 .AddRazorPagesOptions(options => {options.Conventions.AddPageRoute("/lod-cm/Index", "Index").AddPageRoute("/lod-cm/ConceptualModel", "ConceptualModel");})
//                 .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
// #else
//             services.AddMvc()
//                 .AddRazorPagesOptions(options => {options.RootDirectory = "/lod-cm";})
//                 .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
// #endif
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc();
        }
    }
}
