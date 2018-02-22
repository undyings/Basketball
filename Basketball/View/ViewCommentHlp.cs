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
  public class ViewCommentHlp
  {
    static BasketballContext context
    {
      get { return (BasketballContext)SiteContext.Default; }
    }

    readonly static HBuilder h = null;

    public static IHtmlControl GetCommentsPanel(IDataLayer commentConnection,
      SiteState state, LightObject currentUser, TopicStorage topic)
    {
      HPanel addPanel = null;
      if (currentUser != null)
      {
        HPanel editPanel = null;
        if (state.BlockHint == "commentAdd")
        {
          editPanel = new HPanel(
            new HTextArea("commentContent").Width("100%").Height("10em").MarginTop(5).MarginBottom(5),
            Decor.Button("отправить").Event("comment_add_save", "commentData",
              delegate (JsonData json)
              {
                string content = json.GetText("commentContent");
                if (StringHlp.IsEmpty(content))
                  return;

                if (BasketballHlp.IsDuplicate(topic, currentUser.Id, content))
                  return;

                InsertMessageAndUpdate(commentConnection, topic, currentUser, null, content);

                //MessageHlp.InsertMessage(commentConnection, topic.TopicId, currentUser.Id, null, content);
                //topic.UpdateMessages();

                //context.UpdateLastComments(commentConnection == context.ForumConnection);

                ////hack
                //if (commentConnection == context.ForumConnection)
                //{
                //  context.FabricConnection.GetScalar("",
                //    "Update light_object Set act_till=@modifyTime Where obj_id=@topicId",
                //    new DbParameter("modifyTime", DateTime.UtcNow),
                //    new DbParameter("topicId", topic.TopicId)
                //  );

                //  int? sectionId = topic.Topic.GetParentId(ForumSectionType.TopicLinks);

                //  if (sectionId != null)
                //    context.Forum.ForSection(sectionId.Value).Update();
                //}

                state.BlockHint = "";
              }
            ),
            new HElementControl(
              h.Script(h.type("text/javascript"), "$('.commentContent').focus();"),
              ""
            )
          ).EditContainer("commentData");
        }

        addPanel = new HPanel(
          Decor.ButtonMini("оставить комментарий").FontBold().FontSize(12).Padding(2, 7).
            Event("comment_add", "", delegate
            {
              state.SetBlockHint("commentAdd");
            }
            ),
          new HButton("", 
            new HBefore().ContentIcon(11, 11).BackgroundImage(UrlHlp.ImageUrl("refresh.png"))
          ).Color("#9c9c9c").MarginLeft(10).Title("Загрузить новые комментарии")
            .Event("comments_refresh", "", delegate { }),
          editPanel
        );
      }

      Dictionary<int, string> htmlRepresentByMessageId = topic.HtmlRepresentByMessageId;

      RowLink[] allMessages = topic.MessageLink.AllRows;
      RowLink bottomMessage = null;
      if (allMessages.Length > 0)
        bottomMessage = allMessages[Math.Max(0, allMessages.Length - 4)];

      return new HPanel(
        new HAnchor("comments"),
        new HLabel("Комментарии:").MarginTop(30).MarginBottom(20).FontSize("160%")
          .Hide(commentConnection == context.ForumConnection),
        new HPanel(
          new HLabel("Автор").PositionAbsolute().Top(0).Left(0)
            .BoxSizing().Width(100).Padding(7, 5, 5, 5),
          new HLabel("Сообщение").Block().Padding(7, 5, 5, 5).BorderLeft(Decor.columnBorder)
        ).PositionRelative().Align(null).PaddingLeft(100).Background("#dddddd").FontBold(),
        new HGrid<RowLink>(topic.MessageLink.AllRows, delegate (RowLink comment)
          {
            IHtmlControl commentBlock = ViewCommentHlp.GetCommentBlock(
              commentConnection, state, currentUser, topic, htmlRepresentByMessageId, comment
            );

            if (bottomMessage == comment)
              return new HPanel(new HAnchor("bottom"), commentBlock);
            return commentBlock;
          },
          new HRowStyle().Even(new HTone().Background(Decor.evenBackground))
        ).BorderBottom(Decor.bottomBorder).MarginBottom(10),
        //new HAnchor("bottom"),
        addPanel
      );
    }

    public static void InsertMessageAndUpdate(IDataLayer commentConnection, TopicStorage topic, 
      LightObject currentUser, int? whomId, string content)
    {
      MessageHlp.InsertMessage(commentConnection, topic.TopicId, currentUser.Id, whomId, content);
      topic.UpdateMessages();

      context.UpdateLastComments(commentConnection == context.ForumConnection);

      //hack
      if (commentConnection == context.ForumConnection)
      {
        context.FabricConnection.GetScalar("",
          "Update light_object Set act_till=@modifyTime Where obj_id=@topicId",
          new DbParameter("modifyTime", DateTime.UtcNow),
          new DbParameter("topicId", topic.TopicId)
        );

        int? sectionId = topic.Topic.GetParentId(ForumSectionType.TopicLinks);

        if (sectionId != null)
          context.Forum.ForSection(sectionId.Value).Update();
      }
    }

    public static IHtmlControl GetCommentBlock(IDataLayer commentConnection, SiteState state,
      LightObject currentUser, TopicStorage topic, Dictionary<int, string> htmlRepresentByMessageId, RowLink comment)
    {
      LightObject user = context.UserStorage.FindUser(comment.Get(MessageType.UserId));
      DateTime localTime = comment.Get(MessageType.CreateTime).ToLocalTime();

      IHtmlControl whomBlock = GetWhomBlock(state, context.UserStorage, topic, htmlRepresentByMessageId, comment);

      int commentId = comment.Get(MessageType.Id);
      string answerHint = string.Format("answer_{0}", commentId);
      IHtmlControl answerBlock = null;
      if (currentUser != null && state.BlockHint == answerHint)
      {
        answerBlock = new HPanel(
          new HTextArea("answerContent").Width("100%").Height("10em").MarginTop(5).MarginBottom(5),
          Decor.Button("отправить").Event("save_answer", "answerContainer",
            delegate (JsonData json)
            {
              string content = json.GetText("answerContent");
              if (StringHlp.IsEmpty(content))
                return;

              if (BasketballHlp.IsDuplicate(topic, currentUser.Id, content))
                return;

              InsertMessageAndUpdate(commentConnection, topic, currentUser, commentId, content);

              //MessageHlp.InsertMessage(commentConnection, topic.TopicId, currentUser.Id, commentId, content);
              //topic.UpdateMessages();
              //context.UpdateLastComments(commentConnection == context.ForumConnection);

              state.BlockHint = "";
            },
            commentId
          ),
          new HElementControl(
            h.Script(h.type("text/javascript"), "$('.answerContent').focus();"),
            ""
          )
        ).EditContainer("answerContainer");
      }

      IHtmlControl editBlock = null;
      if (currentUser != null && currentUser.Id == user?.Id)
      {
        editBlock = new HPanel(
        );
      }

      string redoHint = string.Format("redo_{0}", commentId);
      HButton redoButton = null;
      if (currentUser != null && currentUser.Id == user?.Id)
      {
        redoButton = Decor.ButtonMini("редактировать").Event("comment_redo", "",
          delegate (JsonData json)
          {
            state.SetBlockHint(redoHint);
          },
          commentId
        );
      }

      IHtmlControl redoBlock = null;
      if (currentUser != null && state.BlockHint == redoHint)
      {
        redoBlock = new HPanel(
          new HTextArea("redoContent", comment.Get(MessageType.Content))
            .Width("100%").Height("10em").MarginTop(5).MarginBottom(5),
          Decor.Button("изменить").Event("save_redo", "redoContainer",
            delegate (JsonData json)
            {
              string content = json.GetText("redoContent");
              if (StringHlp.IsEmpty(content))
                return;

              //content = BasketballHlp.PreSaveComment(content);

              commentConnection.GetScalar("",
                "Update message Set content=@content, modify_time=@time Where id=@id",
                new DbParameter("content", content),
                new DbParameter("time", DateTime.UtcNow),
                new DbParameter("id", commentId)
              );
              topic.UpdateMessages();

              state.BlockHint = "";
            },
            commentId
          ),
          new HElementControl(
            h.Script(h.type("text/javascript"), "$('.redoContent').focus();"),
            ""
          )
        ).EditContainer("redoContainer");
      }

      IHtmlControl deleteElement = null;
      if (state.ModeratorMode)
      {
        deleteElement = new HButton("",
          std.BeforeAwesome(@"\f00d", 0)
        ).MarginLeft(5).Color(Decor.redColor).Title("удалить комментарий")
        .Event("delete_comment", "", delegate
          {
            MessageHlp.DeleteMessage(commentConnection, commentId);
            topic.UpdateMessages();
            context.UpdateLastComments(commentConnection == context.ForumConnection);
          },
          commentId
        );
      }

      string anchor = string.Format("reply{0}", commentId);
      return new HXPanel(
        new HAnchor(anchor),
        new HPanel(
          new HLink(UrlHlp.ShopUrl("user", user?.Id),
            user?.Get(UserType.Login)
          ).FontBold(),
          new HLabel(user?.Get(UserType.FirstName)).Block()
            .MediaTablet(new HStyle().InlineBlock().MarginLeft(5)),
          new HPanel(
            ViewUserHlp.AvatarBlock(user)
          ).MarginTop(5)
            .MediaTablet(new HStyle().Display("none"))
        ).BoxSizing().WidthLimit("100px", "").Padding(7, 5, 10, 5)
          .MediaTablet(new HStyle().Block().PaddingBottom(0).PaddingRight(110)),
        new HPanel(
          new HPanel(
            new HLabel(localTime.ToString("dd.MM.yyyy HH:mm")).MarginRight(5)
              .MediaTablet(new HStyle().MarginBottom(5)),
            new HLink(string.Format("/news/{0}#{1}", topic.TopicId, anchor), "#").TextDecoration("none"),
            deleteElement
          ).Align(false).FontSize("90%").Color(Decor.minorColor),
          whomBlock,
          new HTextView(
            DictionaryHlp.GetValueOrDefault(htmlRepresentByMessageId, commentId)
          ).PaddingBottom(15).MarginBottom(5).BorderBottom("1px solid silver"),
          new HPanel(
            Decor.ButtonMini("ответить").Event("comment_answer", "",
              delegate
              {
                state.SetBlockHint(answerHint);
              },
              commentId
            ),
            redoButton
          ).Hide(currentUser == null),
          answerBlock,
          redoBlock
        ).BoxSizing().Width("100%").BorderLeft(Decor.columnBorder).Padding(7, 5, 5, 5)
          .MediaTablet(new HStyle().Block().MarginTop(-19))
      ).BorderTop("2px solid #fff");
    }

    static IHtmlControl GetWhomBlock(SiteState state, UserStorage userStorage, 
      TopicStorage topic, Dictionary<int, string> htmlRepresentByMessageId, RowLink comment)
    {
      int commentId = comment.Get(MessageType.Id);

      int? whomId = comment.Get(MessageType.WhomId);
      if (whomId == null)
        return null;

      RowLink whom = topic.MessageLink.FindRow(MessageType.MessageById, whomId.Value);
      if (whom == null)
        return null;

      LightObject whomUser = userStorage.FindUser(whom.Get(MessageType.UserId));
      DateTime whomTime = whom.Get(MessageType.CreateTime).ToLocalTime();

      string whomHint = string.Format("whom_{0}", commentId);

      IHtmlControl whomTextBlock = null;
      if (state.BlockHint == whomHint)
      {
        //string whomDisplay = BasketballHlp.PreViewComment(whom.Get(MessageType.Content));
        string whomDisplay = DictionaryHlp.GetValueOrDefault(htmlRepresentByMessageId, whomId.Value);
        whomTextBlock = new HPanel(
          new HTextView(whomDisplay).FontSize(12)
        ).Padding(5);
      }

      return new HPanel(
        new HPanel(
          new HButton(
            whomUser?.Get(UserType.Login),
            new HHover().TextDecoration("none")
          ).Color(Decor.linkColor).TextDecoration("underline").MarginRight(5)
            .Event("whom_display", "", delegate
              {
                state.SetBlockHint(whomHint);
              },
              commentId
            ),
          new HLabel(string.Format("({0})", whomTime.ToString(Decor.timeFormat)))
        ),
        whomTextBlock
      ).Color(Decor.minorColor).FontSize("90%");
    }
  }
}