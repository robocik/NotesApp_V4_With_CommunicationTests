using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using Duende.IdentityServer.Configuration;
using Microsoft.AspNetCore.Authentication;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.Logging;
using NHibernate.AspNetCore.Identity;
using NoteBookApp.FileSystems.AzureBlobStorage;
using NoteBookApp.Logic;
using NoteBookApp.Logic.Domain;
using NoteBookApp.Logic.Handlers.Files;
using NoteBookApp.Logic.Handlers.Notes;
using NoteBookApp.Logic.Interfaces;
using NoteBookApp.Server.Infrastructure;
using NoteBookApp.Shared;

namespace NoteBookApp.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(provider => new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperConfiguration());
            }).CreateMapper());
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            var settings = Configuration
                .GetSection("AppSettings")
                .Get<AppSettings>();
            services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddDefaultTokenProviders()
                .AddHibernateStores();

            services.AddScoped(typeof(IUserStore<ApplicationUser>), typeof(UsersService));
            ConfigureNHibernate(services, settings);

            if (settings.IsDemo)
            {
                services.AddSingleton<IFileSystemProvider, LiveDemoFileSystemProvider>();
                
            }
            else
            {
                services.AddSingleton<IFileSystemProvider>(x => new AzureBlobStorageProvider(Configuration.GetValue<string>("AzureBlob")));
            }
            
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            });
            services.AddHttpContextAccessor();
            services.AddSingleton<IDateTimeProvider,StandardDateTimeProvider>();
            services.AddTransient<INotifierMediatorService, NotifierMediatorService>();

            ConfigureIdentityServer(services);

            services.AddScoped(x =>
            {
                var httpContext = x.GetService<IHttpContextAccessor>();
                var userManager = x.GetService<UserManager<ApplicationUser>>();
                ApplicationUser? user = null;
                if (httpContext?.HttpContext?.User != null)
                {
                    user = userManager?.GetUserAsync(httpContext.HttpContext.User).GetAwaiter().GetResult();
                }
                var securityInfo = new SecurityInfo(user);
                return securityInfo;
            });
            services.AddSingleton<IImageResizer, ImageResizer>();
            

            services.AddMediatR(typeof(GetNotesQueryHandler).Assembly);
            services.AddControllersWithViews(options =>
            {
                options.ModelBinderProviders.Insert(0, new JsonModelBinderProvider());
            });
            services.AddRazorPages();

            
        }

        protected virtual void ConfigureIdentityServer(IServiceCollection services)
        {
            services.AddIdentityServer()
                .AddAspNetIdentity<ApplicationUser>()
                .AddIdentityResources()
                .AddApiResources()
                .AddClients()
                .AddDeveloperSigningCredential();

            services.AddAuthentication().AddIdentityServerJwt();
        }


        protected virtual void ConfigureNHibernate(IServiceCollection services, AppSettings settings)
        {
            if (settings.IsDemo)
            {

                services.AddDemoNHibernate();
                services.AddSingleton<ISessionStore, DistributedSessionStoreWithStart>();
                services.AddSession(options => {
                    options.Cookie.IsEssential = true;
                    options.IdleTimeout = TimeSpan.FromMinutes(30);
                    options.Cookie.Name = ".MyApplication";
                });
            }
            else
            {

                var connectionString = Configuration.GetValue<string>("DefaultConnection");
                services.AddNHibernateSqlServer(connectionString);
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var settings = Configuration
                .GetSection("AppSettings")
                .Get<AppSettings>();
            if (settings?.IsDemo == true)
            {
                var tempFolder = new DirectoryInfo("wwwroot\\Temp");
                if (tempFolder.Exists)
                {
                    tempFolder.Delete(true);
                }
                tempFolder.Create();
                app.UseSession();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            app.ConfigureExceptionHandler(env);
            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseIdentityServer();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    
    }

}
