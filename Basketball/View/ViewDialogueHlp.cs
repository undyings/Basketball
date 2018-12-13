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
  public class ViewDialogueHlp
  {
    static BasketballContext context
    {
      get { return (BasketballContext)SiteContext.Default; }
    }

    public static IHtmlControl GetDialogueView(SiteState state,
      IDataLayer forumConnection, UserStorage userStorage, LightObject user, out string title)
    {
      title = "Диалоги";

      TableLink dialogueLink = DialogueHlp.LoadDialogueLink(forumConnection,
        "user_id = @userId order by modify_time desc",
        new DbParameter("userId", user.Id)
      );

      IHtmlControl[] messageBlocks = new IHtmlControl[dialogueLink.AllRows.Length];
      int i = -1;
      foreach (RowLink dialog in dialogueLink.AllRows)
      {
        ++i;
        messageBlocks[i] = GetDialogBlock(forumConnection, userStorage, user, dialog, i);
      }

      return new HPanel(
        Decor.Title("Диалоги"),
        new HPanel(
          messageBlocks
        )
      );
    }

    static IHtmlControl GetDialogBlock(IDataLayer forumConnection, UserStorage userStorage, 
      LightObject user, RowLink dialog, int messageIndex)
    {
      LightObject collocutor = userStorage.FindUser(dialog.Get(DialogueType.CollocutorId));

      if (collocutor == null)
        return null;

      bool read = dialog.Get(DialogueType.Unread);
      bool inbox = dialog.Get(DialogueType.Inbox);
      LightObject author = inbox ? collocutor : user;
      DateTime localTime = dialog.Get(DialogueType.ModifyTime).ToLocalTime();

      HBefore before = null;
      if (!inbox)
        before = std.BeforeAwesome(@"\f112", 8);

      IHtmlControl messageBlock = new HPanel("", new IHtmlControl[] {
          new HPanel(
            ViewUserHlp.AvatarBlock(collocutor)
          ).PositionAbsolute().Left(0).Top(0).Padding(7, 5, 10, 5),
          new HPanel(
            new HPanel(
              new HLabel(collocutor.Get(UserType.Login)).FontBold(),
              new HLabel(collocutor.Get(UserType.FirstName)).MarginLeft(5),
              new HLabel(localTime.ToString("dd.MM.yyyy HH:mm")).PositionAbsolute().Right(5)
                .FontSize("90%").Color(Decor.minorColor)
            ).PositionRelative().MarginBottom(6),
            new HLabel(
              //BasketballHlp.PreViewComment(dialog.Get(DialogueType.Content)),
              dialog.Get(DialogueType.Content),
              before
            ).Block().PaddingBottom(15).BorderBottom("1px solid silver").MarginBottom(5)
              .FontBold(read).Color(read ? Decor.textColor : Decor.minorColor)
              .Width("100%").NoWrap().Overflow("hidden").CssAttribute("text-overflow", "ellipsis")
          ).BoxSizing().Width("100%").BorderLeft(Decor.columnBorder).Padding(7, 5, 5, 5)
        },
        new HHover().Background(Decor.pageBackground)
      ).PositionRelative().PaddingLeft(64).BorderTop("2px solid #fff").Color(Decor.textColor);

      //if (messageIndex % 2 != 0)
      //  messageBlock.Background(Decor.evenBackground);

      return new HLink(string.Format("{0}", UrlHlp.ShopUrl("dialog", collocutor.Id)),
        messageBlock,
        new HHover().Background(Decor.pageBackground)
      ).TextDecoration("none");
    }

    static IHtmlControl GetCorrespondenceHeader(SiteState state, LightObject user, LightObject collocutor)
    {
      return new HPanel(
        new HPanel(
          new HLink(UrlHlp.ShopUrl("dialog"),
            new HLabel("Назад", 
              std.BeforeAwesome(@"\f104", 8).FontSize(26).FontBold(false).VAlign(-4)
            ).Color(Decor.minorColor).FontBold()
          )
        ).InlineBlock().PositionAbsolute().Left(5).Top(10),
        new HPanel(
          new HLink(UrlHlp.ShopUrl("user", collocutor?.Id),
            collocutor?.Get(UserType.Login)
          ).FontBold(),
          new HLabel(collocutor?.Get(UserType.FirstName)).Block()
        ).Padding(7, 5, 10, 5),
        new HPanel(
          new HLink(UrlHlp.ShopUrl("user", collocutor?.Id),
            ViewUserHlp.AvatarBlock(collocutor).Size(32, 32)
          )
        ).PositionAbsolute().Right(5).Top(5)
      ).PositionRelative().Align(null).BorderBottom(Decor.buttonBorder)
        .PaddingLeft(50).PaddingRight(50);
    }

    public static IHtmlControl GetCorrespondenceView(SiteState state, 
      IDataLayer forumConnection, LightObject user, LightObject collocutor, out string title)
    {
      title = "Личные сообщения";

      int pageIndex = state.Option.Get(OptionType.CorrespondencePageIndex);
      int messageCount = DatabaseHlp.RowCount(forumConnection, "", "correspondence",
        "user_id = @userId and collocutor_id = @collocutorId",
        new DbParameter("userId", user.Id), new DbParameter("collocutorId", collocutor.Id)
      );
      int pageCount = BinaryHlp.RoundUp(messageCount, 5);

      HButton prevButton = null;
      if (pageCount - 1 > pageIndex)
      {
        prevButton = Decor.ButtonMidi("предыдущие").Event("page_prev", "",
          delegate
          {
            state.Option.Set(OptionType.CorrespondencePageIndex, pageIndex + 1);
          }
        );
      }

      HButton nextButton = null;
      if (pageIndex > 0)
      {
        nextButton = Decor.ButtonMidi("следующие").Event("page_next", "",
          delegate
          {
            state.Option.Set(OptionType.CorrespondencePageIndex, pageIndex - 1);
          }
        );
      }

      TableLink messageLink = DialogueHlp.LoadCorrespondenceLink(forumConnection,
        string.Format("user_id = @userId and collocutor_id = @collocutorId order by create_time desc limit 5 offset {0}",
          pageIndex * 5
        ),
        new DbParameter("userId", user.Id), new DbParameter("collocutorId", collocutor.Id)
      );

      RowLink[] messages = messageLink.AllRows;
      List<IHtmlControl> messageBlocks = new List<IHtmlControl>();

      if (prevButton != null || nextButton != null)
      {
        messageBlocks.Add(
          new HPanel(
            new HPanel(prevButton), new HPanel(nextButton)
          ).MarginTop(5).MarginBottom(5)
        );
      }

      for (int i = messages.Length - 1; i >= 0; --i)
      {
        //if (i == 2)
        //  messageBlocks.Add(new HAnchor("bottom"));

        messageBlocks.Add(GetMessageBlock(forumConnection, state, user, collocutor, messages[i], i));
      }

      IHtmlControl addPanel = GetAddPanel(context, state, user, collocutor, false);

      return new HPanel(
        GetCorrespondenceHeader(state, user, collocutor),
        new HPanel(
          messageBlocks.ToArray()
        ).BorderBottom(Decor.bottomBorder),
        addPanel
      );
    }

    const string correspondenceModeration = "correspondence_moderation";

    readonly static HBuilder h = null;

    public static IHtmlControl GetAddPanel(BasketballContext context, SiteState state,
      LightObject user, LightObject collocutor, bool sendFromUserView)
    {
      if (user == null)
        return null;

      if (user.Id == collocutor.Id)
        return null;

      IHtmlControl editPanel = null;
      if (state.BlockHint == "messageAdd")
      {
        string commentValue = BasketballHlp.AddCommentFromCookie();

        editPanel = new HPanel(
          new HTextArea("messageContent", commentValue).BoxSizing().Width("100%")
            .Height("10em").MarginTop(5).MarginBottom(5),
          Decor.Button("отправить")
            .OnClick(BasketballHlp.AddCommentToCookieScript("messageContent"))
            .Event("message_add_save", "messageData",
            delegate (JsonData json)
            {
              string content = json.GetText("messageContent");
              if (StringHlp.IsEmpty(content))
                return;

              DialogueHlp.SendMessage(context, user.Id, collocutor.Id, content);

              state.BlockHint = "";

              BasketballHlp.ResetAddComment();

              if (sendFromUserView)
                state.Operation.Message = "Сообщение успешно отправлено";
            }
          ),
          new HElementControl(
            h.Script(h.type("text/javascript"), "$('.messageContent').focus();"),
            ""
          )
        ).EditContainer("messageData");
      }

      HButton moderatorButton = null;
      HPanel moderatorPanel = null;
      if (!sendFromUserView)
      {
        moderatorButton = new HButton("",
          std.BeforeAwesome(@"\f1e2", 0)
        ).PositionAbsolute().Right(5).Top(0)
          .Title("Модерирование личных сообщений").FontSize(14)
          .Color(state.BlockHint == correspondenceModeration ? Decor.redColor : Decor.disabledColor)
          .Event("correspondence_moderation_set", "", delegate
          {
            state.SetBlockHint(correspondenceModeration);
          });

        if (state.BlockHint == correspondenceModeration)
        {
          bool lockedCollocutor = user.Get(BasketballUserType.LockedUserIds, collocutor.Id);
          moderatorPanel = new HPanel(
            Decor.ButtonMidi(!lockedCollocutor ? "Заблокировать собеседника" : "Разблокировать собеседника")
              .Event("collocutor_locked", "", delegate
              {
                LightObject editUser = DataBox.LoadObject(context.UserConnection, UserType.User, user.Id);
                editUser.Set(BasketballUserType.LockedUserIds, collocutor.Id, !lockedCollocutor);
                editUser.Box.Update();
                context.UserStorage.Update();
              }),
            new HSpoiler(Decor.ButtonMidi("Удаление переписки").Block().FontBold(false),
              new HPanel(
                Decor.ButtonMidi("Удалить? (без подтверждения)")
                  .MarginTop(5).MarginLeft(10)
                  .Event("correspondence_delete", "", delegate
                  {
                    context.ForumConnection.GetScalar("", 
                      "Delete From correspondence Where user_id = @userId and collocutor_id = @collocutorId",
                      new DbParameter("userId", user.Id), new DbParameter("collocutorId", collocutor.Id)
                    );

                    context.ForumConnection.GetScalar("",
                      "Delete From dialogue Where user_id = @userId and collocutor_id = @collocutorId",
                      new DbParameter("userId", user.Id), new DbParameter("collocutorId", collocutor.Id)
                    );

                    context.UpdateUnreadDialogs();
                  })
              )
            ).MarginTop(10)
          ).MarginTop(10);
        }
      }

      bool locked = user.Get(BasketballUserType.LockedUserIds, collocutor.Id) ||
        collocutor.Get(BasketballUserType.LockedUserIds, user.Id);

      return new HPanel(
        new HPanel(
          Decor.ButtonMidi("Написать сообщение")
            .Hide(locked)
            .Event("message_add", "", delegate
              {
                state.SetBlockHint("messageAdd");
              }
            ),
          new HLabel("Вы не можете отправить сообщение этому пользователю").Hide(!locked)
            .MarginLeft(10).Color(Decor.subtitleColor),
          moderatorButton
        ).PositionRelative(),
        editPanel,
        moderatorPanel
      ).MarginTop(10).MarginBottom(10);
    }

    static IHtmlControl GetMessageBlock(IDataLayer forumConnection, SiteState state,
      LightObject user, LightObject collocutor, RowLink message, int messageIndex)
    {
      int messageId = message.Get(CorrespondenceType.Id);
      LightObject author = message.Get(CorrespondenceType.Inbox) ? collocutor : user;
      DateTime localTime = message.Get(CorrespondenceType.CreateTime).ToLocalTime();

      IHtmlControl deleteElement = null;
      if (state.BlockHint == "correspondence_moderation")
      {
        deleteElement = new HButton("",
          std.BeforeAwesome(@"\f00d", 0)
        ).MarginLeft(5).Color(Decor.redColor).Title("удалить комментарий")
        .Event("delete_message", "", delegate
          {
            forumConnection.GetScalar("", "Delete From correspondence Where id = @messageId",
              new DbParameter("messageId", messageId)
            );
          },
          messageId
        );
      }

      IHtmlControl messageBlock = new HPanel("", new IHtmlControl[] {
          new HPanel(
            ViewUserHlp.AvatarBlock(author)
          ).PositionAbsolute().Left(0).Top(0).Padding(7, 5, 10, 5),
          new HPanel(
            new HPanel(
              //new HLabel(author.Get(UserType.Login)).FontBold(),
              new HLink(UrlHlp.ShopUrl("user", author.Id),
                author.Get(UserType.FirstName)
              ).FontBold(),
              //new HLabel(author.Get(UserType.FirstName)).MarginLeft(5),
              new HPanel(
                new HLabel(localTime.ToString("dd.MM.yyyy HH:mm")).FontSize("90%").Color(Decor.minorColor),
                deleteElement
              ).InlineBlock().PositionAbsolute().Right(5)                
            ).PositionRelative().MarginBottom(6),
            new HTextView(
              BasketballHlp.PreViewComment(message.Get(CorrespondenceType.Content))
            ).Block().PaddingBottom(15).BorderBottom("1px solid silver").MarginBottom(5)
          ).BoxSizing().Width("100%").BorderLeft(Decor.columnBorder).Padding(7, 5, 5, 5)
        }
      ).PositionRelative().PaddingLeft(64).BorderTop("2px solid #fff").Color(Decor.textColor);

      //IHtmlControl messageBlock = new HXPanel(
      //  new HPanel(
      //    new HLink(UrlHlp.ShopUrl("user", author.Id),
      //      author.Get(UserType.Login)
      //    ).FontBold(),
      //    new HLabel(author.Get(UserType.FirstName)).Block()
      //      .MediaTablet(new HStyle().InlineBlock().MarginLeft(5)),
      //    new HPanel(
      //      ViewUserHlp.AvatarBlock(author)
      //    ).MarginTop(5)
      //      .MediaTablet(new HStyle().Display("none"))
      //  ).BoxSizing().WidthLimit("100px", "").Padding(7, 5, 10, 5)
      //    .MediaTablet(new HStyle().Block().PaddingBottom(0).PaddingRight(110)),
      //  new HPanel(
      //    new HPanel(
      //      new HLabel(localTime.ToString("dd.MM.yyyy HH:mm")).MarginRight(5)
      //        .MediaTablet(new HStyle().MarginBottom(5)),
      //      deleteElement
      //    ).Align(false).FontSize("90%").Color(Decor.minorColor),
      //    new HTextView(
      //      BasketballHlp.PreViewComment(message.Get(CorrespondenceType.Content))
      //    ).PaddingBottom(15).MarginBottom(5).BorderBottom("1px solid silver")
      //  ).BoxSizing().Width("100%").BorderLeft(Decor.columnBorder).Padding(7, 5, 5, 5)
      //    .MediaTablet(new HStyle().Block().MarginTop(-19))
      //).BorderTop("2px solid #fff");

      if (messageIndex % 2 != 0)
        messageBlock.Background(Decor.evenBackground);

      return messageBlock;
    }
  }
}