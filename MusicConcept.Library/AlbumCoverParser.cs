using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.Diagnostics;

namespace MusicConcept.Library
{
    static class AlbumCoverParser
    {
        public static async Task<bool> ExtractAlbumPicture(string filename, string outFilename)
        {
            TagLib.Tag tag = null;

            using (var stream = await (await StorageFile.GetFileFromPathAsync(filename)).OpenStreamForReadAsync())
            {
                tag = await Task<TagLib.File>.Run(() =>
                {
                    try
                    {
                        return TagLib.File.Create(new TagLib.StreamFileAbstraction(filename, stream, null)).Tag;
                    }
                    catch { return null; }
                });
            }

            if (tag == null)
                return false;

            var picture = tag.Pictures.FirstOrDefault(p => p.Type == TagLib.PictureType.FrontCover) ?? tag.Pictures.FirstOrDefault();

            if (picture == null)
                return false;

            var folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(outFilename));
            StorageFile outFile = await folder.CreateFileAsync(Path.GetFileName(outFilename));
            await FileIO.WriteBytesAsync(outFile, picture.Data.ToArray());

            return true;
        }
    }
}
