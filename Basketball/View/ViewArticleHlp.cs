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
  public class ViewArticleHlp
  {
    static BasketballContext context
    {
      get { return (BasketballContext)SiteContext.Default; }
    }

    const int articleCountOnPage = 20;

    public static IHtmlControl GetActualArticleBlock(SiteState state, LightObject currentUser)
    {
      IHtmlControl[] items = ViewArticleHlp.GetArticleItems(state, context.ActualArticles);

      HPanel editBlock = null;
      if (state.BlockHint == "articleAdd")
      {
        editBlock = new HPanel(
          Decor.PropertyEdit("addArticleTitle", "Заголовок статьи"),
          Decor.Button("Добавить статью").MarginTop(10)
            .Event("article_add_save", "addArticleData",
              delegate (JsonData json)
              {
                string title = json.GetText("addArticleTitle");

                WebOperation operation = state.Operation;

                if (!operation.Validate(title, "Не задан заголовок"))
                  return;

                ObjectBox editBox = new ObjectBox(context.FabricConnection, "1=0");

                int addArticleId = editBox.CreateObject(
                  ArticleType.Article, ArticleType.Title.CreateXmlIds(title), DateTime.UtcNow
                );
                LightObject editArticle = new LightObject(editBox, addArticleId);

                editArticle.Set(ArticleType.PublisherId, currentUser.Id);

                editBox.Update();
                context.UpdateArticles();

                state.BlockHint = "";

                state.RedirectUrl = UrlHlp.ShopUrl("article", addArticleId);
              }
            )
        ).EditContainer("addArticleData")
          .Padding(5, 10).MarginTop(10).Background(Decor.pageBackground);
      }

      HButton addButton = null;
      if (currentUser != null && !BasketballHlp.NoRedactor(currentUser))
      {
        addButton = Decor.Button("Добавить").Hide(currentUser == null).MarginLeft(10)
          .Event("article_add", "", delegate
            {
              state.SetBlockHint("articleAdd");
            }
          );
      }

      return new HPanel(
        Decor.Subtitle("Статьи").MarginBottom(12),
        new HPanel(
          items.ToArray()
        ),
        new HPanel(
          new HLink("/stati",
            "Все статьи",
            new HBefore().ContentIcon(5, 12).BackgroundImage(UrlHlp.ImageUrl("pointer.gif")).MarginRight(5).VAlign(-2)
          ).FontBold(),
          addButton
        ).MarginTop(15),
        editBlock
      ).MarginTop(10);
    }

    public static IHtmlControl GetArticleListView(SiteState state, LightObject currentUser, int pageNumber)
    {
      int[] allArticleIds = context.Articles.AllObjectIds;
      int pageCount = BinaryHlp.RoundUp(allArticleIds.Length, articleCountOnPage);
      int curPos = pageNumber * articleCountOnPage;
      if (curPos < 0 || curPos >= allArticleIds.Length)
        return new HPanel();

      int[] articleIds = ArrayHlp.GetRange(allArticleIds, curPos, 
        Math.Min(articleCountOnPage, allArticleIds.Length - curPos)
      );
      LightObject[] articleList = ArrayHlp.Convert(articleIds, delegate (int id)
      { return new LightObject(context.Articles, id); }
      );

      IHtmlControl[] items = GetArticleItems(state, articleList);

      return new HPanel(
        Decor.Title("Статьи").MarginBottom(15),
        new HPanel(
          new HPanel(
            items
          ),
          ViewJumpHlp.JumpBar("/stati", context.Articles.AllObjectIds.Length, articleCountOnPage, pageNumber)
        )
      );
    }

    public static IHtmlControl[] GetArticleItems(SiteState state, IEnumerable<LightObject> articleList)
    {
      List<IHtmlControl> items = new List<IHtmlControl>();

      foreach (LightObject article in articleList)
      {
        DateTime localTime = (article.Get(ObjectType.ActFrom) ?? DateTime.UtcNow).ToLocalTime();

        int commentCount = context.ArticleStorages.ForTopic(article.Id).MessageLink.AllRows.Length;
        string articleUrl = UrlHlp.ShopUrl("article", article.Id);
        string imageUrl = UrlHlp.ImageUrl(article.Id, false);

        string annotationWithLink = string.Format(
          "{0} <a href='{1}'><img src='/images/full.gif'></img></a>",
          article.Get(ArticleType.Annotation), articleUrl
        );

        items.Add(
          new HPanel(
            new HPanel(
              new HLink(articleUrl,
                article.Get(NewsType.Title)
              ).FontSize("14.4px").FontBold(),
              ViewNewsHlp.GetCommentElement(commentCount, articleUrl)
            ).FontSize("90%"),
            new HXPanel(
              new HPanel(
                new HPanel().InlineBlock().Size(Decor.ArticleThumbWidth, Decor.ArticleThumbHeight)
                  .Background(imageUrl, "no-repeat", "center").CssAttribute("background-size", "100%")
                  .MarginTop(2).MarginBottom(5)
              ),
              new HPanel(
                new HPanel(
                  new HLabel(article.Get(ArticleType.OriginName)),
                  new HLabel("//").MarginLeft(5).MarginRight(5),
                  new HLabel(localTime.ToString("dd MMMM yyyy"))
                ).FontSize("90%").MarginBottom(7).MarginTop(2),
                new HTextView(
                  annotationWithLink
                )
              ).PaddingLeft(20)
            )
          ).Padding(1).BorderBottom("1px solid #e0dede").MarginBottom(5)
        );
      }

      return items.ToArray();
    }

    public static IHtmlControl GetArticleView(SiteState state, LightObject currentUser, TopicStorage topic)
    {
      if (topic == null || topic.Topic == null)
        return null;

      LightObject article = topic.Topic;

      DateTime localTime = (article.Get(ObjectType.ActFrom) ?? DateTime.UtcNow).ToLocalTime();
      int publisherId = article.Get(NewsType.PublisherId);
      LightObject publisher = context.UserStorage.FindUser(publisherId);

      IHtmlControl editPanel = null;
      if (currentUser != null && (currentUser.Id == publisherId || currentUser.Get(BasketballUserType.IsModerator)))
        editPanel = ViewArticleHlp.GetArticleEditPanel(state, topic);

      string author = article.Get(ArticleType.Author);
      int commentCount = topic.MessageLink.AllRows.Length;
      string articleUrl = UrlHlp.ShopUrl("article", article.Id);

      return new HPanel(
        Decor.Title(article.Get(NewsType.Title)),
        new HPanel(
          new HLabel(string.Format("{0},", author)).FontBold().MarginRight(5).Hide(StringHlp.IsEmpty(author)),
          new HLabel(article.Get(ArticleType.OriginName))
            .FontBold().MarginRight(5),
          new HLabel(string.Format("| {0}", localTime.ToString("dd MMMM yyyy")))
        ).FontSize("90%"),
        new HPanel(
          new HLabel("Комментарии:").Color(Decor.minorColor),
          ViewNewsHlp.GetCommentElement(commentCount, articleUrl)
        ).FontSize("90%"),
        //new HLabel(localTime.ToString(Decor.timeFormat)).Block().FontBold(),
        new HTextView(article.Get(NewsType.Text)),
        new HLabel("Автор:").MarginRight(5).Hide(StringHlp.IsEmpty(author)), 
        new HLabel(article.Get(ArticleType.Author)).FontBold().MarginRight(5).Hide(StringHlp.IsEmpty(author)),
        new HLabel("|").MarginRight(5).Hide(StringHlp.IsEmpty(author)),
        new HLink(article.Get(NewsType.OriginUrl), article.Get(NewsType.OriginName)),
        new HPanel(
          new HLabel("Добавил:").MarginRight(5),
          new HLink(UrlHlp.ShopUrl("user", publisherId), publisher?.Get(UserType.Login))
        ).MarginTop(5),
        editPanel,
        ViewCommentHlp.GetCommentsPanel(context.MessageConnection, state, currentUser, topic)
      );
    }

    static IHtmlControl GetArticleEditPanel(SiteState state, TopicStorage topic)
    {
      LightObject article = topic.Topic;

      string blockHint = string.Format("article_edit_{0}", article.Id);

      IHtmlControl redoPanel = null;
      if (state.BlockHint == blockHint)
      {
        IHtmlControl deletePanel = null;
        if (state.ModeratorMode)
        {
          deletePanel = ViewNewsHlp.DeleteTopicPanel(state, topic);
        }

        redoPanel = new HPanel(
          deletePanel,
          new HPanel(
            Decor.PropertyEdit("editArticleTitle", "Заголовок статьи", article.Get(NewsType.Title)),
            new HPanel(
              new HLabel("Аннотация").FontBold(),
              new HTextArea("editArticleAnnotation", article.Get(NewsType.Annotation))
                .Width("100%").Height("4em").MarginBottom(5)
            ),
            HtmlHlp.CKEditorCreate("editArticleText", article.Get(NewsType.Text), "300px", true),
            Decor.PropertyEdit("editArticleAuthor", "Автор", article.Get(ArticleType.Author)),
            Decor.PropertyEdit("editArticleOriginName", "Источник (без префикса http://)", article.Get(NewsType.OriginName)),
            Decor.PropertyEdit("editArticleOriginUrl", "Ссылка (с префиксом http://)", article.Get(NewsType.OriginUrl)),
            Decor.Button("Сохранить статью").CKEditorOnUpdateAll().MarginTop(10).MarginBottom(10)
              .Event("save_article_edit", "editArticleData",
                delegate (JsonData json)
                {
                  string title = json.GetText("editArticleTitle");
                  string annotation = json.GetText("editArticleAnnotation");
                  string text = json.GetText("editArticleText");
                  string author = json.GetText("editArticleAuthor");
                  string originName = json.GetText("editArticleOriginName");
                  string originUrl = json.GetText("editArticleOriginUrl");

                  WebOperation operation = state.Operation;

                  if (!operation.Validate(title, "Не задан заголовок"))
                    return;

                  if (!operation.Validate(text, "Не задан текст"))
                    return;

                  LightObject editNews = DataBox.LoadObject(context.FabricConnection, ArticleType.Article, article.Id);

                  editNews.SetWithoutCheck(NewsType.Title, title);

                  editNews.Set(NewsType.Annotation, annotation);
                  editNews.Set(NewsType.Text, text);
                  editNews.Set(ArticleType.Author, author);
                  editNews.Set(NewsType.OriginName, originName);
                  editNews.Set(NewsType.OriginUrl, originUrl);

                  editNews.Set(ObjectType.ActTill, DateTime.UtcNow);

                  editNews.Box.Update();

                  context.UpdateArticles();
                  context.ArticleStorages.ForTopic(article.Id).UpdateTopic();

                  state.BlockHint = "";
                },
                article.Id
              )
          ).EditContainer("editArticleData"),
          new HPanel(
            EditElementHlp.GetImageThumb(article.Id)
          ),
          new HPanel(
            EditElementHlp.GetDescriptionImagesPanel(article.Id)
          )
        ).Padding(5, 10).MarginTop(10).Background(Decor.pageBackground);
      }

      return new HPanel(
        Decor.Button("Редактировать")
          .Event("article_edit", "", delegate
            {
              state.SetBlockHint(blockHint);
            }
          ),
        redoPanel
      ).MarginTop(5);
    }
  }
}