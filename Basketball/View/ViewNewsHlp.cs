using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NitroBolt.Wui;
using Commune.Basis;
using Commune.Html;
using Commune.Data;
using Shop.Engine;

namespace Basketball
{
  public class ViewNewsHlp
  {
    static BasketballContext context
    {
      get { return (BasketballContext)SiteContext.Default; }
    }

    static IDataLayer fabricConnection
    {
      get
      {
        return SiteContext.Default.FabricConnection;
      }
    }

    const int newsCountOnPage = 50;

    public static IHtmlControl GetActualNewsBlock(SiteState state, LightObject currentUser)
    {
      IHtmlControl[] items = ViewNewsHlp.GetNewsItems(state, context.ActualNews);

      HPanel editBlock = null;
      string editHint = "newsAdd";
      if (state.BlockHint == editHint)
      {
        if (state.Tag == null)
          state.Tag = new List<string>();

        //string unsaveText = BasketballHlp.AddCommentFromCookie();

        editBlock = new HPanel(
          Decor.PropertyEdit("newsTitle", "Заголовок новости"),
          new HPanel(
            HtmlHlp.CKEditorCreate("newsText", "", "300px", true)
          ),
					BasketballHlp.SetCkeditorFromLocalStorageScriptControl("newsText"),
					//ViewTagHlp.GetEditTagsPanel(state, context.Tags.TagBox, state.Tag as List<string>, true),
					new HPanel(
						new HLabel("Теги").FontBold(),
						new HTextEdit("newsTags", "").Width("100%")
					).MarginTop(5).MarginBottom(5),
					Decor.PropertyEdit("newsOriginName", "Источник"),
          Decor.PropertyEdit("newsOriginUrl", "Ссылка"),
          Decor.Button("Добавить новость").MarginTop(10) //.CKEditorOnUpdateAll()
						//.OnClick(string.Format("CK_updateAll(); {0}", BasketballHlp.AddCommentToCookieScript("newsText")))
						.OnClick(string.Format("CK_updateAll(); {0}", 
							BasketballHlp.SetLocalStorageScript("addText", "newsText")
						))
						.Event(Command.SaveNewsAdd, "addNewsData",
              delegate (JsonData json)
              {
                string title = json.GetText("newsTitle");
                string text = json.GetText("newsText");
								string rawTags = json.GetText("newsTags");
                string originName = json.GetText("newsOriginName");
                string originUrl = json.GetText("newsOriginUrl");

                WebOperation operation = state.Operation;

                if (!operation.Validate(title, "Не задан заголовок"))
                  return;

                if (!operation.Validate(text, "Не задан текст"))
                  return;

                ParentBox editBox = new ParentBox(context.FabricConnection, "1=0");

                int addNewsId = editBox.CreateObject(NewsType.News, NewsType.Title.CreateXmlIds(title), DateTime.UtcNow);
                LightParent editNews = new LightParent(editBox, addNewsId);

                editNews.Set(NewsType.PublisherId, currentUser.Id);
                editNews.Set(NewsType.Text, text);
                editNews.Set(NewsType.OriginName, originName);
                editNews.Set(NewsType.OriginUrl, originUrl);

								//string[] tags = rawTags.Split(',');
								List<string> tags = ViewTagHlp.ParseTags(rawTags);
								ViewTagHlp.SaveTags(context, tags, editNews);

                editBox.Update();

                context.UpdateNews();

                state.BlockHint = "";
                state.Tag = null;

								state.Option.Set(OptionType.NewsAdded, true);

								//BasketballHlp.ResetAddComment();
							}
            )
        ).EditContainer("addNewsData").Padding(5, 10).MarginTop(10).Background(Decor.pageBackground);
      }

      IHtmlControl addButton = null;
      if (currentUser != null && !BasketballHlp.NoRedactor(currentUser))
      {
        addButton = Decor.Button("Добавить").MarginLeft(10)
          .Event("news_add", "", delegate
            {
              state.Tag = null;
              state.SetBlockHint(editHint);
            }
          );
      }

      return new HPanel(
        Decor.Subtitle("Новости"),
        new HPanel(
          items.ToArray()
        ),
        new HPanel(
          new HLink("/novosti",
            "Все новости",
            new HBefore().ContentIcon(5, 12).BackgroundImage(UrlHlp.ImageUrl("pointer.gif")).MarginRight(5).VAlign(-2)
          ).FontBold(),
          addButton
        ).MarginTop(15),
        editBlock
      );
    }

