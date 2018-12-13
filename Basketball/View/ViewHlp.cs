using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Commune.Basis;
using Commune.Html;
using Commune.Data;
using Shop.Engine;
using NitroBolt.Wui;
using System.Net.Mail;

namespace Basketball
{
  public class ViewHlp
  {
    static BasketballContext context
    {
      get { return (BasketballContext)SiteContext.Default; }
    }

    static IStore store
    {
      get
      {
        return SiteContext.Default.Store;
      }
    }

    public static IHtmlControl GetCenter(HttpContext httpContext, SiteState state, 
      LightObject currentUser, string kind, int? id, 
      out string title, out string description, out SchemaOrg schema)
    {
      title = "";
      description = "";
      schema = null;

      SiteSettings settings = context.SiteSettings;

      switch (kind)
      {
        case "":
          {
            title = store.SEO.Get(SEOType.MainTitle);
            description = store.SEO.Get(SEOType.MainDescription);
            LightSection main = store.Sections.FindMenu("main");
            return ViewHlp.GetMainView(state, currentUser);
          }
        case "news":
          {
            if (!context.News.ObjectById.Exist(id))
              return null;
            TopicStorage topicStorage = context.NewsStorages.ForTopic(id ?? 0);
            LightKin topic = topicStorage.Topic;
            //string tagsDisplay;
            IHtmlControl view = ViewNewsHlp.GetNewsView(state, currentUser, topicStorage, out description);

            title = topic.Get(NewsType.Title);

            //string postfix = "";
            //if (!StringHlp.IsEmpty(tagsDisplay))
            //  postfix = ". ";
            //description = string.Format("{0}{1}Живое обсуждение баскетбольных событий на basketball.ru.com",
            //  tagsDisplay, postfix
            //);

            string logoUrl = settings.FullUrl("/images/logo.gif");
            schema = new SchemaOrg("NewsArticle", settings.FullUrl(UrlHlp.ShopUrl("news", id)),
              title, new string[] { logoUrl }, topic.Get(ObjectType.ActFrom), topic.Get(ObjectType.ActTill),
              topic.Get(TopicType.OriginName), settings.Organization,
              logoUrl, description
            );

            return view;
          }
        case "article":
          {
            if (!context.Articles.ObjectById.Exist(id))
              return null;
            TopicStorage topicStorage = context.ArticleStorages.ForTopic(id ?? 0);
            LightKin topic = topicStorage.Topic;
            title = topic.Get(ArticleType.Title);
            description = topic.Get(ArticleType.Annotation);

            string logoUrl = settings.FullUrl("/images/logo.gif");
            schema = new SchemaOrg("Article", settings.FullUrl(UrlHlp.ShopUrl("article", id)),
              title, new string[] { logoUrl }, topic.Get(ObjectType.ActFrom), topic.Get(ObjectType.ActTill),
              topic.Get(TopicType.OriginName), settings.Organization,
              logoUrl, description
            );
            return ViewArticleHlp.GetArticleView(state, currentUser, topicStorage);
          }
        case "topic":
          {
            TopicStorage topic = context.Forum.TopicsStorages.ForTopic(id ?? 0);
            title = topic.Topic.Get(TopicType.Title);

            int pageNumber = 0;
            {
              string pageArg = httpContext.Get("page");
              int messageCount = topic.MessageLink.AllRows.Length;
              if (pageArg == "last" && messageCount > 0)
              {
                pageNumber = BinaryHlp.RoundUp(messageCount, ViewForumHlp.forumMessageCountOnPage) - 1;
              }
              else
              {
                pageNumber = ConvertHlp.ToInt(pageArg) ?? 0;
              }
            }

            return ViewForumHlp.GetTopicView(state, currentUser, topic, pageNumber);
          }
        case "tags":
          {
            int? tagId = httpContext.GetUInt("tag");
            int pageNumber = httpContext.GetUInt("page") ?? 0;
            return ViewNewsHlp.GetTagListView(state, currentUser, tagId, pageNumber, out title, out description);
          }
        case "user":
          {
            LightObject user = context.UserStorage.FindUser(id ?? -1);
            if (user == null)
              return null;
            title = string.Format("{0} - Basketball.ru.com", user.Get(UserType.Login));
            return ViewUserHlp.GetUserView(state, currentUser, user);
          }
        case "page":
          {
            LightSection section = store.Sections.FindSection(id);
            title = FabricHlp.GetSeoTitle(section, section.Get(SectionType.Title));
            description = FabricHlp.GetSeoDescription(section, section.Get(SectionType.Annotation));

            int pageNumber = httpContext.GetUInt("page") ?? 0;
            string designKind = section.Get(SectionType.DesignKind);
            switch (designKind)
            {
              case "news":
                {
                  int[] allNewsIds = context.News.AllObjectIds;
                  return ViewNewsHlp.GetNewsListView(state, currentUser, pageNumber);
                }
              case "articles":
                return ViewArticleHlp.GetArticleListView(state, currentUser, pageNumber);
              case "forum":
                return ViewForumHlp.GetForumView(state, currentUser, section);
              case "forumSection":
                return ViewForumHlp.GetForumSectionView(state, currentUser, section);
              default:
                return null;
            }
          }
        case "dialog":
          {
            if (currentUser == null)
              return null;

            if (id == null)
            {
              return ViewDialogueHlp.GetDialogueView(state,
                context.ForumConnection, context.UserStorage, currentUser, out title
              );
            }

            LightObject collocutor = context.UserStorage.FindUser(id.Value);
            if (collocutor == null)
              return null;

            return ViewDialogueHlp.GetCorrespondenceView(state, context.ForumConnection,
              currentUser, collocutor, out title
            );
          }
        case "passwordreset":
          title = "Восстановление пароля - basketball.ru.com";
          return ViewHlp.GetRestorePasswordView(state);
        case "register":
          title = "Регистрация - basketball.ru.com";
          return ViewHlp.GetRegisterView(state);
        case "confirmation":
          {
            title = "Подтверждение аккаунта";

            int? userId = httpContext.GetUInt("id");
            string hash = httpContext.Get("hash");

            if (userId == null)
            {
              return ViewUserHlp.GetMessageView(
                "Вам выслано письмо с кодом активации. Чтобы завершить процедуру регистрации, пройдите по ссылке, указанной в письме, и учётная запись будет активирована. Если вы не получили письмо, то попробуйте войти на сайт со своим логином и паролем. Тогда письмо будет отправлено повторно."
              );
            }

            if (StringHlp.IsEmpty(hash))
              return null;

            LightObject user = context.UserStorage.FindUser(userId.Value);
            if (userId == null)
              return null;

            if (!user.Get(UserType.NotConfirmed))
            {
              return ViewUserHlp.GetMessageView("Пользователь успешно активирован");
            }

            string login = user.Get(UserType.Login);
            string etalon = UserHlp.CalcConfirmationCode(user.Id, login, "bbbin");
            if (hash?.ToLower() != etalon?.ToLower())
            {
              return ViewUserHlp.GetMessageView("Неверный хэш");
            }

            LightObject editUser = DataBox.LoadObject(context.UserConnection, UserType.User, user.Id);
            editUser.Set(UserType.NotConfirmed, false);
            editUser.Box.Update();

            context.UserStorage.Update();

            string xmlLogin = UserType.Login.CreateXmlIds("", login);
            HttpContext.Current.SetUserAndCookie(xmlLogin);

            state.RedirectUrl = "/";

            return new HPanel();
          }
      }

      return null;
    }

