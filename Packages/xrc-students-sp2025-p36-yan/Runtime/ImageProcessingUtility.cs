using UnityEngine;
using System;
using System.IO;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Image processing utility functions
    /// </summary>
    public static class ImageProcessingUtility
    {
        /// <summary>
        /// Crop texture based on specified rectangle
        /// </summary>
        public static Texture2D CropTexture(Texture2D sourceTexture, Rect cropRect)
        {
            int x = Mathf.FloorToInt(cropRect.x);
            int y = Mathf.FloorToInt(cropRect.y);
            int width = Mathf.FloorToInt(cropRect.width);
            int height = Mathf.FloorToInt(cropRect.height);

            x = Mathf.Clamp(x, 0, sourceTexture.width - 1);
            y = Mathf.Clamp(y, 0, sourceTexture.height - 1);
            width = Mathf.Clamp(width, 1, sourceTexture.width - x);
            height = Mathf.Clamp(height, 1, sourceTexture.height - y);

            if (width <= 0 || height <= 0)
            {
                return sourceTexture;
            }

            try
            {
                Texture2D result = new Texture2D(width, height, sourceTexture.format, false);
                Color[] pixels = sourceTexture.GetPixels(x, y, width, height);
                result.SetPixels(pixels);
                result.Apply();
                return result;
            }
            catch (Exception)
            {
                return sourceTexture;
            }
        }

        /// <summary>
        /// Draw a line on the texture using Bresenham's algorithm
        /// </summary>
        public static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color, int thickness = 1)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = (x0 < x1) ? 1 : -1;
            int sy = (y0 < y1) ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                // Draw thick points by offsetting around the central point
                for (int offX = -thickness / 2; offX <= thickness / 2; offX++)
                {
                    for (int offY = -thickness / 2; offY <= thickness / 2; offY++)
                    {
                        int x = x0 + offX;
                        int y = y0 + offY;

                        if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                        {
                            texture.SetPixel(x, y, color);
                        }
                    }
                }

                // Check if we have reached the end point
                if (x0 == x1 && y0 == y1)
                    break;

                // Next point
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        /// <summary>
        /// Save texture to file with timestamp and prefix
        /// </summary>
        public static void SaveTextureToFile(Texture2D texture, string prefix)
        {
            try
            {
                string directoryPath;

#if UNITY_EDITOR
                directoryPath = Application.dataPath + "/AskFromStrokeImages";
#else
                directoryPath = Path.Combine(Application.persistentDataPath, "AskFromStrokeImages");
#endif

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string guid = Guid.NewGuid().ToString().Substring(0, 8);
                string filename = $"{prefix}_{timestamp}_{guid}.png";
                string filePath = Path.Combine(directoryPath, filename);

                byte[] bytes = texture.EncodeToPNG();
                File.WriteAllBytes(filePath, bytes);

                string fullPath = Path.GetFullPath(filePath);
                Debug.Log($"Debug image saved to: {fullPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving debug image: {e.Message}");
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Save texture to a specific directory with given prefix
        /// </summary>
        public static void SaveTextureToFile(Texture2D texture, string prefix, string directoryName)
        {
            try
            {
                string directoryPath;

#if UNITY_EDITOR
                directoryPath = Application.dataPath + "/" + directoryName;
#else
                directoryPath = Path.Combine(Application.persistentDataPath, directoryName);
#endif

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string guid = Guid.NewGuid().ToString().Substring(0, 8);
                string filename = $"{prefix}_{timestamp}_{guid}.png";
                string filePath = Path.Combine(directoryPath, filename);

                byte[] bytes = texture.EncodeToPNG();
                File.WriteAllBytes(filePath, bytes);

                string fullPath = Path.GetFullPath(filePath);
                Debug.Log($"{prefix} image saved to: {fullPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving {prefix} image: {e.Message}");
                Debug.LogException(e);
            }
        }
    }
}