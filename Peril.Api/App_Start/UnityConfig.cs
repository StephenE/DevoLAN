using Microsoft.Practices.Unity;
using Peril.Api.Repository;
using Peril.Api.Repository.Azure;
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
            container.RegisterType<ISessionRepository, SessionRepository>();
            
            GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container);
        }
    }
}