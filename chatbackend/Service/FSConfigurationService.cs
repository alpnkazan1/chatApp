using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using chatbackend.Helpers;

namespace chatbackend.Service
{
    public class FSConfigurationService : IHostedService
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _configuration;

        public FSConfigurationService(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _loggerFactory = loggerFactory;
            _configuration = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var baseFilePath = _configuration["FileStorage:BaseFilePath"];
            var urlSigningKey = _configuration["UrlSigningKey"];

            FileSystemAccess.FileSystemConfigure(_loggerFactory, baseFilePath, urlSigningKey);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        // Helper function to configure ChatHelper
        void ConfigureChatHelper(IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IHostedService, FSConfigurationService>();
            services.AddSingleton<FSConfigurationService>();

            services.AddSingleton(serviceProvider =>
            {
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var baseFilePath = configuration["FileStorage:BaseFilePath"];
                var urlSigningKey = configuration["UrlSigningKey"];

                FileSystemAccess.FileSystemConfigure(loggerFactory, baseFilePath, urlSigningKey);
                return FileSystemAccess.GetSubfolder(1); // You can return smth to be used with FileSystemAccess class in app
            });
        }
    }
}