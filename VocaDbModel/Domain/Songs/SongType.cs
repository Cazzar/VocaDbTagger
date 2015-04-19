﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace VocaDb.Model.Domain.Songs {

	public enum SongType {

		Unspecified		= 0,

		Original		= 1,

		Remaster		= 2,

		Remix			= 4,

		Cover			= 8,

		Instrumental	= 16,

		Mashup			= 32,

		MusicPV			= 64,

		DramaPV			= 128,

		Live			= 256,

		Other			= 512

	}

	[Flags]
	public enum SongTypes {

		Unspecified		= 0,

		Original		= 1,

		Remaster		= 2,

		Remix			= 4,

		Cover			= 8,

		Instrumental	= 16,

		Mashup			= 32,

		MusicPV			= 64,

		DramaPV			= 128,

		Live			= 256,

		Other			= 512

	}

	public static class SongTypesExtender {

		public static IEnumerable<SongType> ToIndividualSelections(this SongTypes selections, bool skipUnspecified = false) {
			
			return EnumVal<SongTypes>
				.GetIndividualValues(selections)
				.Where(t => !skipUnspecified || t != SongTypes.Unspecified)
				.Select(s => (SongType)s);

		}

	}

}
