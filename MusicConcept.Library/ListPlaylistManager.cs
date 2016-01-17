using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicConcept.Library
{
    public sealed class ListPlaylistManager : NotifyPropertyChangedObject
    {
        private SQLiteAsyncConnection databaseConnection;

        private int CurrentIndex
        {
            get { return ApplicationSettings.PlaylistManagerCurrentIndex.Read(); }
            set { ApplicationSettings.PlaylistManagerCurrentIndex.Save(value); }
        }

        public SongRepeatMode RepeatMode
        {
            get { return (SongRepeatMode)Enum.Parse(typeof(SongRepeatMode), ApplicationSettings.PlaylistManagerRepeatMode.Read()); }
            set { ApplicationSettings.PlaylistManagerRepeatMode.Save(value.ToString()); NotifyPropertyChanged("RepeatMode"); }
        }

        public bool IsRandomOrder
        {
            get { return ApplicationSettings.PlaylistManagerIsRandomOrder.Read(); }
            private set
            {
                ApplicationSettings.PlaylistManagerIsRandomOrder.Save(value); NotifyPropertyChanged("IsRandomOrder");
            }
        }

        public async Task SetIsRandomOrder(bool value, int? currentSongId)
        {
            if (IsRandomOrder != value)
            {
                this.IsRandomOrder = value;
                if (currentSongId != null)
                {
                    await this.databaseConnection.TryRunInTransactionAsync(connection =>
                    {
                        if (value)
                            ShufflePlaylist(connection, currentSongId.Value);
                        else
                            UnshufflePlaylist(connection, currentSongId.Value);
                    });
                }
            }
        }

        public ListPlaylistManager(SQLiteAsyncConnection databaseConnection)
        {
            this.databaseConnection = databaseConnection;
        }

        private void ShufflePlaylist(SQLiteConnection connection, int currentSong)
        {
            connection.Execute("create temp table TempPlaylistOrder as select Rank as ShuffledRank from CurrentPlaylistSong order by case when SongId = ? then -1 else abs(random()) end", currentSong);
            connection.Execute("update CurrentPlaylistSong set Rank = (select rowid from TempPlaylistOrder where ShuffledRank = Rank) - 1");
            connection.Execute("drop table TempPlaylistOrder");

            this.CurrentIndex = 0;
        }

        private void UnshufflePlaylist(SQLiteConnection connection, int currentSong)
        {
            connection.Execute("update CurrentPlaylistSong set Rank = ActualRank");

            var song = connection.Query<CurrentPlaylistSong>("Select Rank from CurrentPlaylistSong where SongId = ?", currentSong).FirstOrDefault();
            this.CurrentIndex = song == null ? 0 : song.Rank;
        }

        public Task<bool> PlayFromSource(SongSource songSource, int firstSongId)
        {
            return this.databaseConnection.TryRunInTransactionAsync(connection =>
            {
                this.CurrentIndex = 0;

                connection.Execute("delete from CurrentPlaylistSong");

                AddToList(connection, 0, songSource);

                if (IsRandomOrder)
                    ShufflePlaylist(connection, firstSongId);
                else
                    this.CurrentIndex = connection.Table<CurrentPlaylistSong>().First(p => p.SongId == firstSongId).Rank;

                Debug.WriteLine("CurrentIndex: " + this.CurrentIndex);
            });
        }

        private void AddToList(SQLiteConnection connection, int index, SongSource songSource)
        {
            connection.Execute("create temp table NewPlaylistSongs as select Id as SongId " + songSource.Sql, songSource.SqlArguments);

            var addCount = connection.Query<Song>("select count(*) as Id from NewPlaylistSongs").First().Id;

            if (index <= this.CurrentIndex)
                this.CurrentIndex += addCount;

            connection.Execute("update CurrentPlaylistSong set Rank = Rank + ?, ActualRank = Rank + ? where Rank >= ?", addCount, addCount, index);

            connection.Execute("insert into CurrentPlaylistSong select NULL as Id, SongId as SongId, rowid + ? as Rank, rowid + ? as ActualRank from NewPlaylistSongs", index - 1, index - 1);
            connection.Execute("drop table NewPlaylistSongs");
        }

        public Task<bool> AddNext(SongSource songSource)
        {
            return this.databaseConnection.TryRunInTransactionAsync(connection =>
            {
                CheckRandomOrderDisabled(connection);

                AddToList(connection, this.CurrentIndex + 1, songSource);
            });
        }

        public Task<bool> AddToEnd(SongSource songSource)
        {
            return this.databaseConnection.TryRunInTransactionAsync(connection =>
            {
                CheckRandomOrderDisabled(connection);

                int count = connection.Table<CurrentPlaylistSong>().Count();
                AddToList(connection, count + 1, songSource);
            });
        }

        private void CheckRandomOrderDisabled(SQLiteConnection connection)
        {
            if (this.IsRandomOrder)
                connection.Execute("update CurrentPlaylistSong set ActualRank = Rank");
            this.IsRandomOrder = false;
        }

        public async Task<string> GetNextFileToPlay(bool forceNext)
        {
            if (forceNext || RepeatMode != SongRepeatMode.One)
                ++this.CurrentIndex;

            var currentSong = await GetCurrentFileToPlay();

            if (currentSong != null)
                return currentSong;
            else
            {
                this.CurrentIndex = 0;

                if (RepeatMode == SongRepeatMode.All)
                    return await GetCurrentFileToPlay();
                else
                    return null;
            }
        }

        public async Task<string> GetCurrentFileToPlay()
        {
            var currentFileToPlay = (await databaseConnection.QueryAsync<Song>("select s.FileName from Song s, CurrentPlaylistSong c where c.Rank = ? and s.Id = c.SongId limit 1", this.CurrentIndex)).FirstOrDefault();

            if (currentFileToPlay == null)
                return null;
            else
                return currentFileToPlay.FileName;
        }

        public async Task<string> GetPreviousFileToPlay()
        {
            if (this.CurrentIndex > 0)
            {
                --this.CurrentIndex;
                return await GetCurrentFileToPlay();
            }
            else
                return null;
        }

        public async Task<IEnumerable<Song>> PredictNextSongs(IEnumerable<Song> alreadyPredicted, Song current, int count)
        {
            return (await databaseConnection.QueryAsync<Song>("select s.* from Song s, CurrentPlaylistSong c where s.Id = c.SongId order by c.rank limit ? offset ?", count, alreadyPredicted.Count() + 1 + this.CurrentIndex));
        }

        public Task Remove(int index)
        {
            return this.databaseConnection.TryRunInTransactionAsync(connection =>
            {
                CheckRandomOrderDisabled(connection);

                if (index < this.CurrentIndex)
                    this.CurrentIndex -= 1;

                connection.Execute("delete from CurrentPlaylistSong where Rank = ?", index);
                connection.Execute("update CurrentPlaylistSong set Rank = Rank - 1, ActualRank = Rank - 1 where Rank > ?", index);
            });
        }

        public Task Clear()
        {
            return this.databaseConnection.TryRunInTransactionAsync(connection =>
            {
                connection.Execute("delete from CurrentPlaylistSong where not Rank = ?", this.CurrentIndex);
                connection.Execute("update CurrentPlaylistSong set Rank = 0, ActualRank = 0");
                this.CurrentIndex = 0;
            });
        }

        public Task AddSong(int index, int songId, bool addAsCurrentSong)
        {
            return this.databaseConnection.TryRunInTransactionAsync(connection =>
            {
                CheckRandomOrderDisabled(connection);

                if (addAsCurrentSong)
                    this.CurrentIndex = index;
                else if (index <= this.CurrentIndex)
                    this.CurrentIndex += 1;
                connection.Execute("update CurrentPlaylistSong set Rank = Rank + 1, ActualRank = Rank + 1 where Rank >= ?", index);
                connection.Insert(new CurrentPlaylistSong(songId, index, index));
            });
        }

        public async Task SaveToLibrary(MusicLibrary musicLibrary, string playlistName)
        {
            SavedPlaylist savedPlaylist = null;
            await this.databaseConnection.TryRunInTransactionAsync(connection =>
            {
                savedPlaylist = new SavedPlaylist() { Name = playlistName };

                var mostCommonArtistNames = connection.Query<Song>("select s.Artist from Song s, CurrentPlaylistSong c where s.Id = c.SongId group by s.Artist order by count(s.Artist) desc limit 4").Select(p => p.Artist);
                if (mostCommonArtistNames.Any())
                {
                    savedPlaylist.ShortDescription = string.Join(", ", mostCommonArtistNames.Take(3));
                    if (mostCommonArtistNames.Count() > 3)
                        savedPlaylist.ShortDescription += ", ...";
                }

                connection.Insert(savedPlaylist);

                connection.Execute("delete from SavedPlaylistSong where PlaylistId = ?", savedPlaylist.Id);
                connection.Execute("insert into SavedPlaylistSong (SongId, Rank, PlaylistId) select SongId, Rank, ? from CurrentPlaylistSong", savedPlaylist.Id);
            });
            musicLibrary.SavedPlaylists.Add(savedPlaylist);
        }

        public async Task DeleteFromLibrary(MusicLibrary musicLibrary, SavedPlaylist savedPlaylist)
        {
            await databaseConnection.DeleteAsync(savedPlaylist);
            musicLibrary.SavedPlaylists.Remove(savedPlaylist);
        }

        public Task<bool> LoadFromLibrary(SavedPlaylist savedPlaylist)
        {
            return this.databaseConnection.TryRunInTransactionAsync(connection =>
            {
                connection.Execute("delete from CurrentPlaylistSong");
                connection.Execute("insert into CurrentPlaylistSong (SongId, Rank, ActualRank) select SongId, Rank, Rank from SavedPlaylistSong where PlaylistId = ?", savedPlaylist.Id);
                this.IsRandomOrder = false;
                this.CurrentIndex = 0;
            });
        }

        public async Task<IEnumerable<Song>> GetCompletePlaylist()
        {
            return await databaseConnection.QueryAsync<Song>("select s.* from Song s, CurrentPlaylistSong c where s.Id = c.SongId order by c.rank");
        }

        public int GetCurrentSongIndex()
        {
            return this.CurrentIndex;
        }

        public async Task<string> GetCurrentAlbumCover()
        {
            var albumCover = (await databaseConnection.QueryAsync<CachedAlbumCover>("select a.ImageFileName from CachedAlbumCover a, Song s, CurrentPlaylistSong c where c.Rank = ? and s.Id = c.SongId and a.Album = s.Album and a.Artist = s.Artist limit 1", this.CurrentIndex)).FirstOrDefault();

            return albumCover == null ? null : albumCover.ImageFileName;
        }
    }
}
