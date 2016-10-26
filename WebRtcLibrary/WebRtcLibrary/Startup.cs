#region using block

using Microsoft.Owin;
using Owin;
using WebRtcLibrary;

#endregion

[assembly: OwinStartup(typeof(Startup))]

namespace WebRtcLibrary
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}