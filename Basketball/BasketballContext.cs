using System;
using System.Collections.Generic;
using System.IO;
using Commune.Basis;
using Commune.Data;
using Commune.Task;
using Shop.Engine;
using System.Data;

namespace Basketball
{
  public class BasketballContext : IContext
  {
    public readonly static object lockObj = new object();

    readonly SiteSettings siteSettings;
    public SiteSettings SiteSettings
    {
      get { return siteSettings; }
    }

    readonly TaskPull pull;
    public TaskPull Pull
    {
      get { return pull; }
    }

    readonly string rootPath;
    public string RootPath
    {
      get { return rootPath; }
    }
    readonly string imagesPath;
    public string ImagesPath
    {
      get { return imagesPath; }
    }
    readonly IDataLayer userConnection;
    public IDataLayer UserConnection
    {
      get { return userConnection; }
    }
    readonly IDataLayer fabricConnection;
    public IDataLayer FabricConnection
    {
      get { return fabricConnection; }
    }

    readonly IDataLayer messageConnection;
    public IDataLayer MessageConnection
    {
      get { return messageConnection; }
    }

    readonly IDataLayer forumConnection;
    public IDataLayer ForumConnection
    {
      get { return forumConnection; }
    }

    public IDataLayer OrderConnection
    {
      get { throw new Exception("SiteContext not supported OrderConnection"); }
    }

    readonly EditorSelector sectionEditorSelector;
    public EditorSelector SectionEditorSelector
    {
      get { return sectionEditorSelector; }
    }

    readonly EditorSelector unitEditorSelector;
    public EditorSelector UnitEditorSelector
    {
      get { return unitEditorSelector; }
    }

    readonly UserStorage userStorage;
    public UserStorage UserStorage
    {
      get { return userStorage; }
    }

    readonly ContextTunes contextTunes;
		public ContextTunes ContextTunes
    {
      get { return contextTunes; }
    }


		readonly RawCache<Tuple<ObjectHeadBox, LightHead[]>> newsCache;
    public ObjectHeadBox News
    {
      get
      {
        lock (lockObj)
          return newsCache.Result.Item1;
      }
    }
    public LightHead[] ActualNews
    {
      get
      {
        lock (lockObj)
          return newsCache.Result.Item2;
      }
    }
    long newsChangeTick = 0;
    public void UpdateNews()
    {
      lock (lockObj)
        newsChangeTick++;
    }

    readonly RawCache<Tuple<ObjectBox, LightObject[]>> articlesCache;
    public ObjectBox Articles
    {
      get
      {
        lock (lockObj)
          return articlesCache.Result.Item1;
      }
    }
    public LightObject[] ActualArticles
    {
      get
      {
        lock (lockObj)
          return articlesCache.Result.Item2;
      }
    }
    long articleChangeTick = 0;
    public void UpdateArticles()
    {
      lock (lockObj)
        articleChangeTick++;
    }

    readonly RawCache<RowLink[]> lastPublicationCommentsCache;
    long publicationCommentChangeTick = 0;
    public RowLink[] LastPublicationComments
    {
      get
      {
        lock (lockObj)
          return lastPublicationCommentsCache.Result;
      }
    }

    readonly RawCache<RowLink[]> lastForumCommentsCache;
    long forumCommentChangeTick = 0;
    public RowLink[] LastForumComments
    {
      get
      {
        lock (lockObj)
          return lastForumCommentsCache.Result;
      }
    }

    public void UpdateLastComments(bool forForum)
    {
      lock (lockObj)
      {
        if (forForum)
          forumCommentChangeTick++;
        else
          publicationCommentChangeTick++;
      }
    }

		readonly RawCache<TagStore> tagsCache;
		public TagStore Tags
		{
			get
			{
				lock (lockObj)
					return tagsCache.Result;
			}
		}

    //readonly RawCache<Tuple<ObjectHeadBox, Dictionary<string, int>>> tagsCache;
    //public ObjectHeadBox Tags
    //{
    //  get
    //  {
    //    lock (lockObj)
    //      return tagsCache.Result.Item1;
    //  }
    //}

    //public Dictionary<string, int> TagIdByKey
    //{
    //  get
    //  {
    //    lock (lockObj)
    //      return tagsCache.Result.Item2;
    //  }
    //}

    long tagChangeTick = 0;
    public void UpdateTags()
    {
      lock (lockObj)
        tagChangeTick++;
    }

    readonly RawCache<TableLink> unreadDialogCache;
    public TableLink UnreadDialogLink
    {
      get
      {
        lock (lockObj)
          return unreadDialogCache.Result;
      }
    }

