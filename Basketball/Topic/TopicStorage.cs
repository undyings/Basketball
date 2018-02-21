using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Shop.Engine;
using Commune.Basis;
using Commune.Data;

namespace Basketball
{
  public class TopicStorage
  {
    readonly object lockObj = new object();

    public LightKin Topic
    {
      get
      {
        lock (lockObj)
          return topicCache.Result;
      }
    }

    public TableLink MessageLink
    {
      get
      {
        lock (lockObj)
          return messageLinkCache.Result.Item1;
      }
    }

    public Dictionary<int, string> HtmlRepresentByMessageId
    {
      get
      {
        lock (lockObj)
          return messageLinkCache.Result.Item2;
      }
    }

    readonly RawCache<LightKin> topicCache;
    readonly RawCache<Tuple<TableLink, Dictionary<int, string>>> messageLinkCache;

    long topicChangeTick = 0;
    public void UpdateTopic()
    {
      lock (lockObj)
        topicChangeTick++;
    }

    long messageChangeTick = 0;
    public void UpdateMessages()
    {
      lock (lockObj)
        messageChangeTick++;
    }

    public readonly int TopicId;
    public TopicStorage(IDataLayer topicConnection, IDataLayer messageConnection, int topicTypeId, int topicId)
    {
      this.TopicId = topicId;

      this.topicCache = new Cache<LightKin, long>(
        delegate
        {
          return DataBox.LoadKin(topicConnection, topicTypeId, topicId);
        },
        delegate { return topicChangeTick; }
      );

      this.messageLinkCache = new Cache<Tuple<TableLink, Dictionary<int, string>>, long>(
        delegate
        {
          TableLink messageLink = MessageHlp.LoadMessageLink(messageConnection, topicId);

          Dictionary<int, string> htmlRepresentById = new Dictionary<int, string>(messageLink.AllRows.Length);
          foreach (RowLink message in messageLink.AllRows)
          {
            int messageId = message.Get(MessageType.Id);
            string content = message.Get(MessageType.Content);
            string htmlRepresent = BasketballHlp.PreViewComment(content);
            htmlRepresentById[messageId] = htmlRepresent;
          }

          return _.Tuple(messageLink, htmlRepresentById);
        },
        delegate { return messageChangeTick; }
      );
    }
  }
}