    static IHtmlControl[] GetNewsItems(SiteState state, int[] allNewsIds, int pageNumber)
    {
      //int pageCount = BinaryHlp.RoundUp(allNewsIds.Length, newsCountOnPage);
      //int curPos = pageNumber * newsCountOnPage;
      //if (curPos < 0 || curPos >= allNewsIds.Length)
      //  return null;

      //int[] newsIds = ArrayHlp.GetRange(allNewsIds, curPos, Math.Min(newsCountOnPage, allNewsIds.Length - curPos));

      int[] newsIds = ViewJumpHlp.GetPageItems(allNewsIds, newsCountOnPage, pageNumber);
      if (newsIds == null)
        return null;

      LightHead[] newsList = ArrayHlp.Convert(newsIds, delegate (int id)
        { return new LightHead(context.News, id); }
      );

      return GetNewsItems(state, newsList);
    }

		public static IHtmlControl GetFoundTagListView(SiteState state, out string title)
		{
			title = string.Format("Найденные теги по запросу: {0}", state.Option.Get(OptionType.SearchQuery));

			List<IHtmlControl> elements = new List<IHtmlControl>();

			int[] foundTagIds = state.Option.Get(OptionType.FoundTagIds);
			if (foundTagIds != null)
			{
				foreach (int tagId in foundTagIds)
				{
					RowLink tagRow = context.Tags.TagBox.ObjectById.AnyRow(tagId);
					if (tagRow == null)
						continue;

					string tagDisplay = TagType.DisplayName.Get(tagRow);

					elements.Add(
						new HPanel(
							new HLink(ViewTagHlp.TagUrl(tagId, 0), tagDisplay)
						).MarginBottom(4)
					);
				}
			}

			return new HPanel(
				Decor.Title(title),
				new HPanel(
					elements.ToArray()
				)
			);
		}

    public static IHtmlControl GetTagView(SiteState state, LightObject currentUser, 
      int? tagId, int pageNumber, out string title, out string description)
    {
      title = "";
      description = "";

      if (tagId == null)
        return null;

      RowLink tagRow = context.Tags.TagBox.ObjectById.AnyRow(tagId.Value);
      if (tagRow == null)
        return null;

      string tagDisplay = TagType.DisplayName.Get(tagRow);

      title = string.Format("{0} - все новости на basketball.ru.com", tagDisplay);
      description = string.Format("{0} - все новости. Живое обсуждение баскетбольных событий на basketball.ru.com", tagDisplay);

      int[] newsIds = ViewTagHlp.GetNewsIdsForTag(context.FabricConnection, tagId.Value);

      IHtmlControl[] items = GetNewsItems(state, newsIds, pageNumber);
      if (items == null)
        return null;

      string urlWithoutPageIndex = string.Format("/tags?tag={0}", tagId.Value);
      return new HPanel(
        Decor.Title(tagDisplay), //.Color(Decor.subtitleColor),
        //Decor.Subtitle(string.Format("Тег — {0}", tagDisplay)).MarginTop(5),
        new HPanel(
          new HPanel(
            items
          ),
          ViewJumpHlp.JumpBar(urlWithoutPageIndex, newsIds.Length, newsCountOnPage, pageNumber)
        )
      );
    }

    public static IHtmlControl GetNewsListView(SiteState state, LightObject currentUser, int pageNumber)
    {
      //int pageCount = BinaryHlp.RoundUp(allNewsIds.Length, newsCountOnPage);
      //int curPos = pageNumber * newsCountOnPage;
      //if (curPos < 0 || curPos >= allNewsIds.Length)
      //  return null;

      //int[] newsIds = ArrayHlp.GetRange(allNewsIds, curPos, Math.Min(newsCountOnPage, allNewsIds.Length - curPos));
      //LightHead[] newsList = ArrayHlp.Convert(newsIds, delegate (int id)
      //{ return new LightHead(context.News, id); }
      //);

      int[] allNewsIds = context.News.AllObjectIds;
      IHtmlControl[] items = GetNewsItems(state, allNewsIds, pageNumber);
      if (items == null)
        return null;

      //string title = StringHlp.IsEmpty(tag) ? "Новости" : "Теги";
      return new HPanel(
        Decor.Title("Новости"),
        //Decor.Subtitle(string.Format("Тег — {0}", tag)),
        new HPanel(
          new HPanel(
            items
          ),
          ViewJumpHlp.JumpBar("/novosti", allNewsIds.Length, newsCountOnPage, pageNumber)
        )
      );
    }

    public static IHtmlControl GetCommentElement(int commentCount, string newsUrl)
    {
      IHtmlControl innerElement = GetInnerCommentElement(commentCount, newsUrl);

      return new HPanel(
        new HLabel("(").MarginLeft(5),
        innerElement,
        new HLabel(")"),
        new HLink(string.Format("{0}#bottom", newsUrl),
          "", new HBefore().ContentIcon(5, 10).BackgroundImage(UrlHlp.ImageUrl("bottom.png")).VAlign(-2)
        ).PaddingLeft(3).PaddingRight(3).MarginLeft(5).Title("к последним комментариям").Hide(commentCount < 4)
      ).InlineBlock();
    }

