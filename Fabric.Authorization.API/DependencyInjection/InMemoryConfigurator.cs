﻿using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Nancy.TinyIoc;

namespace Fabric.Authorization.API.DependencyInjection
{
    public class InMemoryConfigurator : BaseSqlServerConfigurator
    {
        public InMemoryConfigurator(IAppConfiguration appConfiguration) : base(appConfiguration)
        {
        }

        protected override TinyIoCContainer.RegisterOptions RegisterDatabaseContext(TinyIoCContainer container)
        {
            return container.Register<IAuthorizationDbContext, InMemoryAuthorizationDbContext>();
        }
    }
}