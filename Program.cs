using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Spectator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(ConfigureContainer);

        private static void ConfigureContainer(HostBuilderContext context, ContainerBuilder builder)
        {
            builder.RegisterType<Application>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ProcessManager>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ProfileManager>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<CategoryProcessorFactory>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<InfluxStorage>().AsImplementedInterfaces().SingleInstance();
        }
    }
}