    public static IHtmlControl GetMainView(SiteState state, LightObject currentUser)
    {
      return new HPanel(
        ViewNewsHlp.GetActualNewsBlock(state, currentUser),
        ViewArticleHlp.GetActualArticleBlock(state, currentUser)
      ).PaddingLeft(15)
      .MediaSmartfon(new HStyle().PaddingLeft(5));
    }

    static readonly HBuilder h = null;

    public static IHtmlControl GetRestorePasswordView(SiteState state)
    {
      return new HPanel(
        Decor.Title("Восстановление пароля"),
        Decor.AuthEdit("login", "Введите логин:"),
        Decor.AuthEdit("email", "Или E-mail:"),
        new HPanel(
          Decor.Button("Выслать пароль на почту").Event("user_restore", "restoreData",
            delegate (JsonData json)
            {
              string login = json.GetText("login");
              string email = json.GetText("email");

              WebOperation operation = state.Operation;

              if (!operation.Validate(StringHlp.IsEmpty(login) && StringHlp.IsEmpty(email),
                "Введите логин или email"))
                return;

              LightObject findUser = null;
              if (!StringHlp.IsEmpty(login))
              {
                string xmlLogin = UserType.Login.CreateXmlIds("", login);
                findUser = context.UserStorage.FindUser(xmlLogin);
              }
              else
              {
                foreach (LightObject user in context.UserStorage.All)
                {
                  if (user.Get(BasketballUserType.Email) == email)
                  {
                    findUser = user;
                    break;
                  }
                }
              }

              if (!operation.Validate(findUser == null, "Пользователь не найден"))
                return;

              try
              {
                HElement answer = h.Div(
                  h.P(string.Format("Ваш логин: {0}", findUser.Get(BasketballUserType.Login))),
                  h.P(string.Format("Ваш пароль: {0}", findUser.Get(BasketballUserType.Password)))
                );

                SiteSettings settings = SiteContext.Default.SiteSettings;
                SmtpClient smtpClient = AuthHlp.CreateSmtpClient(
                  settings.SmtpHost, settings.SmtpPort, settings.SmtpUserName, settings.SmtpPassword);
                AuthHlp.SendMail(smtpClient, settings.MailFrom, findUser.Get(BasketballUserType.Email),
                  "Восстановление пароля", answer.ToHtmlText()
                );
              }
              catch (Exception ex)
              {
                Logger.WriteException(ex);

                operation.Validate(true, string.Format("Непредвиденная ошибка при отправке заявки: {0}", ex.Message));
                return;
              }

              operation.Message = "Пароль выслан вам на почту";
            }
          )
        )
      ).EditContainer("restoreData");
    }

