using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Shop.Engine;
using Commune.Basis;
using Commune.Data;

namespace Basketball
{
  public class ForumStorageCache
  {
    readonly object lockObj = new object();

    readonly Dictionary<int, ForumSectionStorage> forumSectionById = new Dictionary<int, ForumSectionStorage>();
    public ForumSectionStorage ForSection(int sectionId)
    {
      lock (lockObj)
      {
        ForumSectionStorage section;
        if (!forumSectionById.TryGetValue(sectionId, out section))
        {
          section = new ForumSectionStorage(topicConnection, sectionId);
          forumSectionById[sectionId] = section;
        }
        return section;
      }
    }

    public readonly TopicStorageCache TopicsStorages;

    readonly IDataLayer topicConnection;
    readonly IDataLayer messageConnection;
    public ForumStorageCache(IDataLayer topicConnection, IDataLayer messageConnection)
    {
      this.topicConnection = topicConnection;
      this.messageConnection = messageConnection;

      this.TopicsStorages = new TopicStorageCache(topicConnection, messageConnection, TopicType.Topic);
    }
  }
}