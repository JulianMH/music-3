using MusicConcept.Library.LibrarySource;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using System.IO;

namespace MusicConcept.Library
{
    public class MusicLibrary : NotifyPropertyChangedObject
    {
        public const String DatabaseFileName = "MusicLibrary";

        private SQLiteAsyncConnection databaseConnection;

        private const bool ignorePodcasts = true;

        private LocalLibrarySource localLibrarySource;

        public ObservableCollection<Song> Songs { get; private set; }
        public ObservableCollection<string> Artists { get; private set; }
        public ObservableCollection<Album> Albums { get; private set; }
        public ObservableCollection<Genre> Genres { get; private set; }
        public ObservableCollection<SavedPlaylist> SavedPlaylists { get; private set; }
        public ListPlaylistManager PlaylistManager { get; private set; }

        private static List<Func<MusicLibrary, Task>> necessaryUpdatesForLibrary;
        public async static Task<SQLiteAsyncConnection> ConnectToDatabase()
        {
            var databaseConnection = new SQLiteAsyncConnection(DatabaseFileName, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.NoMutex);

            var version = "1.3";
            var oldVersion = ApplicationSettings.DatabaseCreationVersion.Read();
            if (oldVersion != version)
            {
                await databaseConnection.CreateTableAsync<Song>();
                await databaseConnection.CreateTableAsync<SongGenre>();
                await databaseConnection.CreateTableAsync<Genre>();

                await databaseConnection.CreateTableAsync<CachedAlbumCover>();
                await databaseConnection.CreateTableAsync<CurrentPlaylistSong>();
                await databaseConnection.CreateTableAsync<SavedPlaylistSong>();
                await databaseConnection.CreateTableAsync<SavedPlaylist>();

                necessaryUpdatesForLibrary = new List<Func<MusicLibrary, Task>>();
                ApplicationSettings.IsDatabaseSettingUp.Save(true);
                if (oldVersion == "1.0" || oldVersion == "1.1" || oldVersion == "1.3")
                {
                     necessaryUpdatesForLibrary.Add(async library =>
                     {
                         foreach (var song in library.Songs)
                         {
                             try
                             {
                                 var storageFile = await StorageFile.GetFileFromPathAsync(song.FileName);
                                 TagLib.Tag tag = null;
                                 using (var stream = await storageFile.OpenStreamForReadAsync())
                                 {
                                     tag = await Task<TagLib.File>.Run(() =>
                                     {
                                         try
                                         {
                                             return TagLib.File.Create(new TagLib.StreamFileAbstraction(storageFile.Name, stream, null)).Tag;
                                         }
                                         catch { return null; }
                                     });
                                 }

                                 if (tag != null)
                                 { 
                                     library.localLibrarySource.UpdateSongMetadata(tag, song);
                                     await library.databaseConnection.UpdateAsync(song);
                                 }
                             }
                             catch { }
                         }
                     });
                }

                necessaryUpdatesForLibrary.Add(async library => { ApplicationSettings.DatabaseCreationVersion.Save(version); await Task.Delay(0); });

            }


            return databaseConnection;
        }

        private MusicLibrary(LocalLibrarySource localLibrarySource) { this.localLibrarySource = localLibrarySource; }

        public static async Task<MusicLibrary> Open(LocalLibrarySource localLibrarySource, params PropertyChangedEventHandler[] propertyChangedEventHandlers)
        {
            MusicLibrary library = new MusicLibrary(localLibrarySource);

            if (propertyChangedEventHandlers != null)
                foreach (var handler in propertyChangedEventHandlers)
                    library.PropertyChanged += handler;
            library.databaseConnection = await ConnectToDatabase();
            library.PlaylistManager = new ListPlaylistManager(library.databaseConnection);
            library.Songs = new ObservableCollection<Song>(await library.databaseConnection.Table<Song>().ToListAsync());
            library.Artists = new ObservableCollection<string>(library.Songs.Select(p => p.AlbumArtist).Where(p => p.Trim() != "").Distinct(StringComparer.CurrentCultureIgnoreCase));
            library.Albums = new ObservableCollection<Album>(library.Songs.Select(p => new Album(p.Album, p.AlbumArtist, library)).Where(p => p.Name.Trim() != "").Distinct());
            library.SavedPlaylists = new ObservableCollection<SavedPlaylist>(await library.databaseConnection.Table<SavedPlaylist>().ToListAsync());
            library.Genres = new ObservableCollection<Genre>(await library.databaseConnection.Table<Genre>().OrderBy(p => p.Name).ToListAsync());
            library.NotifyPropertyChanged("Songs");
            library.NotifyPropertyChanged("Artists");
            library.NotifyPropertyChanged("Albums");
            library.NotifyPropertyChanged("PlaylistManager");
            library.NotifyPropertyChanged("SavedPlaylists");
            library.NotifyPropertyChanged("Genres");

            return library;
        }

        public void Close()
        {
            this.databaseConnection.Close();
        }

        private List<Song> songsToAdd = new List<Song>();
        private List<Tuple<Song, string>> songGenresToAdd = new List<Tuple<Song, string>>();

