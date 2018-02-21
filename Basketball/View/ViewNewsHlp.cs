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
      string editHint = "news_add";
      if (state.BlockHint == editHint)
      {
        editBlock = new HPanel(
          Decor.PropertyEdit("addNewsTitle", "Заголовок новости"),
          HtmlHlp.CKEditorCreate("addNewsText", "", "300px", true),
          Decor.PropertyEdit("addNewsOriginName", "Источник"),
          Decor.PropertyEdit("addNewsOriginUrl", "Ссылка"),
          Decor.Button("Добавить новость").CKEditorOnUpdateAll().MarginTop(10)
            .Event("save_news_add", "addNewsData",
              delegate (JsonData json)
              {
                string title = json.GetText("addNewsTitle");
                string text = json.GetText("addNewsText");
                string originName = json.GetText("addNewsOriginName");
                string originUrl = json.GetText("addNewsOriginUrl");

                WebOperation operation = state.Operation;

                if (!operation.Validate(title, "Не задан заголовок"))
                  return;

                if (!operation.Validate(text, "Не задан текст"))
                  return;

                ObjectBox editBox = new ObjectBox(context.FabricConnection, "1=0");

                int addNewsId = editBox.CreateObject(NewsType.News, NewsType.Title.CreateXmlIds(title), DateTime.UtcNow);
                LightObject editNews = new LightObject(editBox, addNewsId);

                editNews.Set(NewsType.PublisherId, currentUser.Id);
                editNews.Set(NewsType.Text, text);
                editNews.Set(NewsType.OriginName, originName);
                editNews.Set(NewsType.OriginUrl, originUrl);

                editBox.Update();

                context.UpdateNews();

                state.BlockHint = "";
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

    public static IHtmlControl GetNewsListView(SiteState state, LightObject currentUser, int pageNumber)
    {
      int[] allNewsIds = context.News.AllObjectIds;
      int pageCount = BinaryHlp.RoundUp(allNewsIds.Length, newsCountOnPage);
      int curPos = pageNumber * newsCountOnPage;
      if (curPos < 0 || curPos >= allNewsIds.Length)
        return new HPanel();

      int[] newsIds = ArrayHlp.GetRange(allNewsIds, curPos, Math.Min(newsCountOnPage, allNewsIds.Length - curPos));
      LightHead[] newsList = ArrayHlp.Convert(newsIds, delegate (int id)
      { return new LightHead(context.News, id); }
      );

      IHtmlControl[] items = GetNewsItems(state, newsList);

      return new HPanel(
        Decor.Title("Новости").MarginBottom(0),
        new HPanel(
          new HPanel(
            items
          ),
          ViewJumpHlp.JumpBar("novosti", context.News.AllObjectIds.Length, newsCountOnPage, pageNumber)
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
        bool isFixed = localTime.Date > DateTime.UtcNow.ToLocalTime();

        if (prevTime.Date != localTime.Date)
        {
          prevTime = localTime.Date;

          if (!isFixed)
          {
            items.Add(
              new HPanel(
                new HLabel(localTime.ToString("dd MMMM")).FontBold()
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

    public static IHtmlControl GetNewsView(SiteState state, LightObject currentUser, TopicStorage topic)
    {
      LightObject news = topic.Topic;

      DateTime localTime = (news.Get(ObjectType.ActFrom) ?? DateTime.UtcNow).ToLocalTime();
      int publisherId = news.Get(NewsType.PublisherId);
      LightObject publisher = context.UserStorage.FindUser(publisherId);

      IHtmlControl editPanel = null;
      if (currentUser != null && (currentUser.Id == publisherId || currentUser.Get(BasketballUserType.IsModerator)))
        editPanel = ViewNewsHlp.GetNewsEditPanel(state, topic);

      IHtmlControl moderatorPanel = GetModeratorPanel(state, currentUser, topic, localTime);

      return new HPanel(
        Decor.Title(news.Get(NewsType.Title)),
        new HLabel(localTime.ToString(Decor.timeFormat)).Block().FontBold(),
        new HTextView(news.Get(NewsType.Text)),
        new HPanel(
          new HLabel("Добавил:").MarginRight(5),
          new HLink(UrlHlp.ShopUrl("user", publisherId), publisher?.Get(UserType.Login))
        ),
        new HLink(news.Get(NewsType.OriginUrl), news.Get(NewsType.OriginName)),
        editPanel,
        moderatorPanel,
        ViewCommentHlp.GetCommentsPanel(context.MessageConnection, state, currentUser, topic)
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

        redoPanel = new HPanel(
          deletePanel,
          Decor.PropertyEdit("editNewsTitle", "Заголовок новости", news.Get(NewsType.Title)),
          HtmlHlp.CKEditorCreate("editNewsText", news.Get(NewsType.Text), "300px", true),
          Decor.PropertyEdit("editNewsOriginName", "Источник", news.Get(NewsType.OriginName)),
          Decor.PropertyEdit("editNewsOriginUrl", "Ссылка", news.Get(NewsType.OriginUrl)),
          Decor.Button("Изменить новость").CKEditorOnUpdateAll().MarginTop(10)
            .Event("save_news_edit", "editNewsData",
              delegate (JsonData json)
              {
                string title = json.GetText("editNewsTitle");
                string text = json.GetText("editNewsText");
                string originName = json.GetText("editNewsOriginName");
                string originUrl = json.GetText("editNewsOriginUrl");

                WebOperation operation = state.Operation;

                if (!operation.Validate(title, "Не задан заголовок"))
                  return;
                if (!operation.Validate(text, "Не задан текст"))
                  return;

                LightObject editNews = DataBox.LoadObject(context.FabricConnection, NewsType.News, news.Id);

                editNews.SetWithoutCheck(NewsType.Title, title);

                editNews.Set(NewsType.Text, text);
                editNews.Set(NewsType.OriginName, originName);
                editNews.Set(NewsType.OriginUrl, originUrl);

                editNews.Set(ObjectType.ActTill, DateTime.UtcNow);

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
      ).MarginTop(5);
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
            DataBox.DeleteParentObject(context.FabricConnection, topic.Topic.Id);

            topic.UpdateTopic();
            topic.UpdateMessages();
            context.UpdateLastComments(false);
            context.UpdateNews();
            context.UpdateArticles();
          }
        )
      ).Align(false);
    }
  }
}