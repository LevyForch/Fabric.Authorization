﻿using System;
using System.Security.Claims;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.Domain;
using Fabric.Authorization.Domain.Permissions;
using Fabric.Authorization.Domain.Roles;
using Fabric.Authorization.Domain.Users;
using Fabric.Platform.Bootstrappers.Nancy;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Owin;
using Nancy.TinyIoc;
using Serilog;
using LibOwin;

namespace Fabric.Authorization.API
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        private readonly ILogger _logger;
        private readonly IAppConfiguration _appConfig;

        public Bootstrapper(ILogger logger, IAppConfiguration appConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        }

        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);
            var principal = context.GetOwinEnvironment()[OwinConstants.RequestUser] as ClaimsPrincipal;
            context.CurrentUser = principal;
            var appConfig = container.Resolve<IAppConfiguration>();
            container.UseHttpClientFactory(context, appConfig.IdentityServerConfidentialClientSettings);
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            pipelines.OnError.AddItemToEndOfPipeline((ctx, ex) =>
            {
                _logger.Error(ex, "Unhandled error on request: @{Url}. Error Message: @{Message}", ctx.Request.Url, ex.Message);
                return ctx.Response;
            });
            container.Register(_appConfig);
            container.Register<IUserStore, InMemoryUserStore>();
            container.Register<IUserService, UserService>();
            container.Register<IRoleStore, InMemoryRoleStore>();
            container.Register<IRoleService, RoleService>();
            container.Register<IPermissionService, PermissionService>();
            container.Register<IPermissionStore, InMemoryPermissionStore>();
        }
    }
}