    static IHtmlControl GetInnerCommentElement(int commentCount, string newsUrl)
    {
      HBefore before = new HBefore().ContentIcon(9, 11).BackgroundImage(UrlHlp.ImageUrl("page.gif"))
        .VAlign(-2).MarginRight(5);
      if (commentCount == 0)
        return new HLabel(commentCount, before).Color("#9c9c9c");

      return new HPanel(
        new HLink(string.Format("{0}#comments", newsUrl),
          commentCount.ToString(), before
        )
      ).InlineBlock();
    }

    public static IHtmlControl[] GetNewsItems(SiteState state, IEnumerable<LightHead> newsList)
    {
      List<IHtmlControl> items = new List<IHtmlControl>();

      DateTime prevTime = DateTime.MinValue;
      foreach (LightHead news in newsList)
      {
        DateTime localTime = (news.Get(ObjectType.ActFrom) ?? DateTime.UtcNow).ToLocalTime();
				DateTime currentLocalTime = DateTime.UtcNow.ToLocalTime();
				bool isFixed = localTime.Date > currentLocalTime.AddDays(1);

        if (prevTime.Date != localTime.Date)
        {
          prevTime = localTime.Date;

          if (!isFixed)
          {
						string dateFormat = localTime.Year == currentLocalTime.Year ?
							BasketballHlp.shortDateFormat : BasketballHlp.longDateFormat;

            items.Add(
              new HPanel(
                new HLabel(localTime.ToString(dateFormat)).FontBold()
              ).Padding(1).MarginTop(15).MarginBottom(4)
            );
          }
          else
          {
            items.Add(
              new HPanel().Height(15)
            );
          }
        }

        int commentCount = context.NewsStorages.ForTopic(news.Id).MessageLink.AllRows.Length;
        string newsUrl = UrlHlp.ShopUrl("news", news.Id);

        IHtmlControl commentElement = GetCommentElement(commentCount, newsUrl);

        items.Add(
          new HPanel(
            new HLabel(localTime.ToString("HH:mm")).MarginLeft(5).MarginRight(10),
            new HLink(newsUrl,
              news.Get(NewsType.Title)
            ).FontBold(isFixed),
            commentElement
          ).FontSize("90%").Padding(1)
        );
      }

      return items.ToArray();
    }

    public static IHtmlControl GetNewsView(SiteState state, LightObject currentUser, TopicStorage topic,
      out string description)
    {
      LightObject news = topic.Topic;

      description = BasketballHlp.GetDescriptionForNews(news);

      DateTime localTime = (news.Get(ObjectType.ActFrom) ?? DateTime.UtcNow).ToLocalTime();
      int publisherId = news.Get(NewsType.PublisherId);
      LightObject publisher = context.UserStorage.FindUser(publisherId);

      IHtmlControl editPanel = null;
      if (currentUser != null && (currentUser.Id == publisherId || currentUser.Get(BasketballUserType.IsModerator)))
        editPanel = ViewNewsHlp.GetNewsEditPanel(state, topic);

      IHtmlControl moderatorPanel = GetModeratorPanel(state, currentUser, topic, localTime);

      string originName = news.Get(NewsType.OriginName);
      string originUrl = news.Get(NewsType.OriginUrl);
      if (StringHlp.IsEmpty(originUrl) && !StringHlp.IsEmpty(originName))
        originUrl = "https://" + originName;

			return new HPanel(
        Decor.Title(news.Get(NewsType.Title)),
        new HLabel(localTime.ToString(Decor.timeFormat)).Block().FontBold(),
        new HTextView(news.Get(NewsType.Text)).PositionRelative().Overflow("hidden"),
        new HPanel(
          new HLabel("Добавил:").MarginRight(5),
          new HLink(UrlHlp.ShopUrl("user", publisherId), publisher?.Get(UserType.Login))
        ),
        new HLink(originUrl, originName),
        ViewTagHlp.GetViewTagsPanel(context.Tags.TagBox, topic.Topic),
        editPanel,
        moderatorPanel,
        ViewCommentHlp.GetCommentsPanel(context.MessageConnection, state, currentUser, topic, topic.MessageLink.AllRows)
      );
    }

