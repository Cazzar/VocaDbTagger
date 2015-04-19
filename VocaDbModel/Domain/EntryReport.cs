﻿using System;
using VocaDb.Model.Domain.Users;
using VocaDb.Model.Domain.Versioning;

namespace VocaDb.Model.Domain {

	public abstract class EntryReport {
		
		public const int MaxNotesLength = 400;

		private string hostname;
		private string notes;

		protected EntryReport() {
			Created = DateTime.Now;
			Notes = string.Empty;
		}

		protected EntryReport(User user, string hostname, string notes, int? versionNumber)
			: this() {

			User = user;
			Hostname = hostname;
			Notes = notes;
			VersionNumber = versionNumber;

		}

		public virtual DateTime Created { get; set; }

		public abstract IEntryWithNames EntryBase { get; }

		public virtual EntryType EntryType {
			get { return EntryBase.EntryType; }
		}

		public virtual string Hostname {
			get { return hostname; }
			set { hostname = value; }
		}

		public virtual int Id { get; set; }

		public virtual string Notes {
			get { return notes; }
			set {
				ParamIs.NotNull(() => value);
				notes = value;
			}
		}

		public virtual User User { get; set; }

		public virtual ArchivedObjectVersion VersionBase
		{
			get { return null; }
		}

		public virtual int? VersionNumber { get; set; }

	}
}
