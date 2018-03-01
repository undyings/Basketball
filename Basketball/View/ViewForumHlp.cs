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
  public class ViewForumHlp
  {
    static BasketballContext context
    {
      get { return (BasketballContext)SiteContext.Default; }
    }

    static TopicStorage GetLastTopicForForumSection(LightSection forumSection)
    {
      LightHead lastTopic = _.First(context.Forum.ForSection(forumSection.Id).Topics);
      if (lastTopic == null)
        return null;

      return context.Forum.TopicsStorages.ForTopic(lastTopic.Id);
    }

    static LightObject FindUserForMessage(RowLink message)
    {
      if (message == null)
        return null;

      int lastUserId = message.Get(MessageType.UserId);
      return context.UserStorage.FindUser(lastUserId);
    }

    public static IHtmlControl GetForumView(SiteState state, LightObject currentUser, LightSection forum)
    {
      return new HPanel(
        Decor.Title("Форумы").MarginBottom(15),
        new HGrid<LightSection>(forum.Subsections,
          delegate (LightSection subSection)
          {
            TopicStorage lastTopic = GetLastTopicForForumSection(subSection);
            RowLink lastMessage = lastTopic == null ? null : _.Last(lastTopic.MessageLink.AllRows);
            LightObject lastUser = FindUserForMessage(lastMessage);

            return new HPanel(
              new HPanel(
                new HLink(UrlHlp.ShopUrl("page", subSection.Id),
                  subSection.Get(SectionType.Title)
                ).FontBold()
              ).RelativeWidth(50).PaddingLeft(5).VAlign(8)
                .MediaTablet(new HStyle().Block().Width("auto")),
              new HPanel(
                new HPanel(
                  new HLink(UrlHlp.ShopUrl("topic", lastTopic?.TopicId),
                    lastTopic?.Topic.Get(TopicType.Title)
                  )
                ).MarginTop(7).MarginBottom(3),
                new HPanel(
                  new HLink(UrlHlp.ShopUrl("user", lastUser?.Id),
                    lastUser?.Get(UserType.Login)
                  ),
                  new HLabel(
                    lastMessage?.Get(MessageType.CreateTime).ToLocalTime().ToString(Decor.timeFormat)
                  ).MarginLeft(7).MarginRight(7),
                  new HLink(
                    string.Format("{0}#reply{1}", UrlHlp.ShopUrl("topic", lastTopic?.TopicId), lastMessage?.Get(MessageType.Id)),
                    new HImage("/images/full.gif")
                  ).Hide(lastMessage == null)
                ).MarginBottom(7)
              ).RelativeWidth(50).Padding(0, 5).BorderLeft(Decor.columnBorder)
                .MediaTablet(new HStyle().Width("auto").Border("none"))
            );
          },
          new HRowStyle().Even(new HTone().Background(Decor.evenBackground))
        ).BorderBottom(Decor.bottomBorder).MarginBottom(10),
        DecorEdit.AdminGroupPanel(state.EditMode, forum.Id)
      );
    }

    static IHtmlControl ArrowElement()
    {
      return new HImage(UrlHlp.ImageUrl("arrow.png")).MarginLeft(5).MarginRight(5).MarginBottom(2);
    }

    public static IHtmlControl GetForumSectionView(SiteState state, LightObject currentUser, LightSection section)
    {
      LightHead[] topics = context.Forum.ForSection(section.Id).Topics;

      IHtmlControl addPanel = null;
      if (currentUser != null)
        addPanel = GetTopicAddPanel(state, currentUser, section);

      return new HPanel(
        Decor.Title(section.Get(SectionType.Title)).MarginBottom(15),
        //new HPanel(
        //  new HLink("/forum", "Форумы"),
        //  ArrowElement(),
        //  new HLabel(section.Get(SectionType.Title))
        //).MarginBottom(10),
        addPanel,
        new HGrid<LightHead>(topics, delegate(LightHead topic)
          {
            TopicStorage topicStorage = context.Forum.TopicsStorages.ForTopic(topic.Id);
            //int publisherId = topicStorage.Topic.Get(TopicType.PublisherId);
            //LightObject user = context.UserStorage.FindUser(publisherId);

            RowLink lastMessage = _.Last(topicStorage.MessageLink.AllRows);
            LightObject lastUser = FindUserForMessage(lastMessage);

            return new HPanel(
              new HPanel(
                new HLink(UrlHlp.ShopUrl("topic", topic.Id),
                  topic.Get(TopicType.Title)
                ).FontBold()
              ).RelativeWidth(55).Padding(8, 5, 9, 5).BorderRight(Decor.columnBorder)
                .MediaTablet(new HStyle().Block().Width("auto")),
              //new HPanel(
              //  new HLink(UrlHlp.ShopUrl("user", publisherId),
              //    user?.Get(UserType.Login)
              //  ).FontBold()
              //).Align(null),
              new HPanel(
                new HLabel(topicStorage.MessageLink.AllRows.Length)
              ).Align(null).RelativeWidth(10).PaddingTop(8).PaddingBottom(9).BorderRight(Decor.columnBorder)
                .MediaTablet(new HStyle().Width(50).PaddingTop(0)),
              new HPanel(
                new HLink(UrlHlp.ShopUrl("user", lastUser?.Id),
                  lastUser?.Get(UserType.Login)
                ),
                new HLabel( 
                  lastMessage?.Get(MessageType.CreateTime).ToLocalTime().ToString(Decor.timeFormat)
                ).MarginLeft(7).MarginRight(7),
                new HLink(
                  string.Format("{0}#reply{1}", UrlHlp.ShopUrl("topic", topic.Id), lastMessage?.Get(MessageType.Id)),
                  new HImage("/images/full.gif")
                )
              ).RelativeWidth(35).Padding(0, 5)
                .MediaTablet(new HStyle().Width("auto"))
            );
          },
          new HRowStyle().Even(new HTone().Background(Decor.evenBackground))
        ).BorderBottom(Decor.bottomBorder).MarginBottom(10)
      );
    }

    static IHtmlControl GetTopicAddPanel(SiteState state, LightObject currentUser, LightSection section)
    {
      string blockHint = "topic_add";

      IHtmlControl addPanel = null;
      if (state.BlockHint == blockHint)
      {
        addPanel = new HPanel(
          Decor.PropertyEdit("addTopicTitle", "Заголовок темы"),
          new HTextArea("addTopicText")
            .Width("100%").Height("6em").MarginBottom(5),
          Decor.Button("Сохранить").MarginTop(10)
            .Event("save_topic_add", "addTopicData", delegate(JsonData json)
              {
                string title = json.GetText("addTopicTitle");
                string text = json.GetText("addTopicText");

                WebOperation operation = state.Operation;

                if (!operation.Validate(title, "Не задан заголовок"))
                  return;
                if (!operation.Validate(text, "Не задан текст"))
                  return;

                KinBox editBox = new KinBox(context.FabricConnection, "1=0");

                int addTopicId = editBox.CreateObject(TopicType.Topic, TopicType.Title.CreateXmlIds(title), DateTime.UtcNow);
                LightKin addTopic = new LightKin(editBox, addTopicId);

                addTopic.SetParentId(ForumSectionType.TopicLinks, section.Id);

                addTopic.Set(ObjectType.ActTill, DateTime.UtcNow);
                addTopic.Set(TopicType.PublisherId, currentUser.Id);

                editBox.Update();

                MessageHlp.InsertMessage(context.ForumConnection, addTopic.Id, currentUser.Id, null, text);

                context.UpdateLastComments(true);
                context.Forum.ForSection(section.Id).Update();

                state.BlockHint = "";

                //context.Forum.TopicsStorages.ForTopic(addTopic.Id);
              }
            )
        ).EditContainer("addTopicData").MarginTop(5);
      }

      return new HPanel(
        Decor.Button("Создать тему")
          .Event("topic_add", "", delegate
            {
              state.SetBlockHint(blockHint);
            }
          ),
        addPanel
      ).MarginTop(5).MarginBottom(10);
    }

    public static IHtmlControl GetTopicView(SiteState state, LightObject currentUser, TopicStorage topic)
    {
      if (topic == null || topic.Topic == null)
        return null;

      int? forumSectionId = topic.Topic.GetParentId(ForumSectionType.TopicLinks);
      LightSection forumSection = context.Store.Sections.FindSection(forumSectionId);

      if (forumSection == null)
        return null;

      IHtmlControl editPanel = null;
      if (state.ModeratorMode)
        editPanel = GetTopicRedoPanel(state, currentUser, forumSection, topic);

      return new HPanel(
        Decor.Title(topic.Topic.Get(TopicType.Title)).MarginBottom(15),
        new HPanel(
          new HLink("/forum", "Форум"),
          ArrowElement(),
          new HLink(UrlHlp.ShopUrl("page", forumSectionId),
            forumSection?.Get(SectionType.Title)
          )
        ).MarginBottom(10),
        editPanel,
        ViewCommentHlp.GetCommentsPanel(context.ForumConnection, state, currentUser, topic)
      );
    }

    static IHtmlControl GetTopicRedoPanel(SiteState state, 
      LightObject currentUser, LightSection forumSection, TopicStorage topic)
    {
      string blockHint = string.Format("topic_edit_{0}", topic.TopicId);

      IHtmlControl redoPanel = null;
      if (state.BlockHint == blockHint)
      {
        redoPanel = new HPanel(
          new HPanel(
            Decor.Button("Удалить тему").Event("delete_topic", "", delegate
              {
                int messageCount = topic.MessageLink.AllRows.Length;
                if (!state.Operation.Validate(messageCount > 1, "Тема с комментариями не может быть удалена"))
                  return;
                
                MessageHlp.DeleteTopicMessages(context.ForumConnection, topic.Topic.Id);
                BasketballHlp.DeleteTopic(context.FabricConnection, topic.TopicId);

                topic.UpdateTopic();
                topic.UpdateMessages();
                context.UpdateLastComments(true);
                context.Forum.ForSection(forumSection.Id).Update();

                state.RedirectUrl = "/";
              }
            )
          ).Align(false),
          Decor.PropertyEdit("editTopicTitle", "Заголовок темы", topic.Topic.Get(TopicType.Title)),
          Decor.Button("Переименовать тему")
            .Event("save_topic_edit", "editTopicData",
              delegate(JsonData json)
              {
                string title = json.GetText("editTopicTitle");

                WebOperation operation = state.Operation;

                if (!operation.Validate(title, "Не задан заголовок"))
                  return;

                LightObject editTopic = DataBox.LoadObject(context.FabricConnection, TopicType.Topic, topic.TopicId);
                editTopic.SetWithoutCheck(TopicType.Title, title);

                editTopic.Box.Update();

                context.Forum.ForSection(forumSection.Id).Update();
                topic.UpdateTopic();

                state.BlockHint = "";
              },
              topic.TopicId
            )
        ).EditContainer("editTopicData")
          .Padding(5, 10).MarginTop(5).Background(Decor.pageBackground);
      }

      return new HPanel(
        Decor.Button("Редактировать")
          .Event("topic_edit", "", delegate
            {
              state.SetBlockHint(blockHint);
            }
          ),
        redoPanel
      ).MarginTop(5).MarginBottom(10);
    }
  }
}