    public static IHtmlControl GetRegisterView(SiteState state)
    {
      return new HPanel(
        Decor.Title("Регистрация"),
        Decor.AuthEdit("login", "Логин (*):"),
        Decor.AuthEdit("yourname", "Ваше имя (*):"),
        Decor.AuthEdit("email", "E-mail (*):"),
        Decor.AuthEdit(new HPasswordEdit("password"), "Пароль (*):"),
        Decor.AuthEdit(new HPasswordEdit("passwordRepeat"), "Введите пароль ещё раз (*):"),
        new HPanel(
          Decor.Button("Зарегистрироваться").Event("user_register", "registerData",
            delegate (JsonData json)
            {
              string login = json.GetText("login");
              string name = json.GetText("yourname");
              string email = json.GetText("email");
              string password = json.GetText("password");
              string passwordRepeat = json.GetText("passwordRepeat");

              WebOperation operation = state.Operation;

              if (!operation.Validate(login, "Не задан логин"))
                return;
              if (!operation.Validate(email, "Не задана электронная почта"))
                return;
              if (!operation.Validate(!email.Contains("@"), "Некорректный адрес электронной почты"))
                return;
              if (!operation.Validate(name, "Не задано имя"))
                return;
              if (!operation.Validate(password, "Не задан пароль"))
                return;
              if (!operation.Validate(password != passwordRepeat, "Повтор не совпадает с паролем"))
                return;

              foreach (LightObject userObj in context.UserStorage.All)
              {
                if (!operation.Validate(userObj.Get(UserType.Email)?.ToLower() == email?.ToLower(),
                  "Пользователь с такой электронной почтой уже существует"))
                  return;
              }

              ObjectBox box = new ObjectBox(context.UserConnection, "1=0");

              int? createUserId = box.CreateUniqueObject(UserType.User,
                UserType.Login.CreateXmlIds("", login), DateTime.UtcNow);
              if (!operation.Validate(createUserId == null,
                "Пользователь с таким логином уже существует"))
              {
                return;
              }

              LightObject user = new LightObject(box, createUserId.Value);
              FabricHlp.SetCreateTime(user);
              user.Set(UserType.Email, email);
              user.Set(UserType.FirstName, name);
              user.Set(UserType.Password, password);
              user.Set(UserType.NotConfirmed, true);

              box.Update();

              SiteContext.Default.UserStorage.Update();

              Logger.AddMessage("Зарегистрирован пользователь: {0}, {1}, {2}", user.Id, login, email);

              try
              {
                BasketballHlp.SendRegistrationConfirmation(user.Id, login, email);

                Logger.AddMessage("Отправлено письмо с подтверждением регистрации.");
              }
              catch (Exception ex)
              {
                Logger.WriteException(ex);

                //operation.Validate(true, string.Format("Непредвиденная ошибка при отправке подтверждения: {0}", ex.Message));
                //return;
              }

              //string xmlLogin = UserType.Login.CreateXmlIds("", login);
              //HttpContext.Current.SetUserAndCookie(xmlLogin);

              //operation.Complete("Вы успешно зарегистрированы!", "");

              state.RedirectUrl = "/confirmation";
            }
          )
        )
      ).EditContainer("registerData");
    }

