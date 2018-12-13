using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Commune.Data;

namespace Basketball
{
  public class CorrespondenceType
  {
    public readonly static FieldBlank<int> Id = new FieldBlank<int>("Id", IntLongConverter.Default);
    public readonly static FieldBlank<int> UserId = new FieldBlank<int>("UserId", IntLongConverter.Default);
    public readonly static FieldBlank<int> CollocutorId = new FieldBlank<int>("CollocutorId", IntLongConverter.Default);
    public readonly static FieldBlank<bool> Inbox = new FieldBlank<bool>("Inbox", BoolLongConverter.Default);
    public readonly static FieldBlank<string> Content = new FieldBlank<string>("Content");
    public readonly static FieldBlank<DateTime> CreateTime = new FieldBlank<DateTime>("CreateTime");

    public readonly static SingleIndexBlank MessageById = new SingleIndexBlank("MessageById", Id);
  }

  public class DialogueType
  {
    public readonly static FieldBlank<int> Id = new FieldBlank<int>("Id", IntLongConverter.Default);
    public readonly static FieldBlank<int> UserId = new FieldBlank<int>("UserId", IntLongConverter.Default);
    public readonly static FieldBlank<int> CollocutorId = new FieldBlank<int>("CollocutorId", IntLongConverter.Default);
    public readonly static FieldBlank<bool> Inbox = new FieldBlank<bool>("Inbox", BoolLongConverter.Default);
    public readonly static FieldBlank<string> Content = new FieldBlank<string>("Content");
    public readonly static FieldBlank<DateTime> ModifyTime = new FieldBlank<DateTime>("ModifyTime");
    public readonly static FieldBlank<bool> Unread = new FieldBlank<bool>("Unread", BoolLongConverter.Default);

    public readonly static SingleIndexBlank DialogueById = new SingleIndexBlank("DialogueById", Id);
  }

  public class DialogReadType
  {
    public readonly static FieldBlank<int> UserId = DialogueType.UserId;
    public readonly static FieldBlank<int> Count = new FieldBlank<int>("Count", IntLongConverter.Default);

    public readonly static SingleIndexBlank UnreadByUserId = new SingleIndexBlank("ReadByUserId", UserId);
  }
}