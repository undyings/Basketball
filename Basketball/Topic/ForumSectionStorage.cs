using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Shop.Engine;
using Commune.Basis;
using Commune.Data;

namespace Basketball
{
  public class ForumSectionStorage
  {
    readonly object lockObj = new object();

    readonly RawCache<LightHead[]> topicsCache;

    public LightHead[] Topics
    {
      get
      {
        lock (lockObj)
          return topicsCache.Result;
      }
    }

    long dataChangeTick = 0;
    public void Update()
    {
      lock (lockObj)
        dataChangeTick++;
    }

    readonly IDataLayer fabricConnection;
    public readonly int SectionId;
    public ForumSectionStorage(IDataLayer fabricConnection, int sectionId)
    {
      this.fabricConnection = fabricConnection;
      this.SectionId = sectionId;

      this.topicsCache = new Cache<LightHead[], long>(
        delegate
        {
          ObjectHeadBox topicBox = new ObjectHeadBox(fabricConnection,
            "obj_id in (Select child_id from light_link Where parent_id = @parentId and type_id = @linkTypeId) order by act_till desc",
            new DbParameter("parentId", sectionId),
            new DbParameter("linkTypeId", ForumSectionType.TopicLinks.Kind)
          );

          return ArrayHlp.Convert(topicBox.AllObjectIds, delegate (int topicId)
            { return new LightHead(topicBox, topicId); }
          );
        },
        delegate { return dataChangeTick; }
      );
    }
  }
}