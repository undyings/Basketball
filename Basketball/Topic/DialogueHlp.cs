using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Commune.Data;
using Commune.Basis;

namespace Basketball
{
  public class DialogueHlp
  {
    public static void MarkReadCorrespondence(IDataLayer forumConnection, int userId, int collocutorId)
    {
      forumConnection.GetScalar("", "Update dialogue Set unread = 0 Where user_id = @userId and collocutor_id = @collocutorId",
        new DbParameter("userId", userId), new DbParameter("collocutorId", collocutorId)
      );
    }

    public static void SendMessage(BasketballContext context, int senderId, int recipientId, string content)
    {
      IDataLayer forumConnection = context.ForumConnection;

      DateTime createTime = DateTime.UtcNow;

      if (senderId != recipientId)
        DialogueHlp.InsertMessage(forumConnection, recipientId, senderId, true, content, createTime);
      DialogueHlp.InsertMessage(forumConnection, senderId, recipientId, false, content, createTime);

      if (senderId != recipientId)
        DialogueHlp.UpdateDialog(forumConnection, recipientId, senderId, true, content, createTime);
      DialogueHlp.UpdateDialog(forumConnection, senderId, recipientId, false, content, createTime);

      context.UpdateUnreadDialogs();
    }

    public static void InsertMessage(IDataLayer forumConnection,
      int userId, int collocutorId, bool inbox, string content, DateTime createTime)
    {
      forumConnection.GetScalar("",
        "INSERT INTO correspondence (user_id, collocutor_id, inbox, content, create_time) VALUES (@userId, @collocutorId, @inbox, @content, @createTime);",
        new DbParameter("userId", userId), new DbParameter("collocutorId", collocutorId),
        new DbParameter("inbox", inbox ? 1 : 0), new DbParameter("content", content),
        new DbParameter("createTime", createTime)
      );
    }

    public static void UpdateDialog(IDataLayer forumConnection, int userId, int collocutorId,
      bool inbox, string content, DateTime modifyTime)
    {
      forumConnection.GetScalar("",
        "INSERT OR REPLACE INTO dialogue (user_id, collocutor_id, inbox, content, modify_time, unread) VALUES (@userId, @collocutorId, @inbox, @content, @modifyTime, @unread);",
        new DbParameter("userId", userId), new DbParameter("collocutorId", collocutorId),
        new DbParameter("inbox", inbox ? 1 : 0), new DbParameter("content", content),
        new DbParameter("modifyTime", modifyTime), new DbParameter("unread", inbox ? 1 : 0)
      );
    }

    public static TableLink LoadCorrespondenceLink(IDataLayer forumConnection, string conditionWithoutWhere,
      params DbParameter[] conditionParameters)
    {
      return TableLink.Load(forumConnection,
        new FieldBlank[] {
          CorrespondenceType.Id,
          CorrespondenceType.UserId,
          CorrespondenceType.CollocutorId,
          CorrespondenceType.Inbox,
          CorrespondenceType.Content,
          CorrespondenceType.CreateTime
        },
        new IndexBlank[] {
          CorrespondenceType.MessageById
        }, "",
        "Select id, user_id, collocutor_id, inbox, content, create_time From correspondence",
        conditionWithoutWhere, conditionParameters
      );
    }

    public static TableLink LoadUnreadLink(IDataLayer forumConnection)
    {
      return TableLink.Load(forumConnection,
        new FieldBlank[] {
          DialogReadType.UserId,
          DialogReadType.Count
        },
        new IndexBlank[] {
          DialogReadType.UnreadByUserId
        }, "",
        "Select user_id, count(*) From dialogue",
        "unread = 1 group by user_id"
      );
    }

    public static TableLink LoadDialogueLink(IDataLayer forumConnection, string conditionWithoutWhere,
      params DbParameter[] conditionParameters)
    {
      return TableLink.Load(forumConnection,
        new FieldBlank[] {
          DialogueType.Id,
          DialogueType.UserId,
          DialogueType.CollocutorId,
          DialogueType.Inbox,
          DialogueType.Content,
          DialogueType.ModifyTime,
          DialogueType.Unread
        },
        new IndexBlank[] {
          DialogueType.DialogueById
        }, "",
        "Select id, user_id, collocutor_id, inbox, content, modify_time, unread From dialogue",
        conditionWithoutWhere, conditionParameters
      );
    }

    public static void CheckAndCreateDialogueTables(IDataLayer forumConnection)
    {
      if (!SQLiteDatabaseHlp.TableExist(forumConnection, "correspondence"))
      {
        CreateTableForCorrespondence(forumConnection);
        Logger.AddMessage("Создана таблица переписки");
      }

      if (!SQLiteDatabaseHlp.TableExist(forumConnection, "dialogue"))
      {
        CreateTableForDialogue(forumConnection);
        Logger.AddMessage("Создана таблица диалогов");
      }
    }

    public static void CreateTableForCorrespondence(IDataLayer forumConnection)
    {
      forumConnection.GetScalar("",
        @"CREATE TABLE correspondence (
            id            integer PRIMARY KEY AUTOINCREMENT,
            user_id       integer NOT NULL,
            collocutor_id integer NOT NULL,
            inbox         integer NOT NULL,
            content       text,
            create_time   datetime NOT NULL,
            xml           text
          );

          CREATE INDEX correspondence_by_user_collocutor_time
            ON correspondence
            (user_id, collocutor_id, create_time);
         "
      );
    }

    public static void CreateTableForDialogue(IDataLayer forumConnection)
    {
      forumConnection.GetScalar("",
        @"CREATE TABLE dialogue (
            id            integer PRIMARY KEY AUTOINCREMENT,
            user_id       integer NOT NULL,
            collocutor_id integer NOT NULL,
            inbox         integer NOT NULL,
            content       text,
            modify_time   datetime NOT NULL,
            unread        integer NOT NULL
          );

          CREATE UNIQUE INDEX dialogue_by_user_collocutor_time
            ON dialogue
            (user_id, collocutor_id);

          CREATE INDEX dialogue_by_user_time
            ON dialogue
            (user_id, modify_time);

          CREATE INDEX dialogue_by_unread_user
            ON dialogue
            (unread, user_id);
         "
      );
    }
  }
}