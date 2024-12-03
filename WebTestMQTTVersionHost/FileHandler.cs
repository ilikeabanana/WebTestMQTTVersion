using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace WebTestMQTTVersionHost
{
    internal class FileHandler
    {
        public static byte[] Base64ToFile(string base64String)
        {
            return Convert.FromBase64String(base64String);
        }

        // Detect file type and process it accordingly
        public static void ProcessFile(byte[] fileData, string fileName)
        {
            WebTestMQTTVersionHostPlugin.Log.LogInfo("Processing file...");
            string fileExtension = Path.GetExtension(fileName).ToLower();

            switch (fileExtension)
            {
                case ".png":
                    WebTestMQTTVersionHostPlugin.Log.LogInfo("It's an image");
                    HandlePNG(fileData);
                    break;

                case ".wav":
                case ".ogg":
                    WebTestMQTTVersionHostPlugin.Log.LogInfo("It's an audio file");
                    HandleAudio(fileData, fileExtension);
                    break;

                default:
                    WebTestMQTTVersionHostPlugin.Log.LogWarning("Unsupported file type: " + fileExtension);
                    break;
            }
        }

        // Handle PNG files and create a Sprite
        private static void HandlePNG(byte[] fileData)
        {
            // Convert byte array to base64 for UnityWebRequest
            WebTestMQTTVersionHostPlugin.Log.LogInfo("Converted image to base64");
            MonoSingleton<OptionsMenuToManager>.Instance.StartCoroutine(LoadImage(fileData));
        }

        private static IEnumerator LoadImage(byte[] imageBytes)
        {
            yield return new WaitForSeconds(0.1f);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                WebTestMQTTVersionHostPlugin.Log.LogError("Image byte array is null or empty.");
                yield break;
            }

            WebTestMQTTVersionHostPlugin.Log.LogInfo("Attempting to decode base64 and load image...");
            Texture2D texture = new Texture2D(2, 2); // Temporary size

            try
            {
                if (!texture.LoadImage(imageBytes))
                {
                    WebTestMQTTVersionHostPlugin.Log.LogError("LoadImage failed to decode byte array.");
                    yield break;
                }

                WebTestMQTTVersionHostPlugin.Log.LogInfo($"Image loaded successfully. Dimensions: {texture.width}x{texture.height}");
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                WebTestMQTTVersionHostPlugin.Log.LogInfo("Sprite created.");

                OptionsMenuToManager manager = MonoSingleton<OptionsMenuToManager>.Instance;
                if (manager != null)
                {
                    GameObject go = new GameObject("Image");
                    go.transform.SetParent(manager.transform, false);
                    Image img = go.AddComponent<Image>();
                    img.sprite = sprite;
                    go.transform.localPosition = Vector3.zero;
                    WebTestMQTTVersionHostPlugin.Log.LogInfo("Image added to screen.");
                    while(img.color.a > 0)
                    {
                        img.color = img.color - new Color(0, 0, 0, 0.01f);
                    }
                    GameObject.Destroy(go);
                }
                else
                {
                    WebTestMQTTVersionHostPlugin.Log.LogError("OptionsMenuToManager instance is null.");
                }
            }
            catch (Exception ex)
            {
                WebTestMQTTVersionHostPlugin.Log.LogError($"Exception during image loading: {ex.Message}");
            }

            yield return null;
        }


        // Handle WAV/OGG files and play the audio
        private static void HandleAudio(byte[] fileData, string fileExtension)
        {
            string tempPath = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString() + fileExtension);

            try
            {
                // Save the audio file temporarily
                File.WriteAllBytes(tempPath, fileData);

                // Load and play the audio clip
                MonoSingleton<OptionsMenuToManager>.Instance.StartCoroutine(LoadAudioClip("file://" + tempPath,
                    fileExtension == ".wav" ? AudioType.WAV : AudioType.OGGVORBIS,
                    (audioClip) =>
                    {
                        if (audioClip != null)
                        {
                            GameObject audioPlayer = new GameObject("AudioPlayer");
                            AudioSource audioSource = audioPlayer.AddComponent<AudioSource>();
                            audioSource.clip = audioClip;
                            audioSource.Play();

                            WebTestMQTTVersionHostPlugin.Log.LogInfo("Audio is playing!");

                            // Cleanup after the audio finishes
                            MonoSingleton<OptionsMenuToManager>.Instance.StartCoroutine(CleanupAfterAudio(audioPlayer, audioClip));
                        }
                        else
                        {
                            WebTestMQTTVersionHostPlugin.Log.LogError("AudioClip is null!");
                        }

                        // Delete the temporary file
                        MonoSingleton<OptionsMenuToManager>.Instance.StartCoroutine(DeleteTempFileAfterDelay(tempPath));
                    }));
            }
            catch (Exception ex)
            {
                WebTestMQTTVersionHostPlugin.Log.LogError("Failed to process audio file: " + ex.Message);
            }
        }

        // Coroutine to load an AudioClip
        private static IEnumerator LoadAudioClip(string path, AudioType type, Action<AudioClip> onLoaded)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, type))
            {
                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    WebTestMQTTVersionHostPlugin.Log.LogError("Failed to load audio: " + www.error);
                    onLoaded?.Invoke(null);
                }
                else
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    onLoaded?.Invoke(clip);
                }
            }
        }

        // Coroutine to cleanup temporary files after a delay
        private static IEnumerator DeleteTempFileAfterDelay(string path)
        {
            yield return new WaitForSeconds(1f);
            if (File.Exists(path))
                File.Delete(path);
        }

        // Coroutine to cleanup audio objects after playback
        private static IEnumerator CleanupAfterAudio(GameObject audioPlayer, AudioClip audioClip)
        {
            yield return new WaitUntil(() => !audioPlayer.GetComponent<AudioSource>().isPlaying);

            if (audioClip != null)
                AudioClip.Destroy(audioClip);

            if (audioPlayer != null)
                GameObject.Destroy(audioPlayer);

            WebTestMQTTVersionHostPlugin.Log.LogInfo("Audio cleanup completed.");
        }
    }
}
