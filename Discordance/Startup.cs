using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Discordance;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddLogging();
        services.AddEndpointsApiExplorer();
    }

    public void Configure(IApplicationBuilder app, IHostEnvironment env, IServiceProvider serviceProvider)
    {
        if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

        app.UseHttpLogging();
        app.UseRouting();
        app.UseAuthorization();
        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(1)
        });
        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}