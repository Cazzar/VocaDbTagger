﻿using System;
using System.Data;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using NLog;
using VocaDb.Model.Domain.Globalization;
using NHibernate;
using NHibernate.Linq;
using VocaDb.Model.DataContracts;
using VocaDb.Model.DataContracts.Artists;
using VocaDb.Model.DataContracts.UseCases;
using VocaDb.Model.Domain;
using VocaDb.Model.Domain.Artists;
using VocaDb.Model.Domain.Security;
using VocaDb.Model.Service.Helpers;
using VocaDb.Model.Domain.Albums;
using VocaDb.Model.DataContracts.Albums;
using System.Drawing;
using VocaDb.Model.Helpers;
using System.Collections.Generic;
using VocaDb.Model.Service.QueryableExtenders;
using VocaDb.Model.Service.Repositories;
using VocaDb.Model.Service.Search.Artists;

namespace VocaDb.Model.Service {

	public class ArtistService : ServiceBase {

		private readonly IEntryUrlParser entryUrlParser;

// ReSharper disable UnusedMember.Local
		private static readonly Logger log = LogManager.GetCurrentClassLogger();
// ReSharper restore UnusedMember.Local

		public PartialFindResult<Artist> Find(ISession session, ArtistQueryParams queryParams) {

			var context = new NHibernateRepositoryContext<Artist>(session, PermissionContext);
			return new ArtistSearch(LanguagePreference, context, entryUrlParser).Find(queryParams);

		}

		public ArtistService(ISessionFactory sessionFactory, IUserPermissionContext permissionContext, IEntryLinkFactory entryLinkFactory, IEntryUrlParser entryUrlParser)
			: base(sessionFactory, permissionContext, entryLinkFactory) {
			
			this.entryUrlParser = entryUrlParser;

		}

		public ArtistForAlbumContract AddAlbum(int artistId, int albumId) {

			VerifyManageDatabase();

			return HandleTransaction(session => {

				var artist = session.Load<Artist>(artistId);
				var album = session.Load<Album>(albumId);

				var hasAlbum = session.Query<ArtistForAlbum>().Any(a => a.Artist.Id == artistId && a.Album.Id == albumId);

				if (hasAlbum)
					throw new LinkAlreadyExistsException(string.Format("{0} already has {1}", artist, album));

				AuditLog(string.Format("adding {0} for {1}", 
					EntryLinkFactory.CreateEntryLink(album), EntryLinkFactory.CreateEntryLink(artist)), session);

				var artistForAlbum = artist.AddAlbum(album);
				session.Save(artistForAlbum);

				album.UpdateArtistString();
				session.Update(album);

				return new ArtistForAlbumContract(artistForAlbum, PermissionContext.LanguagePreference);

			});

		}

		public void Archive(ISession session, Artist artist, ArtistDiff diff, ArtistArchiveReason reason, string notes = "") {

			SysLog("Archiving " + artist);

			var agentLoginData = SessionHelper.CreateAgentLoginData(session, PermissionContext);
			var archived = ArchivedArtistVersion.Create(artist, diff, agentLoginData, reason, notes);
			session.Save(archived);

		}

		public void Archive(ISession session, Artist artist, ArtistArchiveReason reason, string notes = "") {

			Archive(session, artist, new ArtistDiff(), reason, notes);

		}

		public void Delete(int id, string notes) {

			UpdateEntity<Artist>(id, (session, a) => {

				AuditLog(string.Format("deleting artist {0}", EntryLinkFactory.CreateEntryLink(a)), session);

				NHibernateUtil.Initialize(a.Picture);
				a.Delete();
			          
				Archive(session, a, new ArtistDiff(false), ArtistArchiveReason.Deleted, notes);
               
			}, PermissionToken.DeleteEntries, skipLog: true);

		}

		public PartialFindResult<ArtistContract> FindArtists(ArtistQueryParams queryParams) {

			return FindArtists(a => new ArtistContract(a, PermissionContext.LanguagePreference), queryParams);

		}

		public PartialFindResult<T> FindArtists<T>(Func<Artist, T> fac, ArtistQueryParams queryParams) {

			return HandleQuery(session => {

				var result = Find(session, queryParams);

				return new PartialFindResult<T>(result.Items.Select(fac).ToArray(),
					result.TotalCount, result.Term, result.FoundExactMatch);

			});

		}

