using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(RoboBankOmniKassa.Startup))]
namespace RoboBankOmniKassa
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
