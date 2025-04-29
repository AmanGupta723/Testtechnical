namespace DashboardAPI.WebSocket
{
    public static class WebSocketManagerExtensions
    {
        public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
        {
            services.AddSingleton<WebSocketHandler>();
            return services;
        }

        public static IApplicationBuilder MapWebSocketManager(this IApplicationBuilder app, PathString path)
        {
            return app.Map(path, (_app) => _app.UseMiddleware<WebSocketManagerMiddleware>());
        }
    }
}