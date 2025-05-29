// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;
using XRC.Students.Sp2025.P36.Yan;
using PassthroughCameraSamples;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Manages image composition for mixed reality applications.
    /// Combines WebCam and VR camera textures for visual processing.
    /// </summary>
    public class MRImageViewerManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Reference to the WebCam texture provider")]
        private WebCamTextureManager webCamTextureManager;

        [SerializeField, Tooltip("Reference to the VR camera capture component")]
        private VRCameraCaptureTexture vrCameraCaptureTexture;

        /// <summary>
        /// Gets pure WebCam texture without any virtual overlays.
        /// Returns null in editor mode or if WebCam is unavailable.
        /// </summary>
        /// <returns>A new Texture2D with the WebCam image, or null if unavailable.</returns>
        public Texture2D GetWebCamTexture2D()
        {
#if UNITY_EDITOR
            return null; // No WebCam in editor
#else
            WebCamTexture webCamTexture = webCamTextureManager.WebCamTexture;
            if (webCamTexture == null) return null;

            Texture2D webCamTex2D = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            webCamTex2D.SetPixels32(webCamTexture.GetPixels32());
            webCamTex2D.Apply();
            return webCamTex2D;
#endif
        }

        /// <summary>
        /// Gets pure VR camera texture without any real-world background.
        /// </summary>
        /// <returns>A new Texture2D with the VR camera image, or null if unavailable.</returns>
        public Texture2D GetVRTexture2D()
        {
            Texture2D vrTex = vrCameraCaptureTexture.GetCaptureTexture();
            if (vrTex == null) return null;

            Texture2D vrTexCopy = new Texture2D(vrTex.width, vrTex.height, TextureFormat.RGBA32, false);
            vrTexCopy.SetPixels(vrTex.GetPixels());
            vrTexCopy.Apply();
            return vrTexCopy;
        }

        /// <summary>
        /// Gets a combined texture from VR and webcam sources.
        /// In editor mode, returns only VR texture.
        /// In runtime, blends VR and webcam textures.
        /// </summary>
        /// <returns>A new Texture2D with the combined result, or null if source textures are unavailable.</returns>
        public Texture2D GetCombinedTexture2D()
        {
#if UNITY_EDITOR
            return GetVRTexture2D();
#else
            WebCamTexture webCamTexture = webCamTextureManager.WebCamTexture;
            Texture2D vrTex = vrCameraCaptureTexture.GetCaptureTexture();

            if (webCamTexture == null || vrTex == null) return null;

            int width = Mathf.Min(webCamTexture.width, vrTex.width);
            int height = Mathf.Min(webCamTexture.height, vrTex.height);

            Texture2D webCamTex2D = new Texture2D(width, height, TextureFormat.RGBA32, false);
            webCamTex2D.SetPixels32(webCamTexture.GetPixels32());
            webCamTex2D.Apply();

            Color[] basePixels = webCamTex2D.GetPixels(0, 0, width, height);
            Color[] overlayPixels = vrTex.GetPixels(0, 0, width, height);
            Color[] outPixels = new Color[basePixels.Length];

            for (int i = 0; i < outPixels.Length; i++)
            {
                Color bg = basePixels[i];
                Color fg = overlayPixels[i];
                float a = fg.a;
                outPixels[i].r = fg.r * a + bg.r * (1 - a);
                outPixels[i].g = fg.g * a + bg.g * (1 - a);
                outPixels[i].b = fg.b * a + bg.b * (1 - a);
                outPixels[i].a = a + (1 - a) * bg.a;
            }

            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.SetPixels(outPixels);
            result.Apply();
            return result;
#endif
        }
    }
}

