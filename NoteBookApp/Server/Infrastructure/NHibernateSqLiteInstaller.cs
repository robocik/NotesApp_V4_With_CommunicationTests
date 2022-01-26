using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using NHibernate;
using NHibernate.AspNetCore.Identity;
using NHibernate.Cfg;
using NHibernate.Connection;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Engine;
using NHibernate.Mapping.ByCode;
using NHibernate.MultiTenancy;
using NHibernate.NetCore;
using NHibernate.Tool.hbm2ddl;
using NoteBookApp.Logic;
using NoteBookApp.Logic.Domain;
using NoteBookApp.Logic.Mappings;
using ISession = NHibernate.ISession;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace NoteBookApp.Server.Infrastructure;

public class DistributedSessionStoreWithStart : ISessionStore
{
    DistributedSessionStore innerStore;

    public Microsoft.AspNetCore.Http.ISession Create(string sessionKey, TimeSpan idleTimeout, TimeSpan ioTimeout, Func<bool> tryEstablishSession,
        bool isNewSessionKey)
    {
        Microsoft.AspNetCore.Http.ISession session = innerStore.Create(sessionKey, idleTimeout, ioTimeout,
            tryEstablishSession, isNewSessionKey);
        if (isNewSessionKey)
        {
            var tenantId = Guid.NewGuid();
            session.SetString("TenantId", tenantId.ToString());
        }
        return session;
    }

    public DistributedSessionStoreWithStart(IDistributedCache cache, ILoggerFactory loggerFactory)
    {
        innerStore = new DistributedSessionStore(cache, loggerFactory);
    }
}

class MultiTenantConnectionProvider : AbstractMultiTenancyConnectionProvider
{
    protected override string GetTenantConnectionString(TenantConfiguration tenantConfiguration, ISessionFactoryImplementor sessionFactory)
    {
        var connectionString = $@"Data Source='wwwroot\Temp\{tenantConfiguration.TenantIdentifier}.db';Version=3;New=True;Compress=True;";
        return connectionString;
    }
}


public static class NHibernateSqLiteInstaller
{
    public static IServiceCollection AddDemoNHibernate(this IServiceCollection services)
    {
        var cfg = new Configuration();
        cfg.DataBaseIntegration(db =>
        {
            db.Dialect<SQLiteDialect>();
            db.Driver<SQLite20Driver>();
            db.ConnectionProvider<DriverConnectionProvider>();
            db.MultiTenancyConnectionProvider<MultiTenantConnectionProvider>();
            db.MultiTenancy = MultiTenancyStrategy.Database;
            db.LogSqlInConsole = true;
            db.LogFormattedSql = true;
            db.ConnectionString = @"Data Source='wwwroot\Temp\TestDb.db';Version=3;New=True;Compress=True;";
        });

        
        cfg.Cache(c => c.UseQueryCache = false);

        var mapping = new ModelMapper();
        mapping.AddMappings(typeof(ApplicationUserMapping).Assembly.GetTypes());
        mapping.AddMapping(typeof(NHibernate.AspNetCore.Identity.Mappings.IdentityUserMappingSqlite));
        mapping.AddMapping(typeof(NHibernate.AspNetCore.Identity.Mappings.IdentityUserLoginMappingSqlite));
        mapping.AddMapping(typeof(NHibernate.AspNetCore.Identity.Mappings.IdentityUserTokenMappingSqlite));


        var mappingDocument = mapping.CompileMappingForAllExplicitlyAddedEntities();
        cfg.AddMapping(mappingDocument);
        services.AddHibernate(cfg);

        services.AddScoped(typeof(IUserStore<ApplicationUser>), typeof(UsersService));

        services.AddScoped<ISession>(provider =>
        {
            var nhSession = CreateNhSession(provider, cfg);

            return nhSession;
        });
        return services;
    }

    private static ISession CreateNhSession(IServiceProvider provider, Configuration cfg)
    {
        var httpContextAccessor = provider.GetService<IHttpContextAccessor>();
        var tempId = httpContextAccessor!.HttpContext!.Session.GetString("TenantId");
        Guid tenantId = Guid.Empty;
        if (tempId != null)
        {
            tenantId = new Guid(tempId);
        }

        var nhSession = provider.GetService<ISessionFactory>()!.WithOptions()
            .Tenant(new TenantConfiguration(tenantId.ToString())).OpenSession();

        var file = new FileInfo(@$"wwwroot\Temp\{tenantId}.db");
        if (!file.Exists || file.Length == 0)
        {
            FillDatabase(cfg, nhSession);
        }

        return nhSession;
    }

    private static void FillDatabase(Configuration cfg, ISession? nhSession)
    {
        SchemaExport exp = new SchemaExport(cfg);
        exp.Execute(true, true, false, nhSession!.Connection, null);

        using (var trans = nhSession.BeginTransaction())
        {
            var demo = new DemoDataCreator(nhSession);
            demo.Create();
            trans.Commit();
        }
    }


}