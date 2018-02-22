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
            title = topic.Get(NewsType.Title);

            string logoUrl = settings.FullUrl("/images/logo.gif");
            schema = new SchemaOrg("NewsArticle", settings.FullUrl(UrlHlp.ShopUrl("news", id)),
              title, new string[] { logoUrl } , topic.Get(ObjectType.ActFrom), topic.Get(ObjectType.ActTill),
              topic.Get(TopicType.OriginName), settings.Organization,
              logoUrl, ""
            );
            return ViewNewsHlp.GetNewsView(state, currentUser, topicStorage);
          }
        case "article":
          {
            if (!context.Articles.ObjectById.Exist(id))
              return null;
            TopicStorage topic = context.ArticleStorages.ForTopic(id ?? 0);
            title = topic.Topic.Get(ArticleType.Title);
            return ViewArticleHlp.GetArticleView(state, currentUser, topic);
          }
        case "topic":
          {
            TopicStorage topic = context.Forum.TopicsStorages.ForTopic(id ?? 0);
            title = topic.Topic.Get(TopicType.Title);
            return ViewForumHlp.GetTopicView(state, currentUser, topic);
          }
        case "tags":
          {
            int? tagId = httpContext.GetUInt("tag");
            int pageNumber = httpContext.GetUInt("page") ?? 0;
            return ViewNewsHlp.GetTagListView(state, currentUser, tagId, pageNumber);
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
        case "passwordreset":
          title = "Восстановление пароля - basketball.ru.com";
          return ViewHlp.GetRestorePasswordView(state);
        case "register":
          title = "Регистрация - basketball.ru.com";
          return ViewHlp.GetRegisterView(state);
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

              box.Update();

              SiteContext.Default.UserStorage.Update();

              string xmlLogin = UserType.Login.CreateXmlIds("", login);
              HttpContext.Current.SetUserAndCookie(xmlLogin);

              //operation.Complete("Вы успешно зарегистрированы!", "");

              state.RedirectUrl = "/";
            }
          )
        )
      ).EditContainer("registerData");
    }
  }
}