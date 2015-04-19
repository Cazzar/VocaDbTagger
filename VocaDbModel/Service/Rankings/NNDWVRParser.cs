using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Remotion.Linq.Parsing;
using VocaDb.Model.DataContracts.Ranking;
using VocaDb.Model.Service.VideoServices;

namespace VocaDb.Model.Service.Rankings {

	public class NNDWVRParser {

		private static readonly Regex wvrIdRegex = new Regex(@"#(\d{3})");

		public RankingContract GetSongs(string url, bool parseAll) {

			throw new NotImplementedException();

		}

	}

}
