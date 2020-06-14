using System;
using System.IO;
using System.Threading.Tasks;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using SixLabors.ImageSharp.PixelFormats;

namespace DeluMc.Utils
{
    /// <summary>
    /// Helper Class to Save Maps as Pngs
    /// </summary>
    public static class Mapper
    {
        /// <summary>
        /// Map Folder Name
        /// </summary>
        private const string kMapFolderName = "DeluMC";

        /// <summary>
        /// Min Image Side Size
        /// </summary>
        private const int kMinImageSize = 1024;

        /// <summary>
        /// Map Folder Path
        /// </summary>
        private static readonly string kMapFolderPath
            = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), kMapFolderName);

        /// <summary>
        /// Set Color for block (z, x)
        /// </summary>
        /// <param name="z">Z Position</param>
        /// <param name="x">X Position</param>
        public delegate Color ColorBlock(int z, int x);

        /// <summary>
        /// Apply a Color to a Bitmap
        /// </summary>
        /// <param name="z">Z Coordinate</param>
        /// <param name="x">X Coordinate</param>
        /// <param name="color">Color to Aply</param>
        public delegate void ColorApplier(int z, int x, in Color color);

        /// <summary>
        /// Do Special Coloring on a bitmap
        /// </summary>
        /// <param name="colorApplier">Function to apply colors</param>
        public delegate void SpecialColors(ColorApplier colorApplier);

        /// <summary>
        /// Resize the image to the specified width and height.
        /// From https://stackoverflow.com/a/24199315
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Image<Rgba32> ResizeNearestNeighborImage(Image<Rgba32> image, int width, int height)
        {

            return image.Clone(context => context
        .Resize(new ResizeOptions
        {
            Sampler = new BoxResampler(),
            Mode = ResizeMode.Max,
            Size = new Size(width, height)
        }));
        }

        /// <summary>
        /// Save bitmap to folder
        /// </summary>
        /// <param name="bitmap">Bitmap to Save</param>
        /// <param name="name">Name of Bitmap</param>
        private static void SaveBitmap(Image bitmap, in string name)
        {
            string fileName = $"{name}.png";
            string filePath = Path.Join(kMapFolderPath, fileName);

            Console.WriteLine($"Saving \"{fileName}\" in \"{filePath}\"");
            try
            {
                bitmap.Save(filePath);
            }
            catch (System.Exception e)
            {
                Console.WriteLine($"Error Saving \"{fileName}\": {e}");
            }
        }

        /// <summary>
        /// Save Map
        /// </summary>
        /// <param name="zSize">Z Axis Size</param>
        /// <param name="xSize">X Azis Size</param>
        /// <param name="name">Name of the Map</param>
        /// <param name="colorWork">Function to call to shade each pixel</param>
        /// <param name="specialColors">Function to call to do special shading</param>
        private static void SaveMap(int zSize, int xSize, in string name, ColorBlock colorWork, SpecialColors specialColors)
        {
            /// <summary>
            /// Z and X are flipped due to minecraft left handed system
            /// </summary>
            Image<Rgba32> bitmap = new Image<Rgba32>(zSize, xSize);

            if (colorWork != null)
            {
                for (int z = 0; z < zSize; z++)
                {
                    for (int x = 0; x < xSize; x++)
                    {
                        bitmap[z, xSize -1 -x] = colorWork.Invoke(z, x);
                    }
                }
            }

            if (specialColors != null)
            {
                ColorApplier colorApplier = (int z, int x, in Color color) =>
                {
                    bitmap[z, xSize - 1 - x] = color;
                };
                specialColors.Invoke(colorApplier);
            }


            int height = 0;
            int width = 0;

            if (xSize < zSize)
            {
                float ratio = (float)zSize / (float)xSize;
                height = Math.Max(kMinImageSize, xSize);
                width = (int)((float)height * ratio);
            }
            else
            {
                float ratio = (float)xSize / (float)zSize;
                width = Math.Max(kMinImageSize, zSize);
                height = (int)((float)width * ratio);
            }

            try
            {
                using (Image<Rgba32> resized = ResizeNearestNeighborImage(bitmap, width, height))
                {
                    SaveBitmap(resized, name + "_resized");
                }
            }
            catch (System.Exception e)
            {
                Console.WriteLine($"Failed to Generate Resized ({xSize}x{zSize}->{width}x{height})Image Version: {e}");
            }

            SaveBitmap(bitmap, name);
            bitmap.Dispose();
        }

        /// <summary>
        /// Info to Save a map as a Bitmap
        /// </summary>
        public struct SaveMapInfo
        {
            /// <summary>
            /// Z Axis Size
            /// </summary>
            public int zSize;

            /// <summary>
            /// X Azis Size
            /// </summary>
            public int xSize;

            /// <summary>
            /// Name of the Map
            /// </summary>
            public string name;

            /// <summary>
            /// Function to call to shade each pixel
            /// </summary>
            public ColorBlock colorWork;

            /// <summary>
            /// Function to call to do special shading
            /// </summary>
            public SpecialColors specialColors;
        }

        /// <summary>
        /// Save Maps as Pngs
        /// </summary>
        /// <param name="saveMapInfos">Info of maps to save</param>
        public static void SaveMaps(SaveMapInfo[] saveMapInfos)
        {
            Directory.CreateDirectory(kMapFolderPath);

            Parallel.For(
                0, saveMapInfos.Length,
                index =>
                {
                    SaveMap(
                        saveMapInfos[index].zSize, saveMapInfos[index].xSize, saveMapInfos[index].name,
                        saveMapInfos[index].colorWork, saveMapInfos[index].specialColors
                    );
                }
            );
        }

    }
}