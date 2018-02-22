using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NitroBolt.Wui;
using Commune.Basis;
using Commune.Html;
using Commune.Data;
using Shop.Engine;
using System.Data;

namespace Basketball
{
  public class ViewTagHlp
  {
    public static string TagUrl(int tagId, int pageNumber)
    {
      string tagUrl = string.Format("/tags?tag={0}", tagId);
      if (pageNumber > 0)
        tagUrl = string.Format("{0}&page={1}", tagUrl, pageNumber);
      return tagUrl;
    }

    public static int[] GetNewsIdsForTag(IDataLayer fabricConnection, int tagId)
    {
      //DataTable table = fabricConnection.GetTable("",
      //  "Select parent_id From light_link Where child_id = @tagId and type_id = @linkType",
      //  new DbParameter("tagId", tagId),
      //  new DbParameter("linkType", TopicType.TagLinks.Kind)
      //);

      DataTable table = fabricConnection.GetTable("",
        "Select obj_id From light_object Where obj_id in (Select parent_id From light_link Where child_id = @tagId and type_id = @linkType) order by act_from desc",
        new DbParameter("tagId", tagId),
        new DbParameter("linkType", TopicType.TagLinks.Kind)
      );

      int[] newsIds = new int[table.Rows.Count];
      for (int i = 0; i < newsIds.Length; ++i)
        newsIds[i] = ConvertHlp.ToInt(table.Rows[i][0]) ?? 0;

      return newsIds;
    }

    public static IHtmlControl GetViewTagsPanel(ObjectHeadBox tagBox, LightParent topic)
    {
      List<IHtmlControl> elements = new List<IHtmlControl>();
      elements.Add(new HLabel("Теги:").FontBold().MarginRight(5));
      int[] tagIds = topic.AllChildIds(TopicType.TagLinks);
      foreach (int tagId in tagIds)
      {
        RowLink tagRow = tagBox.ObjectById.AnyRow(tagId);
        if (tagRow == null)
          continue;

        elements.Add(
          new HLink(TagUrl(tagId, 0), TagType.DisplayName.Get(tagRow)).MarginRight(5)
        );
      }

      return new HPanel(
        elements.ToArray()
      ).MarginTop(10);
    }

    public static IHtmlControl GetEditTagsPanel(ObjectHeadBox tagBox, List<string> tags)
    {
      if (tags == null)
        return null;

      List<IHtmlControl> tagElements = new List<IHtmlControl>();
      int i = -1;
      foreach (string tag in tags)
      {
        ++i;
        int index = i;
        tagElements.Add(
          new HPanel(
            new HLabel(tag),
            new HButton("",
              std.BeforeAwesome(@"\f00d", 0)
            ).MarginLeft(5).Color(Decor.redColor).Title("удалить тег")
            .Event("tag_remove", "", delegate
              {
                if (index < tags.Count)
                  tags.RemoveAt(index);
              },
              index
            )
          ).InlineBlock().Margin(5)
        );
      }

      return new HPanel(
        new HPanel(
          tagElements.ToArray()
        ),
        new HPanel(
          new HTextEdit("addTag"),
          //new HComboEdit<int>("addTag", -1, delegate(int tagId)
          //  {
          //    return TagType.DisplayName.Get(tagBox, tagId);
          //  },
          //  tagBox.AllObjectIds
          //),            
          Decor.Button("Добавить тэг").MarginLeft(5).VAlign(-1)
            .Event("tag_add", "addTagData",
            delegate (JsonData json)
            {
              string addTag = json.GetText("addTag");
              if (StringHlp.IsEmpty(addTag))
                return;

              if (tags.Contains(addTag))
                return;

              tags.Add(addTag);
            })
        ).EditContainer("addTagData")
      ).MarginTop(5);
    }

    public static List<string> GetTopicDisplayTags(ObjectHeadBox tagBox, LightParent topic)
    {
      LightHead[] tags = GetTopicTags(tagBox, topic);
      List<string> displayTags = new List<string>(tags.Length);
      foreach (LightHead tag in tags)
      {
        displayTags.Add(tag.Get(TagType.DisplayName));
      }
      return displayTags;
    }

    public static LightHead[] GetTopicTags(ObjectHeadBox tagBox, LightParent topic)
    {
      int[] tagIds = topic.AllChildIds(TopicType.TagLinks);
      List<LightHead> tags = new List<LightHead>(tagIds.Length);
      foreach (int tagId in tagIds)
      {
        if (tagBox.ObjectById.Exist(tagId))
          tags.Add(new LightHead(tagBox, tagId));
      }
      return tags.ToArray();
    }

    public static void SaveTags(BasketballContext context, SiteState state, LightParent editTopic)
    {
      List<string> tags = state.Tag as List<string>;
      if (tags == null)
        return;

      ObjectHeadBox editBox = null;
      List<int> tagIds = new List<int>();
      foreach (string tag in tags)
      {
        string xmlIds = TagType.DisplayName.CreateXmlIds(tag);
        RowLink tagRow = context.Tags.ObjectByXmlIds.AnyRow(xmlIds);
        if (tagRow != null)
        {
          tagIds.Add(tagRow.Get(ObjectType.ObjectId));
          continue;
        }

        if (editBox == null)
          editBox = new ObjectHeadBox(context.FabricConnection, "1=0");

        int? newTagId = editBox.CreateUniqueObject(TagType.Tag, xmlIds, null);
        if (newTagId == null)
          continue;

        tagIds.Add(newTagId.Value);
      }

      if (editBox != null)
      {
        editBox.Update();
        context.UpdateTags();
      }

      for (int i = 0; i < tagIds.Count; ++i)
      {
        int tagId = tagIds[i];
        if (tagId != editTopic.GetChildId(TopicType.TagLinks, i))
          editTopic.SetChildId(TopicType.TagLinks, i, tagId);
      }

      RowLink[] allTagRows = editTopic.AllChildRows(TopicType.TagLinks);
      for (int i = allTagRows.Length - 1; i >= tagIds.Count; --i)
      {
        editTopic.RemoveChildLink(TopicType.TagLinks, i);
      }
    }
  }
}