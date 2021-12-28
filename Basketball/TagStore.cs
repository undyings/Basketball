using Commune.Basis;
using Commune.Data;
using Shop.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Basketball
{
	public class TagStore
	{
		public readonly ObjectHeadBox TagBox;
		public readonly Dictionary<string, int> TagIdByKey = new Dictionary<string, int>();
		readonly Dictionary<string, List<int>> tagIdsByWord = new Dictionary<string, List<int>>();

		public TagStore(ObjectHeadBox tagBox)
		{
			this.TagBox = tagBox;

			foreach (int tagId in tagBox.AllObjectIds)
			{
				string tagName = TagType.DisplayName.Get(tagBox, tagId);
				if (StringHlp.IsEmpty(tagName))
					continue;

				string tagKey = tagName.ToLower();
				TagIdByKey[tagKey] = tagId;
			}

			foreach (string key in TagIdByKey.Keys)
			{
				int tagId = TagIdByKey[key];

				string[] words = key.Split(new char[] { ' ', ',', '-' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string word in words)
				{
					List<int> ids;
					if (!tagIdsByWord.TryGetValue(word, out ids))
					{
						ids = new List<int>();
						tagIdsByWord[word] = ids;
					}
					ids.Add(tagId);
				}
			}
		}

		public int[] SearchByTags(IDataLayer fabricConnection, string searchQuery)
		{
			if (StringHlp.IsEmpty(searchQuery))
				return new int[0];

			searchQuery = searchQuery.ToLower();

			string[] words = searchQuery.Split(new char[] { ' ', ',', '-' }, StringSplitOptions.RemoveEmptyEntries);
			if (words.Length == 0)
				return new int[0];

			//IEnumerable<int> tagIds = DictionaryHlp.GetValueOrDefault(tagIdsByWord, words[0]);
			//if (tagIds == null)
			//	return new int[0];

			IEnumerable<int> intersectTagIds = null;
			foreach (string word in words)
			{
				IEnumerable<int> tagIds = DictionaryHlp.GetValueOrDefault(tagIdsByWord, word);
				if (tagIds == null)
					return new int[0];

				if (intersectTagIds == null)
					intersectTagIds = tagIds;
				else
					intersectTagIds = intersectTagIds.Intersect(tagIds);
			}

			List<Tuple<int, int>> tagIdsWithNewsCount = new List<Tuple<int, int>>();
			foreach (int tagId in intersectTagIds)
			{
				int[] newsIds = ViewTagHlp.GetNewsIdsForTag(fabricConnection, tagId);
				if (newsIds.Length > 0)
					tagIdsWithNewsCount.Add(new Tuple<int, int>(tagId, newsIds.Length));
			}

			return _.SortBy(tagIdsWithNewsCount, delegate (Tuple<int, int> tag) 
				{ return tag.Item2; }).Select(ti => ti.Item1).Reverse().ToArray();
		}

	}
}