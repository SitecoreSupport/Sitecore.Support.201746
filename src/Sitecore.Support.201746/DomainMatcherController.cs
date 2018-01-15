using Sitecore.FXM.Abstractions;
using Sitecore.FXM.Service.Data.DomainMatchers;
using Sitecore.Services.Core;

namespace Sitecore.Support.FXM.Service.Controllers
{
  [ServicesController("DomainMatcher.Service")]
  public class DomainMatcherController : Sitecore.FXM.Service.Controllers.DomainMatcherController
  {
    public DomainMatcherController() : base(new DecoratingOrderingRepository(new DecoratingAnalyticsRepository(new DecoratingPublishDataRepository(new Sitecore.Support.FXM.Service.Data.DomainMatchers.DomainMatcherEntityRepository()))), new SitecoreContextWrapper())
    {
    }
  }
}