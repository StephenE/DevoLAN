using Microsoft.Practices.Unity;
using Peril.Api.Models;
using Peril.Api.Repository;
using Peril.Api.Repository.Azure;
using System.Configuration;
using System.Web.Http;
using Unity.WebApi;

namespace Peril.Api
{
    public static class UnityConfig
    {
        public static void RegisterComponents()
        {
            var container = new UnityContainer();
            
            // register all components
            container.RegisterType<ISessionRepository, SessionRepository>(new InjectionConstructor(ConfigurationManager.AppSettings["StorageConnectionString"]));
            container.RegisterType<IUserRepository, UserRepository>();
            
            GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container);
        }
    }
}