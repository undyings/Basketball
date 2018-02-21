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
  public class ViewRightColumnHlp
  {
    static BasketballContext context
    {
      get { return (BasketballContext)SiteContext.Default; }
    }

    public static IHtmlControl GetRightColumnView(SiteState state, bool isForum)
    {
      List<IHtmlControl> items = new List<IHtmlControl>(2);
      items.Add(GetActualPublicationPanel(state));

      IHtmlControl forumPanel = GetActualForumPanel(state);
      if (isForum)
        items.Insert(0, forumPanel);
      else
        items.Add(forumPanel);

      return new HPanel(
        items.ToArray()
      ).Align(true).BoxSizing().Width(220);
    }

    public static IHtmlControl GetReclameColumnView(SiteState state)
    {
      LightSection main = context.Store.Sections.FindMenu("main");

      return new HPanel(
        new HGrid<LightKin>(main.Units, delegate (LightKin unit)
          {
            return new HPanel(
              new HLink(unit.Get(UnitType.Link),
                new HImage(UrlHlp.ImageUrl(unit.Id, true)).Alt(unit.Get(UnitType.ImageAlt))
              ).TargetBlank(),
              DecorEdit.AdminUnitRedoIcon(state.EditMode, unit.Id)
            ).PositionRelative();
          },
          new HRowStyle()
        ),
        DecorEdit.AdminUnitPanel(state.EditMode, main.Id)
      ).Width(220);
    }

    static IHtmlControl GetActualForumPanel(SiteState state)
    {
      return new HPanel(
        Decor.Subtitle("На форуме").MarginBottom(10).MarginTop(5),
        new HGrid<RowLink>(context.LastForumComments,
          delegate (RowLink comment)
          {
            int topicId = comment.Get(MessageType.ArticleId);

            TopicStorage topic = context.Forum.TopicsStorages.ForTopic(topicId);
            if (topic == null || topic.Topic == null)
              return new HPanel();

            string url = UrlHlp.ShopUrl("topic", topic?.TopicId);

            int userId = comment.Get(MessageType.UserId);
            LightObject user = context.UserStorage.FindUser(userId);

            DateTime localTime = comment.Get(MessageType.CreateTime).ToLocalTime();
            string replyUrl = string.Format("{0}#reply{1}", url, comment.Get(MessageType.Id));

            return new HPanel(
              new HPanel(
                new HLabel(localTime.ToString("HH:mm")).MarginRight(5)
                  .Title(localTime.ToString(Decor.timeFormat)),
                new HLabel(user?.Get(UserType.Login))
              ),
              new HLink(url,
                topic.Topic.Get(TopicType.Title)
              ).MarginRight(5),
              new HLink(
                replyUrl, "",
                new HBefore().ContentIcon(13, 13).Background("/images/full.gif", "no-repeat", "bottom").VAlign(-2)
              //new HImage("/images/full.gif").VAlign(-2)
              )
            //ViewNewsHlp.GetCommentElement(topic.MessageLink.AllRows.Length, url)
            ).MarginBottom(6);
          },
          new HRowStyle()
        ).FontSize("90%")
      ).Padding(15, 15, 10, 15).MarginBottom(5).Background(Decor.pageBackground);
    }

    static IHtmlControl GetActualPublicationPanel(SiteState state)
    {
      return new HPanel(
        Decor.Subtitle("Обсуждаемое").MarginBottom(10).MarginTop(5),
        new HGrid<RowLink>(context.LastPublicationComments,
          delegate (RowLink comment)
          {
            int topicId = comment.Get(MessageType.ArticleId);

            TopicStorage topic = null;
            string url = "";
            if (context.News.ObjectById.Exist(topicId))
            {
              topic = context.NewsStorages.ForTopic(topicId);
              url = UrlHlp.ShopUrl("news", topic?.TopicId);
            }
            else if (context.Articles.ObjectById.Exist(topicId))
            {
              topic = context.ArticleStorages.ForTopic(topicId);
              url = UrlHlp.ShopUrl("article", topic?.TopicId);
            }

            if (topic == null || topic.Topic == null)
              return new HPanel();

            int userId = comment.Get(MessageType.UserId);
            LightObject user = context.UserStorage.FindUser(userId);

            DateTime localTime = comment.Get(MessageType.CreateTime).ToLocalTime();
            string replyUrl = string.Format("{0}#reply{1}", url, comment.Get(MessageType.Id));

            return new HPanel(
              new HPanel(
                new HLabel(localTime.ToString("HH:mm")).MarginRight(5)
                  .Title(localTime.ToString(Decor.timeFormat)),
                new HLabel(user?.Get(UserType.Login))
              ),
              new HLink(url,
                topic.Topic.Get(TopicType.Title)
              ).MarginRight(5),
              new HLink(
                replyUrl, "",
                new HBefore().ContentIcon(13, 13).Background("/images/full.gif", "no-repeat", "bottom").VAlign(-2)
                //new HImage("/images/full.gif").VAlign(-2)
              )
              //ViewNewsHlp.GetCommentElement(topic.MessageLink.AllRows.Length, url)
            ).MarginBottom(6);
          },
          new HRowStyle()
        ).FontSize("90%")
      ).Padding(15, 15, 10, 15).MarginBottom(5).Background(Decor.pageBackground);
    }
  }
}