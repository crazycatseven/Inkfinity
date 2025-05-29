using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace XRC.Students.Sp2025.P36.Yan
{
    /// <summary>
    /// Handles API requests to ChatGPT service with image input.
    /// </summary>
    public class ChatGPTRequest : MonoBehaviour
    {
        /// <summary>
        /// Callback for response text.
        /// </summary>
        public delegate void ResponseCallback(string responseText);

        private static ChatGPTRequest instance;
        /// <summary>
        /// Singleton instance of ChatGPTRequest.
        /// </summary>
        public static ChatGPTRequest Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<ChatGPTRequest>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("ChatGPTRequest");
                        instance = go.AddComponent<ChatGPTRequest>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        [Header("API Settings")]
        [SerializeField]
        private string apiKey = "";
        [SerializeField]
        private string model = "gpt-4o-mini";
        [SerializeField]
        private string apiUrl = "https://api.openai.com/v1/chat/completions";

        private string promptText = "";
        private Texture2D imageToSend = null;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        /// <summary>
        /// Encodes image to Base64 string.
        /// </summary>
        /// <param name="texture">The texture to encode.</param>
        /// <returns>Base64 string of the image.</returns>
        private string EncodeImageToBase64(Texture2D texture)
        {
            byte[] imageBytes = texture.EncodeToPNG();
            return Convert.ToBase64String(imageBytes);
        }

        /// <summary>
        /// Creates the JSON request body.
        /// </summary>
        /// <returns>JSON string for the request body.</returns>
        private string CreateRequestBody()
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("API key not set, please set a valid API key in the Inspector");
                return null;
            }
            if (imageToSend == null)
            {
                Debug.LogError("No image to send");
                return null;
            }
            string base64Image = EncodeImageToBase64(imageToSend);
            var requestObject = new
            {
                model = model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = promptText },
                            new
                            {
                                type = "image_url",
                                image_url = new { url = "data:image/png;base64," + base64Image, detail = "auto" }
                            }
                        }
                    }
                }
            };
            return JsonConvert.SerializeObject(requestObject);
        }

        /// <summary>
        /// Sends a request with prompt and image.
        /// </summary>
        /// <param name="prompt">Prompt text.</param>
        /// <param name="image">Image to send.</param>
        public void SendRequest(string prompt = "", Texture2D image = null)
        {
            SendRequest(prompt, image, null);
        }

        /// <summary>
        /// Sends a request with prompt, image and callback.
        /// </summary>
        /// <param name="prompt">Prompt text.</param>
        /// <param name="image">Image to send.</param>
        /// <param name="callback">Callback for response.</param>
        public void SendRequest(string prompt, Texture2D image, ResponseCallback callback)
        {
            promptText = prompt;
            imageToSend = image;
            StartCoroutine(SendRequestCoroutine(callback));
        }

        /// <summary>
        /// Coroutine to handle API request/response.
        /// </summary>
        /// <param name="callback">Callback for response.</param>
        /// <returns>IEnumerator for coroutine.</returns>
        private IEnumerator SendRequestCoroutine(ResponseCallback callback = null)
        {
            string response = "";
            string requestBody = CreateRequestBody();
            if (string.IsNullOrEmpty(requestBody))
            {
                response = "Error: API key not set or image invalid";
                callback?.Invoke(response);
                yield break;
            }
            using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    string errorMessage = request.error;
                    if (request.responseCode == 401)
                    {
                        errorMessage = "API key invalid (401)";
                    }
                    else if (request.responseCode == 429)
                    {
                        errorMessage = "Rate limit reached (429)";
                    }
                    Debug.LogError("Error: " + errorMessage);
                    response = "Error: " + errorMessage;
                }
                else
                {
                    string jsonResponse = request.downloadHandler.text;
                    JObject responseObject = JObject.Parse(jsonResponse);
                    if (responseObject["choices"] != null && responseObject["choices"].HasValues)
                    {
                        response = responseObject["choices"][0]["message"]["content"].ToString();
                    }
                    else
                    {
                        response = "Error: Unable to parse response";
                    }
                }
                callback?.Invoke(response);
            }
        }

        /// <summary>
        /// Sends a multi-image prompt sequence (text and image pairs) to ChatGPT.
        /// </summary>
        public void SendMultiImageRequest(List<(string text, Texture2D image)> promptSequence, Action<string> callback)
        {
            StartCoroutine(SendMultiImageRequestCoroutine(promptSequence, callback));
        }

        private IEnumerator SendMultiImageRequestCoroutine(List<(string text, Texture2D image)> promptSequence, Action<string> callback)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                callback?.Invoke("Error: API key not set");
                yield break;
            }
            var contentList = new List<object>();
            foreach (var (text, image) in promptSequence)
            {
                if (!string.IsNullOrEmpty(text))
                    contentList.Add(new { type = "text", text = text });
                if (image != null)
                {
                    string base64 = EncodeImageToBase64(image);
                    contentList.Add(new
                    {
                        type = "image_url",
                        image_url = new { url = "data:image/png;base64," + base64, detail = "auto" }
                    });
                }
            }
            var requestObject = new
            {
                model = model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = contentList
                    }
                }
            };
            string requestBody = JsonConvert.SerializeObject(requestObject);
            using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);
                yield return request.SendWebRequest();
                string response = "";
                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    response = "Error: " + request.error;
                }
                else
                {
                    string jsonResponse = request.downloadHandler.text;
                    JObject responseObject = JObject.Parse(jsonResponse);
                    if (responseObject["choices"] != null && responseObject["choices"].HasValues)
                        response = responseObject["choices"][0]["message"]["content"].ToString();
                    else
                        response = "Error: Unable to parse response";
                }
                callback?.Invoke(response);
            }
        }
    }
}