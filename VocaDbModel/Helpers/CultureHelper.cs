﻿using System.Globalization;

namespace VocaDb.Model.Helpers {

	public static class CultureHelper {

		public static CultureInfo Default
		{
			get
			{
				return CultureInfo.InvariantCulture;
			}
		}

		/// <summary>
		/// Gets the culture with the specific name, or the application default culture (English basically).
		/// </summary>
		/// <param name="cultureName">Culture name, for example "en-US".</param>
		/// <returns>The specified culture, or application default culture. Cannot be null.</returns>
		public static CultureInfo GetCultureOrDefault(string cultureName) {
			
			if (string.IsNullOrEmpty(cultureName))
				return Default;

			return CultureInfo.GetCultureInfo(cultureName);

		}

	}
}