		public EntryRefWithCommonPropertiesContract[] FindDuplicates(string[] anyName, string url) {

			var names = anyName.Select(n => n.Trim()).Where(n => n != string.Empty).ToArray();
			var urlTrimmed = url != null ? url.Trim() : null;

			if (!names.Any() && string.IsNullOrEmpty(url))
				return new EntryRefWithCommonPropertiesContract[] { };

			return HandleQuery(session => {

				// TODO: moved Distinct after ToArray to work around NH bug
				var nameMatches = (names.Any() ? session.Query<ArtistName>()
					.Where(n => names.Contains(n.Value) && !n.Artist.Deleted)
					.OrderBy(n => n.Artist)
					.Select(n => n.Artist)
					.Take(10)
					.ToArray()
					.Distinct(): new Artist[] {});

				var linkMatches = !string.IsNullOrEmpty(urlTrimmed) ?
					session.Query<ArtistWebLink>()
					.Where(w => w.Url == urlTrimmed)
					.Select(w => w.Artist)
					.Take(10)
					.ToArray()
					.Distinct() : new Artist[] {};

				return nameMatches.Union(linkMatches)
					.Select(n => new EntryRefWithCommonPropertiesContract(n, PermissionContext.LanguagePreference))
					.ToArray();

			});

		}

		public string[] FindNames(ArtistSearchTextQuery textQuery, int maxResults) {

			if (textQuery.IsEmpty)
				return new string[] {};

			return HandleQuery(session => {

				var names = session.Query<ArtistName>()
					.Where(a => !a.Artist.Deleted)
					.WhereArtistNameIs(textQuery)
					.Select(n => n.Value)
					.OrderBy(n => n)
					.Distinct()
					.Take(maxResults)
					.ToArray();

				return NameHelper.MoveExactNamesToTop(names, textQuery.Query);

			});

		}

		public EntryForPictureDisplayContract GetArchivedArtistPicture(int archivedVersionId) {

			return HandleQuery(session =>
				EntryForPictureDisplayContract.Create(
				session.Load<ArchivedArtistVersion>(archivedVersionId), LanguagePreference));

		}

		public ArtistContract GetArtist(int id) {

			return HandleQuery(session => new ArtistContract(session.Load<Artist>(id), LanguagePreference));

		}

		public ArtistForEditContract GetArtistForEdit(int id) {

			return
				HandleQuery(session =>
					new ArtistForEditContract(session.Load<Artist>(id), PermissionContext.LanguagePreference));

		}

		public ArtistContract GetArtistWithAdditionalNames(int id) {

			return HandleQuery(session => new ArtistContract(session.Load<Artist>(id), PermissionContext.LanguagePreference));

		}

		/// <summary>
		/// Gets the picture for a <see cref="Artist"/>.
		/// </summary>
		/// <param name="id">Artist Id.</param>
		/// <param name="requestedSize">Requested size. If Empty, original size will be returned.</param>
		/// <returns>Data contract for the picture. Can be null if there is no picture.</returns>
		public EntryForPictureDisplayContract GetArtistPicture(int id, Size requestedSize) {

			return HandleQuery(session => 
				EntryForPictureDisplayContract.Create(session.Load<Artist>(id), PermissionContext.LanguagePreference, requestedSize));

		}

		public ArtistWithArchivedVersionsContract GetArtistWithArchivedVersions(int artistId) {

			return HandleQuery(session => new ArtistWithArchivedVersionsContract(
				session.Load<Artist>(artistId), PermissionContext.LanguagePreference));

		}

		public ArtistForApiContract[] GetArtistsWithYoutubeChannels(ContentLanguagePreference languagePreference) {

			return HandleQuery(session => {

				var contracts = session.Query<ArtistWebLink>()
					.Where(l => !l.Artist.Deleted 
						&& (l.Artist.ArtistType == ArtistType.Producer || l.Artist.ArtistType == ArtistType.Circle || l.Artist.ArtistType == ArtistType.Animator) 
						&& (l.Url.Contains("youtube.com/user/") || l.Url.Contains("youtube.com/channel/")))
					.Select(l => l.Artist)
					.Distinct()
					.ToArray()
					.Select(a => new ArtistForApiContract(a, languagePreference, null, false, ArtistOptionalFields.WebLinks))
					.ToArray();

				return contracts;

			});

		}

