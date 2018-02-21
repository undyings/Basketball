using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Commune.Basis;
using Commune.Html;
using Commune.Data;
using Shop.Engine;
using NitroBolt.Wui;

namespace Basketball
{
  public class ViewHeaderHlp
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

    public static IHtmlControl GetHeader(HttpContext httpContext, SiteState state,
      LightObject currentUser, string kind, int? id, bool isForum)
    {
      return new HPanel(
        new HPanel(
          new HLink("/",
            new HPanel(
              new HImage(UrlHlp.ImageUrl("logo.gif"))
                .MediaTablet(new HStyle().Display("none")),
              new HImage(UrlHlp.ImageUrl("ball.jpg")).Display("none")
                .MediaTablet(new HStyle().InlineBlock())
            )
          ).PositionAbsolute().Top(11).Left(15)
            .MediaSmartfon(new HStyle().Left(5)),
          ViewHeaderHlp.GetAuthBlock(httpContext, state, currentUser)
        ).PositionRelative().BoxSizing().Height(55).Align(false)
        .Padding(11, 15).MarginLeft(12).MarginRight(12)
          .Background(Decor.panelBackground)
          .MediaTablet(new HStyle().MarginLeft(0).MarginRight(0))
          .MediaSmartfon(new HStyle().PaddingLeft(5).PaddingRight(5)),
        ViewHeaderHlp.GetMenu(state, kind, id, isForum)
      );
    }

    public static IHtmlControl GetAuthBlock(HttpContext httpContext, SiteState state, LightObject currentUser)
    {
      HImage keyImage = new HImage(UrlHlp.ImageUrl("key.gif")).MarginRight(8).VAlign(false);

      if (currentUser == null)
      {
        return new HPanel(
          keyImage,
          new HTextEdit("authLogin").Width(90).MarginRight(5),
          new HPasswordEdit("authPassword").Width(90).MarginRight(5),
          Decor.Button("Войти").Event("user_login", "loginData", delegate (JsonData json)
          {
            string login = json.GetText("authLogin");
            string password = json.GetText("authPassword");

            WebOperation operation = state.Operation;

            if (!operation.Validate(login, "Введите логин"))
              return;

            if (!operation.Validate(password, "Введите пароль"))
              return;

            string xmlLogin = UserType.Login.CreateXmlIds("", login);
            LightObject user = SiteContext.Default.UserStorage.FindUser(xmlLogin);
            if (!operation.Validate(user == null, "Логин не найден"))
              return;
            if (!operation.Validate(user.Get(UserType.Password) != password, "Неверный пароль"))
              return;

            if (!operation.Validate(BasketballHlp.IsBanned(user),
              string.Format("Вы заблокированы до {0} и не можете войти на сайт",
                user.Get(BasketballUserType.BannedUntil)?.ToLocalTime().ToString("dd-MM-yyyy HH:mm")
              )
            ))
              return;


            httpContext.SetUserAndCookie(xmlLogin);
          }
          ),
          new HPanel(
            new HPanel(new HLink("/register", "Регистрация"))
              .MediaSmartfon(new HStyle().InlineBlock()),
            new HPanel(new HLink("/passwordreset", "Забыли пароль"))
              .MediaSmartfon(new HStyle().InlineBlock().MarginLeft(10))
          ).Align(true).InlineBlock().MarginLeft(5).FontSize("80%").VAlign(false)
            .MediaSmartfon(new HStyle().Block().MarginLeft(18))
        ).EditContainer("loginData").PositionRelative().InlineBlock().MarginTop(10)
          .MediaTablet(new HStyle().MarginTop(5))
          .MediaSmartfon(new HStyle().MarginTop(0));
      }

      HButton moderatorButton = null;
      if (currentUser.Get(BasketballUserType.IsModerator))
      {
        moderatorButton = new HButton("",
          std.BeforeAwesome(@"\f1e2", 0)
        ).Title("Режим модерирования").MarginRight(3).MarginLeft(5).FontSize(14)
        .Color(state.ModeratorMode ? Decor.redColor : Decor.disabledColor)
        .Event("moderator_mode_set", "", delegate
        {
          state.ModeratorMode = !state.ModeratorMode;
        });
      }

      return new HPanel(
        keyImage,
        new HLabel("Здравствуйте,").MarginRight(5),
        new HLink(UrlHlp.ShopUrl("user", currentUser.Id), currentUser.Get(UserType.FirstName)).FontBold(),
        new HLabel("!"),
        moderatorButton,
        Decor.Button("Выйти").MarginLeft(5).Event("user_logout", "", delegate (JsonData json)
        {
          httpContext.Logout();
        }
        )
      ).InlineBlock().MarginTop(10);
    }

    public static IHtmlControl GetMenu(SiteState state, string kind, int? id, bool isForum)
    {
      LightSection main = store.Sections.FindMenu("main");

      List<IHtmlControl> items = new List<IHtmlControl>();
      foreach (LightSection section in main.Subsections)
      {
        bool isSelected = kind == "page" && id == section.Id;
        string designKind = section.Get(SectionType.DesignKind);
        if (designKind == "news" && kind == "news")
          isSelected = true;
        else if (designKind == "articles" && kind == "article")
          isSelected = true;
        else if (designKind == "forum" && isForum)
          isSelected = true;
        //else if (designKind == "forum" && kind == "topic")
        //  isSelected = true;
        //else if (designKind == "forum" && kind == "page")
        //{
        //  LightSection pageSection = store.Sections.FindSection(id);
        //  if (pageSection != null && pageSection.Get(SectionType.DesignKind) == "forumSection")
        //    isSelected = true;
        //}

        items.Add(
          ViewHeaderHlp.GetMenuItem(state, section, isSelected)
        );
      }

      if (state.EditMode)
      {
        items.Add(DecorEdit.AdminGroupPanel(true, main.Id));
        //items.Add(
        //  new HPanel(
        //    DecorEdit.AddIconButton(true, UrlHlp.EditUrl(main.Id, "page", null)),
        //    DecorEdit.SortIconButton(true, UrlHlp.EditUrl(main.Id, "sorting_section", null))
        //  ).InlineBlock().Background(Decor.pageBackground)
        //);
      }

      return new HPanel(
        items.ToArray()
      ).Align(true).Padding(3, 2, 2, 2).Background(Decor.menuBackground);
    }

    public static IHtmlControl GetMenuItem(SiteState state, LightSection section, bool isSelected)
    {
      HLabel label = new HLabel(section.NameInMenu, new HHover().TextDecoration("none"))
        .Padding(10, 17, 12, 17).TextDecoration("underline");
      if (!isSelected)
        label.Color(Decor.menuColor);
      else
        label.Color(Decor.menuSelectedColor).FontBold().Background(Decor.menuSelectedBackground);

      return new HPanel(
        new HLink(UrlHlp.ShopUrl("page", section.Id),
          label
        )
      ).InlineBlock();
    }
  }
}