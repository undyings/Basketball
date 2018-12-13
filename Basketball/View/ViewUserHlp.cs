using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Commune.Basis;
using Commune.Html;
using Commune.Data;
using Shop.Engine;
using NitroBolt.Wui;
using System.IO;

namespace Basketball
{
  public class ViewUserHlp
  {
    static BasketballContext context
    {
      get { return (BasketballContext)SiteContext.Default; }
    }

    public static IHtmlControl GetMessageView(string message)
    {
      return new HPanel(
        Decor.Subtitle(message)
      );
    }

    public static IHtmlControl GetUserView(SiteState state, LightObject currentUser, LightObject user)
    {
      string communityMember = user.Get(BasketballUserType.CommunityMember);

      bool isModerator = user.Get(BasketballUserType.IsModerator);
      DateTime? bannedUntil = user.Get(BasketballUserType.BannedUntil);
      DateTime? noRedactorUntil = user.Get(BasketballUserType.NotRedactorUntil);
      string bannedUntilStr = "";
      if (bannedUntil != null && bannedUntil > DateTime.UtcNow)
        bannedUntilStr = bannedUntil.Value.ToLocalTime().ToString(Decor.timeFormat);
      string noRedactorUntilStr = "";
      if (noRedactorUntil != null && noRedactorUntil > DateTime.UtcNow)
        noRedactorUntilStr = noRedactorUntil.Value.ToLocalTime().ToString(Decor.timeFormat);

      IHtmlControl[] rows = new IHtmlControl[] {
        UserField("Имя", ValueLabel(user.Get(UserType.FirstName)).FontBold()),
        UserField("Дата регистрации",
          ValueLabel(user.Get(ObjectType.ActFrom)?.ToLocalTime().ToString(Decor.timeFormat))
        ),
        !StringHlp.IsEmpty(communityMember) ? UserField("На basketball.ru c", ValueLabel(communityMember)) : null,
        UserField("Страна", ValueLabel(user.Get(BasketballUserType.Country))),
        UserField("Город", ValueLabel(user.Get(BasketballUserType.City))),
        UserField("Интересы", ValueLabel(user.Get(BasketballUserType.Interests))),
        UserField("О себе", ValueLabel(user.Get(BasketballUserType.AboutMe))),
        UserField("Статус", ValueLabel("Модератор").FontBold()).Hide(!isModerator),
        UserField("Заблокирован до", ValueLabel(bannedUntilStr).FontBold()).Hide(StringHlp.IsEmpty(bannedUntilStr)),
        UserField("Не добавляет до", ValueLabel(noRedactorUntilStr).FontBold()).Hide(StringHlp.IsEmpty(noRedactorUntilStr))
      };

      int i = -1;
      foreach (IHtmlControl row in rows)
      {
        if (row == null)
          continue;

        ++i;
        if (i % 2 == 0)
          row.Background(Decor.pageBackground);
      }

      IHtmlControl redoAvatarPanel = null;
      IHtmlControl editPanel = null;
      if (currentUser != null && currentUser.Id == user.Id)
      {
        redoAvatarPanel = GetRedoAvatarPanel(user);
        editPanel = GetUserEditPanel(state, user);
      }

      IHtmlControl adminPanel = null;
      if (state.EditMode)
        adminPanel = GetAdminPanel(user);

      IHtmlControl moderatorPanel = null;
      if (state.ModeratorMode)
        moderatorPanel = GetModeratorPanel(user);

      return new HPanel(
        Decor.Title(user.Get(UserType.Login)),
        new HPanel(
          AvatarBlock(user).PositionAbsolute().Left(12).Top("50%").MarginTop(-25),
          new HPanel(
            rows
          ).Padding(1).Border("1px solid #eeeeee")
        ).PositionRelative().BoxSizing().WidthLimit("", "480px").PaddingLeft(74),
        ViewDialogueHlp.GetAddPanel(context, state, currentUser, user, true),
        redoAvatarPanel,
        editPanel,
        adminPanel,
        moderatorPanel
      );
    }

    public static IHtmlControl AvatarBlock(LightObject user)
    {
      string imageUrl = UrlHlp.ImageUrl(string.Format("users/{0}/avatar.png", user.Id));
      if (imageUrl.EndsWith("placeholder.png"))
        imageUrl = UrlHlp.ImageUrl("no_avatar.gif");

      return new HPanel().InlineBlock().Size(Decor.AvatarSize, Decor.AvatarSize)
        .Background(imageUrl, "no-repeat", "center").CssAttribute("background-size", "100%");
    }

    static IHtmlControl GetModeratorPanel(LightObject user)
    {
      return new HPanel(
        Decor.PropertyEdit("editBannedUntil", "Заблокировать до (например, 22.12.2018)", 
          user.Get(BasketballUserType.BannedUntil)?.ToString("dd.MM.yyyy")
        ),
        Decor.PropertyEdit("editNotRedactorUntil", "Запретить добавление новостей до",
          user.Get(BasketballUserType.NotRedactorUntil)?.ToString("dd.MM.yyyy")
        ),
        Decor.Button("Сохранить").Event("user_rights_save", "moderatorEdit",
          delegate (JsonData json)
          {
            string bannedUntilStr = json.GetText("editBannedUntil");
            string notRedactorUntilStr = json.GetText("editNotRedactorUntil");

            LightObject editUser = DataBox.LoadObject(context.UserConnection, UserType.User, user.Id);

            DateTime bannedUntil;
            if (DateTime.TryParse(bannedUntilStr, out bannedUntil))
              editUser.Set(BasketballUserType.BannedUntil, bannedUntil);
            else
              editUser.Set(BasketballUserType.BannedUntil, null);

            DateTime notRedactorUntil;
            if (DateTime.TryParse(notRedactorUntilStr, out notRedactorUntil))
              editUser.Set(BasketballUserType.NotRedactorUntil, notRedactorUntil);
            else
              editUser.Set(BasketballUserType.NotRedactorUntil, null);

            editUser.Box.Update();
            context.UserStorage.Update();
          }
        )
      ).EditContainer("moderatorEdit").MarginTop(10);
    }