        private async Task AddAllRemainingSongs()
        {
            var newAlbums = songsToAdd.Select(p => new Tuple<string, string>(p.Album, p.AlbumArtist)).Distinct()
                .Where(p => p.Item1.Trim() != "" && !this.Albums.Any(q => q.Artist == p.Item2 && q.Name == p.Item1)).Select(p => new Album(p.Item1, p.Item2, this));
            foreach (var newAlbum in newAlbums) this.Albums.Add(newAlbum);
            var newArtists = songsToAdd.Select(p => p.AlbumArtist).Distinct(StringComparer.OrdinalIgnoreCase).Where(p => !this.Artists.Contains(p, StringComparer.OrdinalIgnoreCase));
            foreach (var newArtist in newArtists) this.Artists.Add(newArtist);

            foreach (var song in songsToAdd)
            {
                var exisitingSong = this.Songs.Where(p => p.FileName == song.FileName).FirstOrDefault();
                if (exisitingSong != null)
                {
                    song.Id = exisitingSong.Id;
                    await databaseConnection.UpdateAsync(song);
                    this.Songs.Remove(song);
                }
                else
                    await databaseConnection.InsertAsync(song);
                this.Songs.Add(song);

                await databaseConnection.ExecuteAsync("delete from SongGenre where SongId = ?", song.Id);
            }

            foreach (var genre in songGenresToAdd)
            {
                var genreInLibrary = this.Genres.FirstOrDefault(p => string.Equals(p.Name, genre.Item2, StringComparison.OrdinalIgnoreCase));
                if (genreInLibrary == null)
                {
                    genreInLibrary = new Genre(genre.Item2);
                    await databaseConnection.InsertAsync(genreInLibrary);
                    this.Genres.Add(genreInLibrary);
                }
                await databaseConnection.InsertAsync(new SongGenre(genre.Item1.Id, genreInLibrary.Id));
            }
            songsToAdd.Clear();
            songGenresToAdd.Clear();
        }

        private async Task AddOrUpdateSong(Song song, IEnumerable<string> genres)
        {
            songsToAdd.Add(song);
            foreach (var genre in genres)
                songGenresToAdd.Add(new Tuple<Song, string>(song, genre));

            if (songsToAdd.Count > 25)
                await AddAllRemainingSongs();
        }

        private async Task RemoveSong(string fileName)
        {
            var song = this.Songs.FirstOrDefault(p => p.FileName == fileName);

            if (song != null)
            {
                this.Songs.Remove(song);
                await databaseConnection.DeleteAsync(song);
                await databaseConnection.ExecuteAsync("delete from SongGenre where SongId = ?", song.Id);
            }
        }

        public async Task UpdateLibrary()
        {
            DateTime lastUpdate = DateTime.Now;


            Stopwatch s = new Stopwatch();
            s.Start();

            if (necessaryUpdatesForLibrary != null)
                foreach (var update in necessaryUpdatesForLibrary)
                    await update(this);

            s.Stop();
            Debug.WriteLine("Other Update Time: " + s.ElapsedMilliseconds);
            s.Reset();
            s.Start();

            await localLibrarySource.Update(AddOrUpdateSong, RemoveSong);
            await AddAllRemainingSongs();

            s.Stop();
            Debug.WriteLine("Folder Update Time: " + s.ElapsedMilliseconds);
            s.Reset();
            s.Start();
            await CleanupAlbumCovers();
            s.Stop();
            Debug.WriteLine("Album Cover Cleanup Time: " + s.ElapsedMilliseconds);

            ApplicationSettings.IsDatabaseSettingUp.Save(false);
        }

        public async Task<StorageFile> GetAlbumCoverFileNameForSong(string album, string artist)
        {
            CachedAlbumCover result = null;

            await databaseConnection.TryRunInTransactionAsync(connection =>
            {
                result = connection.Table<CachedAlbumCover>().Where(p => p.Artist == artist && p.Album == album).FirstOrDefault();
                if (result == null)
                {
                    result = new CachedAlbumCover(album, artist);
                    connection.Insert(result);
                }
            });

            if (result.ImageFileName == null)
            {
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(Guid.NewGuid().ToString());
                result.ImageFileName = file.Path;
                await databaseConnection.UpdateAsync(result);

                return file;
            }

            return await StorageFile.GetFileFromPathAsync(result.ImageFileName);

        }

        public async Task<string> GetAlbumCoverForSong(string album, string artist)
        {
            CachedAlbumCover result = null;

            if (!await databaseConnection.TryRunInTransactionAsync(connection =>
            {
                result = connection.Table<CachedAlbumCover>().Where(p => p.Artist == artist && p.Album == album).FirstOrDefault();
                if (result == null)
                {
                    result = new CachedAlbumCover(album, artist);
                    connection.Insert(result);
                }
            }))
                return null;

            if (result.ImageFileName == null)
            {
                var song = this.Songs.FirstOrDefault(p => p.Album == album && p.AlbumArtist == artist);
                if (song != null)
                {
                    await result.Load(song.FileName);
                    if (!await databaseConnection.TryRunInTransactionAsync(connection =>
                        connection.Update(result)))
                        return null;
                }
            }

            return result.ImageFileName;
        }

        #region Update Algorithms

        public async Task CleanupAlbumCovers()
        {
            var albumCoversToDelete = await databaseConnection.QueryAsync<CachedAlbumCover>("select * from CachedAlbumCover where not Id in (select distinct a.Id from Song s, CachedAlbumCover a where LOWER(s.Album) = LOWER(a.Album) and LOWER(s.AlbumArtist = a.Artist))");

            foreach (var coverToDelete in albumCoversToDelete)
            {
                try
                {
                    if (coverToDelete.ImageFileName != null && coverToDelete.ImageFileName != "")
                        await (await StorageFile.GetFileFromPathAsync(coverToDelete.ImageFileName)).DeleteAsync();
                }
                catch (Exception e) { Debug.WriteLine("Deleting Album Cover Failed: " + e.Message); }

                await databaseConnection.DeleteAsync(coverToDelete);
            }
        }
        #endregion

        public async Task<IEnumerable<Song>> GetSongsForGenre(Genre genre)
        {
            return await databaseConnection.QueryAsync<Song>("select song.* from Song song, SongGenre songGenre where song.Id = songGenre.SongId and songGenre.GenreId = ?", genre.Id);
        }
    }
}
