﻿using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Hosting
{
    public interface IHostBuilder
    {
        IServiceCollection Services { get; }
        //ConfigurationManager? Configuration { get; }
        ILifecycleBuilder Lifecycle { get; }
        IPlatform Platform { get; }

        IHost Build();
    }
}
