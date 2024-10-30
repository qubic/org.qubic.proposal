
using org.qubic.common.caching;
using org.qubic.proposal.api.HostedServices;
using org.qubic.proposal.api.Model;

namespace org.qubic.proposal.api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // load settings
            var appSettings = new AppSettings();
            builder.Configuration.GetSection("AppSettings").Bind(appSettings);
            builder.Services.AddSingleton(appSettings);

            // register redis
            builder.Services.AddSingleton<IRedisConfiguration>(appSettings.Redis);
            builder.Services.AddSingleton<RedisService>();


            // register services
            builder.Services.AddHostedService<NetworkSyncService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();

            // trigger the start of the hosted service
            app.Services.GetRequiredService<NetworkSyncService>();
        }
    }
}