    static IHtmlControl GetAdminPanel(LightObject user)
    {
      bool isModerator = user.Get(BasketballUserType.IsModerator);
      return new HPanel(
        Decor.Button(isModerator ? "Лишить прав модератора" : "Сделать модератором")
          .Event("moderator_rights", "", delegate
            {
              LightObject editUser = DataBox.LoadObject(context.UserConnection, UserType.User, user.Id);
              editUser.Set(BasketballUserType.IsModerator, !isModerator);
              editUser.Box.Update();

              context.UserStorage.Update();
            }
          )
      );
    }

    static IHtmlControl GetRedoAvatarPanel(LightObject user)
    {
      return new HPanel(
        new HFileUploader("/avatarupload", "Выбрать аватар", user.Id),
        Decor.Button("Удалить аватар").MarginTop(5).Event("avatar_delete", "",
          delegate
          {
            File.Delete(UrlHlp.ImagePath(Path.Combine("users", user.Id.ToString(), "avatar.png")));
          }
        )
      ).MarginTop(5);
    }

    static HLabel ValueLabel(string value)
    {
      if (StringHlp.IsEmpty(value))
        value = "не указано";

      HLabel valueLabel = new HLabel(value);
      if (StringHlp.IsEmpty(value))
        valueLabel.Color(Decor.minorColor);

      return valueLabel;
    }

    static IHtmlControl GetUserRedoPanel(SiteState state, LightObject user)
    {
      return new HPanel(
        Decor.PropertyEdit("editUserName", "Ваше имя (*):", user.Get(UserType.FirstName)),
        Decor.PropertyEdit("editUserEmail", "Email (*):", user.Get(UserType.Email)),
        Decor.PropertyEdit("editUserCountry", "Страна:", user.Get(BasketballUserType.Country)),
        Decor.PropertyEdit("editUserCity", "Город:", user.Get(BasketballUserType.City)),
        Decor.PropertyEdit("editUserInterests", "Интересы:", user.Get(BasketballUserType.Interests)),
        Decor.PropertyEdit("editUserAboutMe", "О себе:", user.Get(BasketballUserType.AboutMe)),
        Decor.PropertyEdit("editUserCommunity", "На basketball.ru с:", user.Get(BasketballUserType.CommunityMember)),
        Decor.Button("Сохранить")
          .Event("user_edit_save", "userContainer",
            delegate (JsonData json)
            {
              string name = json.GetText("editUserName");
              string email = json.GetText("editUserEmail");
              string country = json.GetText("editUserCountry");
              string city = json.GetText("editUserCity");
              string interests = json.GetText("editUserInterests");
              string aboutMe = json.GetText("editUserAboutMe");
              string community = json.GetText("editUserCommunity");

              WebOperation operation = state.Operation;

              if (!operation.Validate(email, "Не задана электронная почта"))
                return;
              if (!operation.Validate(!email.Contains("@"), "Некорректный адрес электронной почты"))
                return;
              if (!operation.Validate(name, "Не задано имя"))
                return;

              LightObject editUser = DataBox.LoadObject(context.UserConnection, UserType.User, user.Id);

              editUser.Set(UserType.FirstName, name);
              editUser.Set(UserType.Email, email);
              editUser.Set(BasketballUserType.Country, country);
              editUser.Set(BasketballUserType.City, city);
              editUser.Set(BasketballUserType.Interests, interests);
              editUser.Set(BasketballUserType.AboutMe, aboutMe);
              editUser.Set(BasketballUserType.CommunityMember, community);

              editUser.Set(ObjectType.ActTill, DateTime.UtcNow);

              editUser.Box.Update();

              context.UserStorage.Update();

              state.BlockHint = "";
            }
          )
      ).EditContainer("userContainer");
    }

    static IHtmlControl GetUserEditPanel(SiteState state, LightObject user)
    {
      string blockHint = string.Format("userEdit_{0}", user.Id);

      IHtmlControl redoPanel = null;
      if (state.BlockHint == blockHint)
        redoPanel = GetUserRedoPanel(state, user);

      return new HPanel(
        Decor.Button("Редактировать профиль").MarginBottom(5)
          .Event("user_edit", "", delegate
            {
              state.SetBlockHint(blockHint);
            }
          ),
        redoPanel
      ).MarginTop(5).WidthLimit("", "480px");
    }

    static IHtmlControl UserField(string name, HLabel valueLabel)
    {
      return new HPanel(
        new HLabel(name).VAlign(true).Width(105).Padding(5, 10),
        valueLabel.Padding(5, 10).BorderLeft(Decor.columnBorder)
      ).Border("1px solid #fff");
    }
  }
}