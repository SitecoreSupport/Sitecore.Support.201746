namespace Sitecore.Support.FXM.Service.Data.DomainMatchers
{
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.FXM.Abstractions;
  using Sitecore.FXM.Service.Data.DomainMatchers;
  using Sitecore.FXM.Service.Data.DomainMatchers.ContentSearch.Model;
  using Sitecore.FXM.Service.Data.DomainMatchers.ContentSearch.Repositories;
  using Sitecore.FXM.Service.Data.DynamicApiQuery;
  using Sitecore.FXM.Service.Model;
  using Sitecore.FXM.Utilities;
  using Sitecore.SecurityModel;
  using Sitecore.Services.Core;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Linq.Expressions;
 
  public class DomainMatcherEntityRepository : IDomainMatcherEntityRepository, IRepository<DomainMatcherEntity>
  {
    private readonly IConfigurationFactory configurationFactory;
    private readonly IDomainMatcherSearchRepository domainSearchMasterRepo;

    public DomainMatcherEntityRepository() : this(new ConfigurationFactoryWrapper(), new DomainMatcherSearchRepository(DomainMatcherRepoType.Master))
    {
    }

    public DomainMatcherEntityRepository(IConfigurationFactory configuration, IDomainMatcherSearchRepository domainSearchMasterRepo)
    {
      Assert.ArgumentNotNull(configuration, "configuration");
      Assert.ArgumentNotNull(domainSearchMasterRepo, "domainSearchMasterRepo");
      this.configurationFactory = configuration;
      this.domainSearchMasterRepo = domainSearchMasterRepo;
    }

    public void Add(DomainMatcherEntity entity)
    {
      Uri uri;
      if (entity.Domain.StartsWith(Uri.UriSchemeHttp) || entity.Domain.StartsWith(Uri.UriSchemeHttps))
      {
        uri = new Uri(entity.Domain);
      }
      else
      {
        uri = new Uri($"http://{entity.Domain}");
      }
      if (string.IsNullOrEmpty(entity.Name))
      {
        entity.Name = uri.DnsSafeHost.Replace(".", " dot ");
      }
      var scheme = Sitecore.Configuration.Settings.GetSetting("FXM.Protocol", "http://");
      Item item = this.GetContentDatabase().Database.GetItem("{40AC2D94-D9D5-4907-8B3A-346A9DC8BD35}");
      using (new SecurityDisabler())
      {
        Item item2 = item.Add(entity.Name, new TemplateID(new ID("{036DB470-1850-4848-A48A-0931F825B867}")));
        item2.Editing.BeginEdit();
        item2.Fields["{7E1A879D-CAEE-47D0-86E7-205425916418}"].Value = scheme + entity.Domain;
        item2.Editing.EndEdit();
        entity.Id = item2.ID.ToString();
      }
    }

    public void Delete(DomainMatcherEntity entity)
    {
      Item domainMatcherItem = this.GetDomainMatcherItem(entity.Id, this.GetContentDatabase());
      if (domainMatcherItem != null)
      {
        using (new SecurityDisabler())
        {
          domainMatcherItem.Delete();
        }
      }
    }

    public bool Exists(DomainMatcherEntity entity) =>
        (((entity != null) && !string.IsNullOrEmpty(entity.Id)) && (this.domainSearchMasterRepo.Get(new Guid(entity.Id)) != null));

    public DomainMatcherEntity FindById(string id) =>
        this.QueryOne(id);

    public IQueryable<DomainMatcherEntity> GetAll() =>
        this.QueryAll(null, null, m => m.Name, null, 0, 0x270f);

    private IDatabase GetContentDatabase() =>
        this.configurationFactory.GetDatabase(FxmUtility.MasterDatabaseName());

    private Item GetDomainMatcherItem(string itemId, IDatabase database)
    {
      if (!string.IsNullOrEmpty(itemId))
      {
        Item item = database.GetItem(new ID(itemId));
        if (item == null)
        {
          return null;
        }
        if (item.TemplateID.Guid == new Guid("{036DB470-1850-4848-A48A-0931F825B867}"))
        {
          return item;
        }
      }
      return null;
    }

    public IQueryable<DomainMatcherEntity> QueryAll(string nameSearch = null, IEnumerable<OrderByMetadata<DomainMatcherEntity>> ordering = null)
    {
      Expression<Func<DomainMatcherSearchItem, bool>> searchQuery = null;
      if (!string.IsNullOrWhiteSpace(nameSearch))
      {
        nameSearch = nameSearch.ToLower();
        searchQuery = matcher => matcher.DisplayName.Contains(nameSearch) || matcher.Domain.Contains(nameSearch);
      }
      return this.QueryAll(null, searchQuery, null, ordering, 0, 0x270f);
    }

    public IQueryable<DomainMatcherEntity> QueryAll(Expression<Func<DomainMatcherSearchItem, bool>> sourceFilter = null, Expression<Func<DomainMatcherSearchItem, bool>> searchQuery = null, Func<DomainMatcherSearchItem, object> sourceOrderBy = null, IEnumerable<OrderByMetadata<DomainMatcherEntity>> resultOrdering = null, int sourceSkip = 0, int sourceTop = 0x270f) =>
        (from m in this.domainSearchMasterRepo.GetAll(sourceFilter, searchQuery, sourceOrderBy, sourceSkip, sourceTop).ToList<DomainMatcherSearchItem>()
         group m by m.Id.ToString() into g
         select DomainMatcherEntityFactory.CreateFromMaster((from m in g
                                                             orderby m.Version descending
                                                             select m).FirstOrDefault<DomainMatcherSearchItem>())).AsQueryable<DomainMatcherEntity>().ApplyOrdering<DomainMatcherEntity>(resultOrdering).AsQueryable<DomainMatcherEntity>();

    public IQueryable<DomainMatcherEntity> QueryAllForUser(string userName, string nameSearch = null, IEnumerable<OrderByMetadata<DomainMatcherEntity>> ordering = null)
    {
      Expression<Func<DomainMatcherSearchItem, bool>> searchQuery = null;
      userName = userName.ToLower();
      if (!string.IsNullOrWhiteSpace(nameSearch))
      {
        nameSearch = nameSearch.ToLower();
        searchQuery = matcher => matcher.DisplayName.Contains(nameSearch) || matcher.Domain.Contains(nameSearch);
      }
      return this.QueryAll(matcher => matcher.CreatedBy == userName, searchQuery, null, ordering, 0, 0x270f);
    }

    public DomainMatcherEntity QueryOne(string id)
    {
      DomainMatcherSearchItem domain = this.domainSearchMasterRepo.Get(new Guid(id));
      if (domain != null)
      {
        return DomainMatcherEntityFactory.CreateFromMaster(domain);
      }
      return null;
    }

    public void Update(DomainMatcherEntity entity)
    {
      Uri uri;
      Item domainMatcherItem = this.GetDomainMatcherItem(entity.Id, this.GetContentDatabase());
      if (entity.Domain.StartsWith(Uri.UriSchemeHttp) || entity.Domain.StartsWith(Uri.UriSchemeHttps))
      {
        uri = new Uri(entity.Domain);
      }
      else
      {
        uri = new Uri($"http://{entity.Domain}");
      }
      if (string.IsNullOrEmpty(entity.Name))
      {
        entity.Name = uri.DnsSafeHost.Replace(".", " dot ");
      }
      entity.Domain = uri.DnsSafeHost;
      if (domainMatcherItem != null)
      {
       var scheme = Sitecore.Configuration.Settings.GetSetting("FXM.Protocol", "http://");
       using (new SecurityDisabler())
      {
          domainMatcherItem.Editing.BeginEdit();
          domainMatcherItem.Fields["{7E1A879D-CAEE-47D0-86E7-205425916418}"].Value = scheme + entity.Domain;
          domainMatcherItem.Editing.EndEdit();
          entity.Id = domainMatcherItem.ID.ToString();
      }
      }
    }
  }
}
