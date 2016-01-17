using MusicConcept.Library;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Web.Http;
using System.IO;
using System.Windows.Input;
using MusicConcept.Common;
using Windows.Storage;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.ApplicationModel.Resources;

namespace MusicConcept.ViewModels
{
    class AlbumCoverFetchViewModel : NotifyPropertyChangedObject
    {
        public string AlbumArtistAndName { get; private set; }

        public ObservableCollection<WriteableBitmap> Images { get; private set; }

        public bool IsLoading { get; private set; }
        public bool IsEmpty { get; private set; }

        private Album album;

        public AlbumCoverFetchViewModel(Album album)
        {
            this.AlbumArtistAndName = album.ArtistAndAlbum;

            this.Images = new ObservableCollection<WriteableBitmap>();
            this.album = album;

            LoadImageUrls(album);
        }

        private void LoadImageUrls(Album album)
        {
            this.IsEmpty = false; NotifyPropertyChanged("IsEmpty");
            this.IsLoading = true; NotifyPropertyChanged("IsLoading");

            //var client = new HttpClient();

            //var urls = await XboxMusicAlbumCover.TryGetAlbumUrl(client, album.Name, album.Artist);
            //if (urls == null)
            //    return;

            //foreach (var url in urls)
            //{

            //    var image = new WriteableBitmap(480, 480);

            //    using (var inStream = (await client.GetInputStreamAsync(new Uri(url))).AsStreamForRead())
            //    using (var memoryStream = new MemoryStream())
            //    {
            //        await inStream.CopyToAsync(memoryStream);
            //        memoryStream.Seek(0, SeekOrigin.Begin);
            //        await image.SetSourceAsync(memoryStream.AsRandomAccessStream());
            //    }

            //    this.Images.Add(image);
            //}

            this.IsLoading = false; NotifyPropertyChanged("IsLoading");
            this.IsEmpty = !this.Images.Any(); NotifyPropertyChanged("IsEmpty");
        }

        internal async void SaveCover(object cover)
        {
            var writeableBitmap = cover as WriteableBitmap;

            var storageFile = await album.GetCoverFile();

            try
            {
                byte[] pixels = writeableBitmap.PixelBuffer.ToArray();

                using (var writeStream = await storageFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, writeStream);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Straight,
                        (uint)writeableBitmap.PixelWidth,
                        (uint)writeableBitmap.PixelHeight,
                        96,
                        96,
                        pixels);
                    await encoder.FlushAsync();

                    using (var outputStream = writeStream.GetOutputStreamAt(0))
                    {
                        await outputStream.FlushAsync();
                    }
                }
            }
            catch
            {
                Action showErrorMessage = async () =>
                {
                    await new MessageDialog(ResourceLoader.GetForViewIndependentUse().GetString("AlbumCoverFetchViewModelSaveCropError")).ShowAsync();
                };

                if (System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debugger.Break();
                else
                    showErrorMessage();
            }

            album.LoadCover();
        }

        async public void SaveAndCropCover(StorageFile originalImgFile)
        {


            using (IRandomAccessStream stream = await originalImgFile.OpenReadAsync())
            {


                // Create a decoder from the stream. With the decoder, we can get  
                // the properties of the image. 
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);




                var shortSide = Math.Min(decoder.PixelWidth, decoder.PixelHeight);
                double scale = Math.Min(1, shortSide / 480);
                Size corpSize = new Size(shortSide, shortSide);
                Point startPoint = new Point((decoder.PixelWidth - shortSide) / 2, (decoder.PixelHeight - shortSide) / 2);


                // Source: https://code.msdn.microsoft.com/windowsapps/CSWin8AppCropBitmap-52fa1ad7

                if (double.IsNaN(scale) || double.IsInfinity(scale))
                {
                    scale = 1;
                }


                // Convert start point and size to integer. 
                uint startPointX = (uint)Math.Floor(startPoint.X * scale);
                uint startPointY = (uint)Math.Floor(startPoint.Y * scale);
                uint height = (uint)Math.Floor(corpSize.Height * scale);
                uint width = (uint)Math.Floor(corpSize.Width * scale);





                // The scaledSize of original image. 
                uint scaledWidth = (uint)Math.Floor(decoder.PixelWidth * scale);
                uint scaledHeight = (uint)Math.Floor(decoder.PixelHeight * scale);



                // Refine the start point and the size.  
                if (startPointX + width > scaledWidth)
                {
                    startPointX = scaledWidth - width;
                }


                if (startPointY + height > scaledHeight)
                {
                    startPointY = scaledHeight - height;
                }


                // Create cropping BitmapTransform and define the bounds. 
                BitmapTransform transform = new BitmapTransform();
                BitmapBounds bounds = new BitmapBounds();
                bounds.X = startPointX;
                bounds.Y = startPointY;
                bounds.Height = height;
                bounds.Width = width;
                transform.Bounds = bounds;


                transform.ScaledWidth = scaledWidth;
                transform.ScaledHeight = scaledHeight;

                // Get the cropped pixels within the bounds of transform. 
                PixelDataProvider pix = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    transform,
                    ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.ColorManageToSRgb);
                byte[] pixels = pix.DetachPixelData();


                // Stream the bytes into a WriteableBitmap 
                WriteableBitmap cropBmp = new WriteableBitmap((int)width, (int)height);
                Stream pixStream = cropBmp.PixelBuffer.AsStream();
                pixStream.Write(pixels, 0, (int)(width * height * 4));

                this.SaveCover(cropBmp);
            }
        }
    }
}
