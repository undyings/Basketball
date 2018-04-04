using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Commune.Html;
using NitroBolt.Wui;
using Commune.Data;
using Commune.Basis;
using Shop.Engine;
using System.Text;

namespace Basketball
{
  public class BasketballHlp
  {
    public static string GetDescriptionForNews(LightObject topic)
    {
      try
      {
        string text = topic.Get(NewsType.Text);
        if (StringHlp.IsEmpty(text))
          return "";

        int endIndex1 = text.IndexOf("</p>");
        int endIndex2 = text.IndexOf("</h3>");

        if (endIndex1 < 0 && endIndex2 < 0)
          return "";

        int endIndex;
        if (endIndex1 < 0 || endIndex2 < 0)
          endIndex = Math.Max(endIndex1, endIndex2);
        else
          endIndex = Math.Min(endIndex1, endIndex2);

        StringBuilder builder = new StringBuilder();
        string rawDescription = text.Substring(0, endIndex);

        bool openBracket = false;
        int currentIndex = 0;
        while (currentIndex < rawDescription.Length)
        {
          int startIndex = currentIndex;

          if (openBracket)
          {
            openBracket = false;
            currentIndex = rawDescription.IndexOf('>', currentIndex) + 1;
            if (currentIndex == 0)
              break;
            continue;
          }

          openBracket = true;
          currentIndex = rawDescription.IndexOf('<', currentIndex);
          if (currentIndex < 0)
            currentIndex = rawDescription.Length;

          builder.Append(rawDescription.Substring(startIndex, currentIndex - startIndex).Trim('\n'));
          continue;
        }

        return builder.ToString().Replace("&laquo;", "«").Replace("&raquo;", "»").Replace("&quot;", "'");
      }
      catch (Exception ex)
      {
        Logger.WriteException(ex, "TopicId: {0}", topic?.Id);
        return "";
      }

      //return text.Substring(startIndex + 3, endIndex - startIndex - 3).Replace("&laquo;", "«").Replace("&raquo;", "»");
    }

    //hack нельзя использовать рекурсивное удаление, из-за того, что теги сделаны детьми новостей
    public static void DeleteTopic(IDataLayer dbConnection, int objectId)
    {
      dbConnection.GetScalar("",
        "Delete From light_link Where child_id = @objectId",
        new DbParameter("objectId", objectId)
      );

      dbConnection.GetScalar("",
        "Delete From light_link Where parent_id = @objectId",
        new DbParameter("objectId", objectId)
      );

      dbConnection.GetScalar("",
        "Delete From light_property Where obj_id = @objectId",
        new DbParameter("objectId", objectId)
      );

      dbConnection.GetScalar("",
        "Delete From light_object Where obj_id = @objectId",
        new DbParameter("objectId", objectId)
      );
    }

    public static LightHead[] TagForTopic(ObjectHeadBox tagBox, LightKin topic)
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

    public static bool IsDuplicate(TopicStorage topic, int currentUserId, string content)
    {
      RowLink[] messageRows = topic.MessageLink.AllRows;
      if (messageRows.Length == 0)
        return false;

      RowLink lastMessage = _.Last(messageRows);
      if (lastMessage.Get(MessageType.UserId) == currentUserId && lastMessage.Get(MessageType.Content) == content)
        return true;

      return false;
    }

    public static bool IsBanned(LightObject user)
    {
      DateTime? bannedUntil = user.Get(BasketballUserType.BannedUntil);
      if (bannedUntil != null && bannedUntil.Value > DateTime.UtcNow)
        return true;

      return false;
    }

    public static bool NoRedactor(LightObject user)
    {
      DateTime? noRedactorUntil = user.Get(BasketballUserType.NotRedactorUntil);
      if (noRedactorUntil != null && noRedactorUntil.Value > DateTime.UtcNow)
        return true;

      return false;
    }

    class Smiley
    {
      public const string Smile = " <img src='/images/smiley_smile.gif'></img>";
      public const string Sad = " <img src='/images/smiley_sad.gif'></img>";
      public const string Wink = " <img src='/images/smiley_wink.gif'></img>";
      public const string Blah = " <img src='/images/smiley_blah.gif'></img>";
    }

    const string linkFormat = "<a target='_blank' rel='nofollow' href='{0}'>{1}</a>";
    const string imageFormat = "<img src='{0}'></img>";
    const string httpPrefix = "http://";
    const string httpsPrefix = "https://";

    public static string PreViewComment(string content)
    {
      try
      {
        if (StringHlp.IsEmpty(content))
          return "";

        LinkInfo[] links = GetLinks(content);
        if (links.Length > 0)
        {
          StringBuilder builder = new StringBuilder();
          if (links[0].Index > 0)
            builder.Append(content.Substring(0, links[0].Index));

          for (int i = 0; i < links.Length; ++i)
          {
            LinkInfo link = links[i];
            if (!link.IsImage)
            {
              string display = link.Value;
              if (display.StartsWith(httpPrefix))
                display = display.Substring(httpPrefix.Length);
              else if (display.StartsWith(httpsPrefix))
                display = display.Substring(httpsPrefix.Length);

              builder.AppendFormat(linkFormat, link.Value, display);
            }
            else
            {
              builder.AppendFormat(imageFormat, link.Value);
            }

            int startIndex = link.Index + link.Value.Length;
            int endIndex = content.Length;
            if (i + 1 < links.Length)
              endIndex = links[i + 1].Index;

            if (endIndex > startIndex)
              builder.Append(content.Substring(startIndex, endIndex - startIndex));
          }

          content = builder.ToString();
        }

        content = content.Replace("\n", "<br>");

        content = content.Replace(":)", Smiley.Smile);
        content = content.Replace(":(", Smiley.Sad);
        content = content.Replace(" :/", Smiley.Blah);
        content = content.Replace(" ;)", Smiley.Wink);


        return content;
      }
      catch (Exception ex)
      {
        Logger.WriteException(ex);
        return content;
      }
    }

    struct LinkInfo
    {
      public readonly int Index;
      public readonly string Value;
      public readonly bool IsImage;

      public LinkInfo(int index, string value, bool isImage)
      {
        this.Index = index;
        this.Value = value;
        this.IsImage = isImage;
      }
    }

    static LinkInfo[] GetLinks(string content)
    {
      List<LinkInfo> links = new List<LinkInfo>();

      int nextIndex = 0;
      while (nextIndex >= 0)
      {
        int index = content.IndexOf("http", nextIndex);
        if (index < 0)
          break;

        nextIndex = index + 4;

        if (index > 0 && (content[index - 1] == '\'' || content[index - 1] == '"'))
          continue;

        int endIndex = content.Length;
        for (int i = index + 4; i < content.Length; ++i)
        {
          if (content[i] == ' ' || content[i] == '\n')
          {
            endIndex = i;
            break;
          }
        }

        string value = content.Substring(index, endIndex - index);
        bool isImage = value.EndsWith(".jpg") || value.EndsWith(".gif") || 
          value.EndsWith(".png") || value.EndsWith(".bmp") || value.EndsWith(".jpeg");

        LinkInfo link = new LinkInfo(index, value, isImage);
        links.Add(link);

        nextIndex = link.Index + link.Value.Length;
      }

      return links.ToArray();
    }

    static string DecorLink(string content, string prefix, int position)
    {
      int index = content.IndexOf(prefix);
      if (index < 0)
        return content;

      return content;
    }
  }
}