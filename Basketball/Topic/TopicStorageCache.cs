using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Commune.Basis;
using Commune.Data;

namespace Basketball
{
  public class TopicStorageCache
  {
    readonly object lockObj = new object();

    readonly Dictionary<int, TopicStorage> topicStorageById = new Dictionary<int, TopicStorage>();

    public int TopicCount
    {
      get
      {
        lock (lockObj)
          return topicStorageById.Count;
      }
    }

    public TopicStorage ForTopic(int topicId)
    {
      lock (lockObj)
      {
        TopicStorage storage;
        if (!topicStorageById.TryGetValue(topicId, out storage))
        {
          storage = new TopicStorage(topicConnection, messageConnection, topicTypeId, topicId);

					DateTime refTime = DateTime.UtcNow.AddDays(-7);
					RowLink lastMessage = _.Last(storage.MessageLink.AllRows);

					DateTime? topicTime = storage.Topic.Get(ObjectType.ActFrom);
					if ((topicTime != null && topicTime.Value > refTime) ||
						(lastMessage != null && lastMessage.Get(CorrespondenceType.CreateTime) > refTime))
					{
						topicStorageById[topicId] = storage;
					}
        }
        return storage;
      }
    }

    readonly IDataLayer topicConnection;
    readonly IDataLayer messageConnection;
    readonly int topicTypeId;
    public TopicStorageCache(IDataLayer topicConnection, IDataLayer messageConnection, int topicTypeId)
    {
      this.topicConnection = topicConnection;
      this.messageConnection = messageConnection;
      this.topicTypeId = topicTypeId;
    }
  }
}