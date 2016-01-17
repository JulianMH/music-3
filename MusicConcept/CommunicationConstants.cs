using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicConcept
{
    static class CommunicationConstants
    {
        //Both Directions
        public const string PlaybackNewSong = "PlaybackNewSong";
    /*    public const string NewPlayback = "NewPlayback";
        public const string NextSong = "NextSong";
        public const string PreviousSong = "PreviousSong";*/

        //Background to Foreground
        public const string BackgroundTaskStarted = "BackgroundTaskStarted";
        public const string BackgroundTaskStopped = "BackgroundTaskStopped";

        //Foreground to Background
        public const string TogglePlayPause = "TogglePlayPause";
        public const string GetInfo = "GetInfo";
    }
}