		public EntryWithTagUsagesContract GetEntryWithTagUsages(int artistId) {

			return HandleQuery(session => {

				var artist = session.Load<Artist>(artistId);
				return new EntryWithTagUsagesContract(artist, artist.Tags.Usages);

			});

		}

		public ArchivedArtistVersionDetailsContract GetVersionDetails(int id, int comparedVersionId) {

			return HandleQuery(session =>
				new ArchivedArtistVersionDetailsContract(session.Load<ArchivedArtistVersion>(id),
					comparedVersionId != 0 ? session.Load<ArchivedArtistVersion>(comparedVersionId) : null, PermissionContext.LanguagePreference));

		}

		public XDocument GetVersionXml(int id) {
			return HandleQuery(session => session.Load<ArchivedArtistVersion>(id).Data);
		}

		public void Merge(int sourceId, int targetId) {

			PermissionContext.VerifyPermission(PermissionToken.MergeEntries);

			if (sourceId == targetId)
				throw new ArgumentException("Source and target artists can't be the same", "targetId");

			HandleTransaction(session => {

				var source = session.Load<Artist>(sourceId);
				var target = session.Load<Artist>(targetId);

				AuditLog(string.Format("Merging {0} to {1}", 
					EntryLinkFactory.CreateEntryLink(source), EntryLinkFactory.CreateEntryLink(target)), session);

				NHibernateUtil.Initialize(source.Picture);
				NHibernateUtil.Initialize(target.Picture);

				foreach (var n in source.Names.Names.Where(n => !target.HasName(n))) {
					var name = target.CreateName(n.Value, n.Language);
					session.Save(name);
				}

				foreach (var w in source.WebLinks.Where(w => !target.HasWebLink(w.Url))) {
					var link = target.CreateWebLink(w.Description, w.Url, w.Category);
					session.Save(link);
				}

				var groups = source.Groups.Where(g => !target.HasGroup(g.Group)).ToArray();
				foreach (var g in groups) {
					g.MoveToMember(target);
					session.Update(g);
				}

				var members = source.Members.Where(m => !m.Member.HasGroup(target)).ToArray();
				foreach (var m in members) {
					m.MoveToGroup(target);
					session.Update(m);
				}

				var albums = source.Albums.Where(a => !target.HasAlbum(a.Album)).ToArray();
				foreach (var a in albums) {
					a.Move(target);
					session.Update(a);
				}

				var songs = source.Songs.Where(s => !target.HasSong(s.Song)).ToArray();
				foreach (var s in songs) {
					s.Move(target);
					session.Update(s);
				}

				var ownerUsers = source.OwnerUsers.Where(s => !target.HasOwnerUser(s.User)).ToArray();
				foreach (var u in ownerUsers) {
					u.Move(target);
					session.Update(u);
				}

				var pictures = source.Pictures.ToArray();
				foreach (var p in pictures) {
					p.Move(target);
					session.Update(p);
				}

				var users = source.Users.ToArray();
				foreach (var u in users) {
					u.Move(target);
					session.Update(u);
				}

				if (target.Description.Original == string.Empty)
					target.Description.Original = source.Description.Original;

				if (target.Description.English == string.Empty)
					target.Description.English = source.Description.English;

				// Create merge record
				var mergeEntry = new ArtistMergeRecord(source, target);
				session.Save(mergeEntry);

				source.Deleted = true;

				Archive(session, target, ArtistArchiveReason.Merged, string.Format("Merged from '{0}'", source));

				NHibernateUtil.Initialize(source.Picture);
				NHibernateUtil.Initialize(target.Picture);

				session.Update(source);
				session.Update(target);

			});

		}

		public int RemoveTagUsage(long tagUsageId) {

			return RemoveTagUsage<ArtistTagUsage>(tagUsageId);

		}

