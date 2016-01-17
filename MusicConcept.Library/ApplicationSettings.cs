using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.Reflection;

namespace MusicConcept.Library
{
    public static class ApplicationSettings
    {
        public static readonly ApplicationSetting<string> PlaylistManagerRepeatMode = new ApplicationSetting<string>("PlaylistManagerRepeatMode", "None");
        public static readonly ApplicationSetting<bool> PlaylistManagerIsRandomOrder = new ApplicationSetting<bool>("PlaylistManagerIsRandomOrder", false);
        public static readonly ApplicationSetting<int> PlaylistManagerCurrentIndex = new ApplicationSetting<int>("PlaylistManagerCurrentIndex", 0);

        public const int BackgroundTaskResumeSongTimeNone = -1;
        public static readonly ApplicationSetting<double> BackgroundTaskResumeSongTime = new ApplicationSetting<double>("BackgroundTaskResumeSongTime", BackgroundTaskResumeSongTimeNone);
        public static readonly ApplicationSetting<double> CurrentSongLength = new ApplicationSetting<double>("CurrentSongLength", 0);
        public static readonly ApplicationSetting<string> CurrentSongFileName = new ApplicationSetting<string>("CurrentSongFileName", null);

        public static readonly ApplicationSetting<string> CurrentlyEnabledMusicFilters = new ApplicationSetting<string>("CurrentlyEnabledMusicFilters", "AllSongsFilter, AlbumsFilter, ArtistsFilter, SearchFilter");

        public static readonly ApplicationSetting<bool> IsDatabaseSettingUp = new ApplicationSetting<bool>("IsDatabaseSettingUp", true);
        public static readonly ApplicationSetting<string> DatabaseCreationVersion = new ApplicationSetting<string>("DatabaseCreationVersion", "-1");

        public static readonly ApplicationSetting<string> AppLastStartupVersion = new ApplicationSetting<string>("AppLastStartupVersion", "");
        public static readonly ApplicationSetting<int> ReviewReminderStartupNumber = new ApplicationSetting<int>("ReviewReminderStartupNumber", 0);

        public static readonly ApplicationSetting<string> SortingAlphabet = new ApplicationSetting<string>("SortingAlphabet", 
            SortingHelper.SymbolChar + SortingHelper.LatinAlphabet);

        public sealed class ApplicationSetting<T>
        {
            private string key;
            private T defaultValue;

            internal ApplicationSetting(string key, T defaultValue)
            {
                this.key = key;
                this.defaultValue = defaultValue;
            }

            /// <summary>
            /// Function to read a setting value and clear it after reading it.
            /// </summary>
            public T ReadReset()
            {
                lock (this)
                {
                    Debug.WriteLine(key);
                    if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
                        return defaultValue;
                    else
                    {
                        var value = ApplicationData.Current.LocalSettings.Values[key];
                        ApplicationData.Current.LocalSettings.Values.Remove(key);
                        return (T)value;
                    }
                }
            }

            /// <summary>
            /// Function to read a setting value.
            /// </summary>
            public T Read()
            {
                lock (this)
                {
                    if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
                        return defaultValue;
                    else
                        return (T)ApplicationData.Current.LocalSettings.Values[key];
                }
            }

            /// <summary>
            /// Save a key value pair in settings. Create if it doesn't exist.
            /// </summary>
            public void Save(T value)
            {
                lock (this)
                {
                    if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
                        ApplicationData.Current.LocalSettings.Values.Add(key, value);
                    else
                        ApplicationData.Current.LocalSettings.Values[key] = value;
                }
            }
        }
    }
}