    static IHtmlControl GetModeratorPanel(SiteState state, 
      LightObject currentUser, TopicStorage topic, DateTime localTime)
    {
      bool isModerator = currentUser != null && currentUser.Get(BasketballUserType.IsModerator);

      if (!state.EditMode && !state.ModeratorMode)
        return null;

      return new HPanel(
        new HTextEdit("newsDate", localTime.ToString(Decor.timeFormat))
          .Width(100).MarginRight(10).FontFamily("Tahoma").FontSize(12),
        Decor.Button("Изменить дату").Event("news_date_modify", "newsAdminContainer",
          delegate (JsonData json)
          {
            string dateStr = json.GetText("newsDate");

            WebOperation operation = state.Operation;

            if (!operation.Validate(dateStr, "Дата не задана"))
              return;
            DateTime editDate;
            if (!operation.Validate(!DateTime.TryParse(dateStr, out editDate), "Неверный формат даты"))
              return;

            LightObject editNews = DataBox.LoadObject(context.FabricConnection, NewsType.News, topic.TopicId);
            editNews.Set(ObjectType.ActFrom, editDate.ToUniversalTime());
            editNews.Set(ObjectType.ActTill, DateTime.UtcNow);

            editNews.Box.Update();

            context.UpdateNews();
            context.NewsStorages.ForTopic(topic.TopicId).UpdateTopic();
          }
        )
      ).EditContainer("newsAdminContainer");
    }

    static IHtmlControl GetNewsEditPanel(SiteState state, TopicStorage topic)
    {
      LightObject news = topic.Topic;

      string blockHint = string.Format("news_edit_{0}", news.Id);

      IHtmlControl redoPanel = null;
      if (state.BlockHint == blockHint)
      {
        IHtmlControl deletePanel = null;
        if (state.ModeratorMode)
        {
          deletePanel = DeleteTopicPanel(state, topic);
        }

        if (state.Tag == null)
          state.Tag = ViewTagHlp.GetTopicDisplayTags(context.Tags.TagBox, topic.Topic);

        redoPanel = new HPanel(
          deletePanel,
          Decor.PropertyEdit("newsTitle", "Заголовок новости", news.Get(NewsType.Title)),
          new HPanel(
            HtmlHlp.CKEditorCreate("newsText", news.Get(NewsType.Text), "300px", true)
          ),
          ViewTagHlp.GetEditTagsPanel(state, context.Tags.TagBox, state.Tag as List<string>, false),
          Decor.PropertyEdit("newsOriginName", "Источник", news.Get(NewsType.OriginName)),
          Decor.PropertyEdit("newsOriginUrl", "Ссылка", news.Get(NewsType.OriginUrl)),
          Decor.Button("Изменить новость").CKEditorOnUpdateAll().MarginTop(10)
            .Event("save_news_edit", "editNewsData",
              delegate (JsonData json)
              {
                string title = json.GetText("newsTitle");
                string text = json.GetText("newsText");
                string originName = json.GetText("newsOriginName");
                string originUrl = json.GetText("newsOriginUrl");

                WebOperation operation = state.Operation;

                if (!operation.Validate(title, "Не задан заголовок"))
                  return;
                if (!operation.Validate(text, "Не задан текст"))
                  return;

                LightKin editNews = DataBox.LoadKin(context.FabricConnection, NewsType.News, news.Id);

                editNews.SetWithoutCheck(NewsType.Title, title);

                editNews.Set(NewsType.Text, text);
                editNews.Set(NewsType.OriginName, originName);
                editNews.Set(NewsType.OriginUrl, originUrl);

                editNews.Set(ObjectType.ActTill, DateTime.UtcNow);

                ViewTagHlp.SaveTags(context, state.Tag as List<string>, editNews);

                editNews.Box.Update();

                context.UpdateNews();
                context.NewsStorages.ForTopic(news.Id).UpdateTopic();

                state.BlockHint = "";
              },
              news.Id
            )
        ).EditContainer("editNewsData")
          .Padding(5, 10).MarginTop(10).Background(Decor.pageBackground);
      }

      return new HPanel(
        Decor.Button("Редактировать")
          .Event("news_edit", "", delegate
            {
              state.SetBlockHint(blockHint);
            }
          ),
        redoPanel
      ).MarginTop(10);
    }

    public static IHtmlControl DeleteTopicPanel(SiteState state, TopicStorage topic)
    {
      return new HPanel(
        Decor.Button("Удалить").Event("delete_topic", "", delegate
          {
            int messageCount = topic.MessageLink.AllRows.Length;
            if (!state.Operation.Validate(messageCount > 0, "Новость с комментариями не может быть удалена"))
              return;

            MessageHlp.DeleteTopicMessages(context.MessageConnection, topic.Topic.Id);
            BasketballHlp.DeleteTopic(context.FabricConnection, topic.TopicId);

            topic.UpdateTopic();
            topic.UpdateMessages();
            context.UpdateLastComments(false);
            context.UpdateNews();
            context.UpdateArticles();

            state.RedirectUrl = "/";
          }
        )
      ).Align(false);
    }
  }
}