		public void Restore(int artistId) {

			PermissionContext.VerifyPermission(PermissionToken.DeleteEntries);

			HandleTransaction(session => {

				var artist = session.Load<Artist>(artistId);

				NHibernateUtil.Initialize(artist.Picture);
				artist.Deleted = false;

				session.Update(artist);

				Archive(session, artist, new ArtistDiff(false), ArtistArchiveReason.Restored);

				AuditLog("restored " + EntryLinkFactory.CreateEntryLink(artist), session);

			});

		}

		/// <summary>
		/// Reverts an album to an earlier archived version.
		/// </summary>
		/// <param name="archivedArtistVersionId">Id of the archived version to be restored.</param>
		/// <returns>Result of the revert operation, with possible warnings if any. Cannot be null.</returns>
		/// <remarks>Requires the RestoreRevisions permission.</remarks>
		public EntryRevertedContract RevertToVersion(int archivedArtistVersionId) {

			PermissionContext.VerifyPermission(PermissionToken.RestoreRevisions);

			return HandleTransaction(session => {

				var archivedVersion = session.Load<ArchivedArtistVersion>(archivedArtistVersionId);
				var artist = archivedVersion.Artist;

				SysLog("reverting " + artist + " to version " + archivedVersion.Version);

				var fullProperties = ArchivedArtistContract.GetAllProperties(archivedVersion);
				var warnings = new List<string>();

				artist.ArtistType = fullProperties.ArtistType;
				artist.Description.Original = fullProperties.Description;
				artist.Description.English = fullProperties.DescriptionEng ?? string.Empty;
				artist.TranslatedName.DefaultLanguage = fullProperties.TranslatedName.DefaultLanguage;
				artist.BaseVoicebank = SessionHelper.RestoreWeakRootEntityRef<Artist>(session, warnings, fullProperties.BaseVoicebank);

				// Picture
				var versionWithPic = archivedVersion.GetLatestVersionWithField(ArtistEditableFields.Picture);

				if (versionWithPic != null)
					artist.Picture = versionWithPic.Picture;

				/*
				// Albums
				SessionHelper.RestoreObjectRefs<ArtistForAlbum, Album>(
					session, warnings, artist.AllAlbums, fullProperties.Albums, (a1, a2) => (a1.Album.Id == a2.Id),
					album => (!artist.HasAlbum(album) ? artist.AddAlbum(album) : null),
					albumForArtist => albumForArtist.Delete());
				 */

				// Groups
				SessionHelper.RestoreObjectRefs<GroupForArtist, Artist>(
					session, warnings, artist.AllGroups, fullProperties.Groups, (a1, a2) => (a1.Group.Id == a2.Id),
					grp => (!artist.HasGroup(grp) ? artist.AddGroup(grp) : null),
					groupForArtist => groupForArtist.Delete());

				/*
				// Members
				SessionHelper.RestoreObjectRefs<GroupForArtist, Artist>(
					session, warnings, artist.AllMembers, fullProperties.Members, (a1, a2) => (a1.Member.Id == a2.Id),
					member => (!artist.HasMember(member) ? artist.AddMember(member) : null),
					groupForArtist => groupForArtist.Delete());
				 */

				// Names
				if (fullProperties.Names != null) {
					var nameDiff = artist.Names.SyncByContent(fullProperties.Names, artist);
					SessionHelper.Sync(session, nameDiff);
				}

				// Weblinks
				if (fullProperties.WebLinks != null) {
					var webLinkDiff = WebLink.SyncByValue(artist.WebLinks, fullProperties.WebLinks, artist);
					SessionHelper.Sync(session, webLinkDiff);
				}

				Archive(session, artist, ArtistArchiveReason.Reverted, string.Format("Reverted to version {0}", archivedVersion.Version));
				AuditLog(string.Format("reverted {0} to revision {1}", EntryLinkFactory.CreateEntryLink(artist), archivedVersion.Version), session);

				return new EntryRevertedContract(artist, warnings);

			});

		}

	}

	public enum ArtistSortRule {

		/// <summary>
		/// Not sorted (random order)
		/// </summary>
		None,

		/// <summary>
		/// Sort by name (ascending)
		/// </summary>
		Name,

		/// <summary>
		/// Sort by addition date (descending)
		/// </summary>
		AdditionDate,

		/// <summary>
		/// Sort by addition date (ascending)
		/// </summary>
		AdditionDateAsc,

		SongCount,

		SongRating,

		FollowerCount

	}

}
