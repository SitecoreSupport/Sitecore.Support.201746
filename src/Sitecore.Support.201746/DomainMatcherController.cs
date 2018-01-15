using Sitecore.Diagnostics;
using Sitecore.FXM.Abstractions;
using Sitecore.FXM.Service.Controllers;
using Sitecore.FXM.Service.Data.DomainMatchers;
using Sitecore.FXM.Service.Model;
using Sitecore.Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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