    long unreadChangeTick = 0;
    public void UpdateUnreadDialogs()
    {
      lock (lockObj)
        unreadChangeTick++;
    }

    public readonly TopicStorageCache NewsStorages;
    public readonly TopicStorageCache ArticleStorages;
    public readonly ForumStorageCache Forum;

    public BasketballContext(string rootPath,
      EditorSelector sectionEditorSelector, EditorSelector unitEditorSelector,
      IDataLayer userConnection, IDataLayer fabricConnection, 
      IDataLayer messageConnection, IDataLayer forumConnection)
    {
      this.rootPath = rootPath;
      this.imagesPath = Path.Combine(RootPath, "Images");
      this.userConnection = userConnection;
      this.fabricConnection = fabricConnection;
      this.messageConnection = messageConnection;
      this.forumConnection = forumConnection;
      this.sectionEditorSelector = sectionEditorSelector;
      this.unitEditorSelector = unitEditorSelector;

      this.userStorage = new UserStorage(userConnection);
      this.NewsStorages = new TopicStorageCache(fabricConnection, messageConnection, NewsType.News);
      this.ArticleStorages = new TopicStorageCache(fabricConnection, messageConnection, ArticleType.Article);
      this.Forum = new ForumStorageCache(fabricConnection, forumConnection);

      string settingsPath = Path.Combine(rootPath, "SiteSettings.config");
      if (!File.Exists(settingsPath))
        this.siteSettings = new SiteSettings();
      else
        this.siteSettings = XmlSerialization.Load<SiteSettings>(settingsPath);

      this.pull = new TaskPull(
        new ThreadLabel[] { Labels.Service },
        TimeSpan.FromMinutes(15)
      );

      this.newsCache = new Cache<Tuple<ObjectHeadBox, LightHead[]>, long>(
        delegate
        {
          ObjectHeadBox newsBox = new ObjectHeadBox(fabricConnection, 
            string.Format("{0} order by act_from desc", DataCondition.ForTypes(NewsType.News))
          );

          int[] allNewsIds = newsBox.AllObjectIds;

          List<LightHead> actualNews = new List<LightHead>();
          for (int i = 0; i < Math.Min(22, allNewsIds.Length); ++i)
          {
            int newsId = allNewsIds[i];
            actualNews.Add(new LightHead(newsBox, newsId));
          }

          return _.Tuple(newsBox, actualNews.ToArray());
        },
        delegate { return newsChangeTick; }
      );

      this.articlesCache = new Cache<Tuple<ObjectBox, LightObject[]>, long>(
        delegate
        {
          ObjectBox articleBox = new ObjectBox(fabricConnection,
            string.Format("{0} order by act_from desc", DataCondition.ForTypes(ArticleType.Article))
          );

          int[] allArticleIds = articleBox.AllObjectIds;

          ObjectBox actualBox = new ObjectBox(FabricConnection,
            string.Format("{0} order by act_from desc limit 5", DataCondition.ForTypes(ArticleType.Article))
          );

          List<LightObject> actualArticles = new List<LightObject>();
          foreach (int articleId in actualBox.AllObjectIds)
          {
            actualArticles.Add(new LightObject(actualBox, articleId));
          }

          return _.Tuple(articleBox, actualArticles.ToArray());
        },
        delegate { return articleChangeTick; }
      );

      this.lightStoreCache = new Cache<IStore, long>(
        delegate
        {
          LightObject contacts = DataBox.LoadOrCreateObject(fabricConnection,
            ContactsType.Contacts, ContactsType.Kind.CreateXmlIds, "main");

          LightObject seo = DataBox.LoadOrCreateObject(fabricConnection,
            SEOType.SEO, SEOType.Kind.CreateXmlIds, "main");

          SectionStorage sections = SectionStorage.Load(fabricConnection);

          WidgetStorage widgets = WidgetStorage.Load(fabricConnection, siteSettings.DisableScripts);

          RedirectStorage redirects = RedirectStorage.Load(fabricConnection);

          SiteStore store = new SiteStore(sections, null, widgets, redirects, contacts, seo);
          store.Links.AddLink("register", null);
          store.Links.AddLink("passwordreset", null);

          return store;
        },
        delegate { return dataChangeTick; }
      );

      this.lastPublicationCommentsCache = new Cache<RowLink[], long>(
        delegate
        {
          DataTable table =  messageConnection.GetTable("", 
            "Select Distinct article_id From message order by create_time desc limit 10"
          );

          List<RowLink> lastComments = new List<RowLink>(10);
          foreach (DataRow row in table.Rows)
          {
            int topicId = ConvertHlp.ToInt(row[0]) ?? -1;

            if (News.ObjectById.Exist(topicId))
            {
              TopicStorage topic = NewsStorages.ForTopic(topicId);
              RowLink lastMessage = _.Last(topic.MessageLink.AllRows);
              if (lastMessage != null)
                lastComments.Add(lastMessage);
            }
            else if (Articles.ObjectById.Exist(topicId))
            {
              TopicStorage topic = ArticleStorages.ForTopic(topicId);
              RowLink lastMessage = _.Last(topic.MessageLink.AllRows);
              if (lastMessage != null)
                lastComments.Add(lastMessage);
            }
          }

          return lastComments.ToArray();
        },
        delegate { return publicationCommentChangeTick; }
      );

      this.lastForumCommentsCache = new Cache<RowLink[], long>(
        delegate
        {
          DataTable table = forumConnection.GetTable("",
            "Select Distinct article_id From message order by create_time desc limit 7"
          );

					List<RowLink> lastComments = new List<RowLink>(7);
          foreach (DataRow row in table.Rows)
          {
            int topicId = ConvertHlp.ToInt(row[0]) ?? -1;

            TopicStorage topic = Forum.TopicsStorages.ForTopic(topicId);
            RowLink lastMessage = _.Last(topic.MessageLink.AllRows);
            if (lastMessage != null)
              lastComments.Add(lastMessage);
          }

          return lastComments.ToArray();
        },
        delegate { return forumCommentChangeTick; }
      );

			this.tagsCache = new Cache<TagStore, long>(
				delegate
				{
					ObjectHeadBox tagBox = new ObjectHeadBox(fabricConnection, DataCondition.ForTypes(TagType.Tag) + " order by xml_ids asc");

					return new TagStore(tagBox);
				},
				delegate { return tagChangeTick; }
			);

      //this.tagsCache = new Cache<Tuple<ObjectHeadBox, Dictionary<string, int>>, long>(
      //  delegate
      //  {
      //    ObjectHeadBox tagBox = new ObjectHeadBox(fabricConnection, DataCondition.ForTypes(TagType.Tag) + " order by xml_ids asc");

      //    Dictionary<string, int> tagIdByKey = new Dictionary<string, int>();
      //    foreach (int tagId in tagBox.AllObjectIds)
      //    {
      //      string tagName = TagType.DisplayName.Get(tagBox, tagId);
      //      if (StringHlp.IsEmpty(tagName))
      //        continue;

      //      string tagKey = tagName.ToLower();
      //      tagIdByKey[tagKey] = tagId;
      //    }

      //    return _.Tuple(tagBox, tagIdByKey);
      //  },
      //  delegate { return tagChangeTick; }
      //);

      this.unreadDialogCache = new Cache<TableLink, long>(
        delegate
        {
          return DialogueHlp.LoadUnreadLink(forumConnection);
        },
        delegate { return unreadChangeTick; }
      );

      Pull.StartTask(Labels.Service,
        SiteTasks.SitemapXmlChecker(this, rootPath,
          delegate (LinkInfo[] sectionlinks)
          {
            List<LightLink> allLinks = new List<LightLink>();
            allLinks.AddRange(
              ArrayHlp.Convert(sectionlinks, delegate (LinkInfo link)
              {
                return new LightLink(link.Directory, null);
              })
            );

            foreach (int articleId in Articles.AllObjectIds)
            {
              LightHead article = new LightHead(Articles, articleId);
              allLinks.Add(
                new LightLink(UrlHlp.ShopUrl("article", articleId),
                  article.Get(ObjectType.ActTill) ?? article.Get(ObjectType.ActFrom)
                )
              );
            }

            foreach (int newsId in News.AllObjectIds)
            {
              LightHead news = new LightHead(News, newsId);
              allLinks.Add(
                new LightLink(UrlHlp.ShopUrl("news", newsId),
                  news.Get(ObjectType.ActTill) ?? news.Get(ObjectType.ActFrom)
                )
              );
            }

            //foreach (int tagId in Tags.AllObjectIds)
            //{
            //  LightHead tag = new LightHead(Tags, tagId);
            //  allLinks.Add(
            //    new LightLink(string.Format("/tags?tag={0}", tagId)
            //    )
            //  );
            //}

            return allLinks.ToArray();
          }
        )
      );
    }

    readonly RawCache<IStore> lightStoreCache;
    public IStore Store
    {
      get
      {
        lock (lockObj)
          return lightStoreCache.Result;
      }
    }

    long dataChangeTick = 0;
    public void UpdateStore()
    {
      lock (lockObj)
        dataChangeTick++;
    }
  }
}