    public static IHtmlControl GetFooterView(bool isMain)
    {
      return new HPanel(
        new HPanel(
          new HPanel(
            new HLabel("Создание сайта —").MarginRight(5).Color("#646565"),
            new HLink("http://webkrokus.ru", "webkrokus.ru").TargetBlank()
          ).PositionAbsolute().Top("50%").Left(0).MarginTop(-7),
          std.Upbutton(UrlHlp.ImageUrl("upbutton.png")).Right(20),
          HtmlHlp.UpbuttonScript(100, 26)
        ).PositionRelative().Height(46).PaddingRight(8).Align(false)
        //new HPanel(
        //  new HLink("#top",
        //    new HLabel("Наверх").FontBold(),
        //    new HBefore().ContentIcon(10, 15).BackgroundImage(UrlHlp.ImageUrl("footer_upstair.png"))
        //    .VAlign(-3).MarginRight(5)
        //  ),
        //  new HPanel(
        //    new HLabel("Создание сайта —").MarginRight(5).Color("#646565"),
        //    new HLink("http://webkrokus.ru", "КРОКУС").TargetBlank()
        //  ).PositionAbsolute().Top(0).Right(10)
        //    .MediaSmartfon(new HStyle().Right(5))
        //).PositionRelative().WidthLimit("", isMain ? "930px" : "1020px")
      ).Align(true).Background(Decor.panelBackground)
        .MarginLeft(12).MarginRight(12).PaddingLeft(25).PaddingBottom(10)
        .MediaTablet(new HStyle().PaddingLeft(10).MarginLeft(0).MarginRight(0));

      //return new HPanel(
      //  new HPanel(
      //    new HLabel("Создание сайта –").Block(),
      //    new HPanel(
      //      new HLabel("web-студия").MarginRight(5),
      //      new HLabel("Крокус")
      //    )
      //  ).InlineBlock().Align(true).MarginTop(10).MarginRight(30)
      //).Align(false).Height(60).PositionRelative().Background(Decor.menuBackground)
      //.Color(Decor.menuColor).LineHeight(16);
    }
  }
}