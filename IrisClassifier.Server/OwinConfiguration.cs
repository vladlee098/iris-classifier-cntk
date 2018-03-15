using Owin;

namespace IrisCliassifierService.Host
{
    public class OwinConfiguration
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            appBuilder.UseWebApi(WebApiConfiguration.Register());
        }
    }
}
