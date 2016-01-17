using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;

namespace MusicConcept.Library.LibrarySource
{
    public class LocalLibrarySource
    {
        private class Folder
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public string Path { get; set; }
            public DateTime LastUpdateTime { get; set; }
            public bool CheckedToday { get; set; }
        }

        private class File
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public int FolderId { get; set; }
            public string Path { get; set; }
            public DateTime LastUpdateTime { get; set; }
        }

        public const String DatabaseFileName = "LocalLibrarySource";
        private StorageFolder musicLibraryFolder;
        private SQLiteAsyncConnection databaseConnection;

        private Func<Song, IEnumerable<string>, Task> addSongWithGenres;
        private Func<string, Task> removeSong;

        public LocalLibrarySource(StorageFolder musicLibrary)
        {
            this.databaseConnection = new SQLiteAsyncConnection(DatabaseFileName, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.NoMutex);

            this.musicLibraryFolder = musicLibrary;
        }

        public LocalLibrarySource()
            : this(KnownFolders.MusicLibrary)
        {

        }

        public async Task Update(Func<Song, IEnumerable<string>, Task> addSongWithGenres, Func<string, Task> removeSong)
        {
            this.addSongWithGenres = addSongWithGenres;
            this.removeSong = removeSong;

            await databaseConnection.CreateTableAsync<File>();
            await databaseConnection.CreateTableAsync<Folder>();

            await databaseConnection.ExecuteAsync("update Folder set CheckedToday = '0'");

            await UpdateFolder(musicLibraryFolder);

            var songsToRemove = await databaseConnection.QueryAsync<File>("select file.* from File file, Folder folder where folder.Id = file.FolderId and not folder.CheckedToday");

            foreach (var song in songsToRemove)
            {
                await removeSong(song.Path);
                await databaseConnection.DeleteAsync(song);
            }

            await databaseConnection.ExecuteAsync("delete from Folder where not CheckedToday");
        }

        async Task UpdateFolder(StorageFolder storageFolder)
        {
            var folder = await databaseConnection.Table<Folder>().Where(p => p.Path == storageFolder.Path).FirstOrDefaultAsync();

            if (folder == null)
            {
                folder = new Folder() { LastUpdateTime = DateTime.Now, CheckedToday = true, Path = storageFolder.Path };
                await databaseConnection.InsertAsync(folder);

                await UpdateFilesInFolder(folder.Id, storageFolder);
            }
            else
            {
                var dateChanged = (await storageFolder.GetBasicPropertiesAsync()).DateModified;
                if (dateChanged > folder.LastUpdateTime)
                {
                    await UpdateFilesInFolder(folder.Id, storageFolder);
                    folder.LastUpdateTime = DateTime.Now;
                }

                folder.CheckedToday = true;
                await databaseConnection.UpdateAsync(folder);
            }

            foreach (var subFolder in await storageFolder.GetFoldersAsync())
                await UpdateFolder(subFolder);
        }

        private async Task AddSong(StorageFile storageFile)
        {
            if (storageFile.Name == "tmp")
                return;

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

            if (tag != null && tag.Title != null && tag.Title.Trim() != "" && !tag.Genres.Contains("Podcast"))
            {
                var song = new Song() { FileName = storageFile.Path };
                UpdateSongMetadata(tag, song);
                await addSongWithGenres(song, tag.Genres);
            }
        }

        public void UpdateSongMetadata(TagLib.Tag tag, Song song)
        {
            song.SetAllData(tag.Title, tag.Album ?? "", tag.FirstAlbumArtist ?? "", tag.Track, tag.Disc, tag.FirstPerformer ?? "", song.FileName);
        }

        private async Task RemoveSong(string path)
        {
            await removeSong(path);
        }

        private async Task UpdateFilesInFolder(int folderId, StorageFolder storageFolder)
        {
            var files = await databaseConnection.Table<File>().Where(p => p.FolderId == folderId).ToListAsync();

            foreach (var storageFile in await storageFolder.GetFilesAsync())
            {
                var file = files.Where(p => p.Path == storageFile.Path).FirstOrDefault();

                if (file == null)
                {
                    await databaseConnection.InsertAsync(new File() { LastUpdateTime = DateTime.Now, FolderId = folderId, Path = storageFile.Path });

                    await AddSong(storageFile);
                }
                else
                {
                    files.Remove(file);

                    var dateChanged = (await storageFolder.GetBasicPropertiesAsync()).DateModified;
                    if (dateChanged > file.LastUpdateTime)
                    {
                        await RemoveSong(storageFile.Path);
                        await AddSong(storageFile);

                        file.LastUpdateTime = DateTime.Now;
                        await databaseConnection.UpdateAsync(file);
                    }

                }
            }
        }


    }
}
