﻿using System;
using System.Linq;
using NHibernate;
using VocaDb.Model.Domain.Globalization;
using VocaDb.Model.Domain;
using VocaDb.Model.Domain.Tags;
using VocaDb.Model.Service.Search;
using VocaDb.Model.Service.Search.Tags;

namespace VocaDb.Model.Service.Helpers {

	public static class FindHelpers {

		private const int MaxSearchWords = 6;

		/// <summary>
		/// Adds a filter for a list of names.
		/// </summary>
		/// <typeparam name="T">Entry name type</typeparam>
		/// <param name="query">Entry name query. Cannot be null.</param>
		/// <param name="textQuery">Name query filter. Cannot be null.</param>
		/// <returns>Filtered query. Cannot be null.</returns>
		public static IQueryable<T> AddEntryNameFilter<T>(IQueryable<T> query, SearchTextQuery textQuery)
			where T : LocalizedString {

			if (textQuery.IsEmpty)
				return query;

			var nameFilter = textQuery.Query;

			switch (textQuery.MatchMode) {
				case NameMatchMode.Exact:
					return query.Where(m => m.Value == nameFilter);

				case NameMatchMode.Partial:
					return query.Where(m => m.Value.Contains(nameFilter));

				case NameMatchMode.StartsWith:
					return query.Where(m => m.Value.StartsWith(nameFilter));

				case NameMatchMode.Words:
					var words = textQuery.Words;

					switch (words.Length) {
						case 1:
							query = query.Where(q => q.Value.Contains(words[0]));
							break;
						case 2:
							query = query.Where(q => q.Value.Contains(words[0]) && q.Value.Contains(words[1]));
							break;
						case 3:
							query = query.Where(q => q.Value.Contains(words[0]) && q.Value.Contains(words[1]) && q.Value.Contains(words[2]));
							break;
						case 4:
							query = query.Where(q => q.Value.Contains(words[0]) && q.Value.Contains(words[1]) && q.Value.Contains(words[2]) && q.Value.Contains(words[3]));
							break;
						case 5:
							query = query.Where(q => q.Value.Contains(words[0]) && q.Value.Contains(words[1]) && q.Value.Contains(words[2]) && q.Value.Contains(words[3]) && q.Value.Contains(words[4]));
							break;
						case 6:
							query = query.Where(q => q.Value.Contains(words[0]) && q.Value.Contains(words[1]) && q.Value.Contains(words[2]) && q.Value.Contains(words[3]) && q.Value.Contains(words[4]) && q.Value.Contains(words[5]));
							break;
					}
					return query;

			}

			return query;

		}

		public static IOrderedQueryable<T> AddNameOrder<T>(IQueryable<T> criteria, ContentLanguagePreference languagePreference) where T : IEntryWithNames {

			switch (languagePreference) {
				case ContentLanguagePreference.Japanese:
					return criteria.OrderBy(e => e.Names.SortNames.Japanese);
				case ContentLanguagePreference.English:
					return criteria.OrderBy(e => e.Names.SortNames.English);
				default:
					return criteria.OrderBy(e => e.Names.SortNames.Romaji);
			}

		}

		public static IQueryOver<TRoot, TSubType> AddNameOrder<TRoot, TSubType>(IQueryOver<TRoot, TSubType> criteria, 
			ContentLanguagePreference languagePreference) where TSubType : IEntryWithNames {

			switch (languagePreference) {
				case ContentLanguagePreference.Japanese:
					return criteria.OrderBy(e => e.Names.SortNames.Japanese).Asc;
				case ContentLanguagePreference.English:
					return criteria.OrderBy(e => e.Names.SortNames.English).Asc;
				default:
					return criteria.OrderBy(e => e.Names.SortNames.Romaji).Asc;
			}

		}

		public static IQueryable<Tag> AddTagNameFilter(IQueryable<Tag> query, TagSearchTextQuery textQuery) {

			if (textQuery.IsEmpty)
				return query;

			var name = textQuery.Query;

			switch (textQuery.MatchMode) {
				case NameMatchMode.Exact:
					return query.Where(m => m.Name == name);

				case NameMatchMode.StartsWith:
					return query.Where(m => m.Name.StartsWith(name));

				default:
					return query.Where(m => m.Name.Contains(name));
			}

		}

		/// <summary>
		/// Processes T-SQL wildcards, specifically the brackets "[]" and percent wildcard "%" from the search term.
		/// </summary>
		/// <param name="term">Search term, for example "alone [SNDI RMX]". Can be null or empty.</param>
		/// <returns>Cleaned (escaped) term, for example "alone [[]SNDI RMX]".</returns>
		/// <remarks>
		/// Because brackets are used for character group wildcards in T-SQL "like" queries, 
		/// searches such as "alone [SNDI RMX]" did not work.
		/// </remarks>
		public static string CleanTerm(string term) {

			if (string.IsNullOrEmpty(term))
				return term;

			return term.Replace("[", "[[]").Replace("%", "[%]");

		}

		/// <summary>
		/// Gets match mode and query for search.
		/// 
		/// Handles short query terms and wildcard searches ("*" in the end).
		/// Will only be applied if the current match mode is Auto.
		/// </summary>
		/// <param name="query">Text query. Can be null or empty.</param>
		/// <param name="matchMode">Current match mode. If Auto, will be set if something else besides Auto.</param>
		/// <param name="defaultMode">Default match mode to be used for normal queries.</param>
		/// <returns>Text query. Wildcard characters are removed. Can be null or empty, if original query is.</returns>
		public static string GetMatchModeAndQueryForSearch(string query, ref NameMatchMode matchMode, NameMatchMode defaultMode = NameMatchMode.Words) {

			if (string.IsNullOrEmpty(query))
				return query;

			// Remove SQL wildcard characters from the query, regardless of name match mode
			query = CleanTerm(query.Trim());

			// If name match mode is already decided, there's nothing more to do
			if (matchMode != NameMatchMode.Auto)
				return query;

			if (query.Length > 1 && query.EndsWith("*")) {
				matchMode = NameMatchMode.StartsWith;
				return query.Substring(0, query.Length - 1);
			}

			if (query.Length > 2 && query.StartsWith("\"") && query.EndsWith("\"")) {
				matchMode = NameMatchMode.Exact;
				return query.Substring(1, query.Length - 2);
			}

			if (query.Length <= 2) {
				matchMode = NameMatchMode.StartsWith;
				return query;
			}

			matchMode = defaultMode;
			return query;

		}

		public static string[] GetQueryWords(string query) {

			return query
				.Split(new[] { ' ' }, MaxSearchWords, StringSplitOptions.RemoveEmptyEntries)
				.Distinct()
				.ToArray();

		}

	}

}
