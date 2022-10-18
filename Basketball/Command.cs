using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Basketball
{
	public class Command
	{
		public const string SaveCommentAdd = "save_comment_add";
		public const string SaveAnswer = "save_answer";
		public const string SaveMessageAdd = "save_message_add";
		public const string SaveNewsAdd = "save_news_add";

		public const string SaveArticleAdd = "save_article_add";

		public static string GetLocalStorageKey(string command)
		{
			switch (command)
			{
				case SaveCommentAdd:
				case SaveAnswer:
				case SaveMessageAdd:
					return "addComment";
				case SaveNewsAdd:
					return "addText";
				default:
					return "";
			}
		}

		public static string GetHint(string command, object id)
		{
			switch (command)
			{
				case SaveCommentAdd:
					return "commentAdd";
				case SaveAnswer:
					return string.Format("answer_{0}", id);
				case SaveMessageAdd:
					return "messageAdd";
				case SaveNewsAdd:
					return "newsAdd";
				case SaveArticleAdd:
					return "articleAdd";
				default:
					return "";
			}
		}
	}
}