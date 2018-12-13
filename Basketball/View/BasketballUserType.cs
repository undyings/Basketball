using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Commune.Data;
using Shop.Engine;

namespace Basketball
{
  public class BasketballUserType : UserType
  {
    public readonly static RowPropertyBlank<string> Interests  = DataBox.Create(1111, DataBox.StringValue);
    public readonly static RowPropertyBlank<string> Country = DataBox.Create(1112, DataBox.StringValue);
    public readonly static RowPropertyBlank<string> City = DataBox.Create(1113, DataBox.StringValue);
    public readonly static RowPropertyBlank<string> AboutMe = DataBox.Create(1114, DataBox.StringValue);
    public readonly static RowPropertyBlank<string> CommunityMember = DataBox.Create(1115, DataBox.StringValue);

    public readonly static RowPropertyBlank<bool> LockedUserIds = DataBox.Create(1120, DataBox.BoolValue);

    public readonly static RowPropertyBlank<bool> IsModerator = DataBox.Create(1201, DataBox.BoolValue);
    public readonly static RowPropertyBlank<DateTime?> BannedUntil = DataBox.Create(1202, DataBox.DateTimeNullableValue);
    public readonly static RowPropertyBlank<DateTime?> NotRedactorUntil = DataBox.Create(1203, DataBox.DateTimeNullableValue);
  }

  public class ArticleType : NewsType
  {
    public const int Article = 5010;

    public readonly static RowPropertyBlank<string> Author = DataBox.Create(5200, DataBox.StringValue);
  }

  public class TopicType : NewsType
  {
    public const int Topic = 5020;

    public readonly static LinkKindBlank TagLinks = new LinkKindBlank(5600);
  }

  public class ForumSectionType : SectionType
  {
    public readonly static LinkKindBlank TopicLinks = new LinkKindBlank(17600);
  }

  public class TagType
  {
    public const int Tag = 5030;

    public readonly static XmlDisplayName DisplayName = new XmlDisplayName();
  }

  public class OptionType
  {
    public readonly static FieldBlank<int> CorrespondencePageIndex = new FieldBlank<int>("CorrespondencePageIndex");
  }

}