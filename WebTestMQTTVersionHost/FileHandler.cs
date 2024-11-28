using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
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
            string fileExtension = Path.GetExtension(fileName).ToLower();

            switch (fileExtension)
            {
                case ".png":
                    HandlePNG(fileData);
                    break;

                case ".wav":
                case ".ogg":
                    HandleAudio(fileData, fileExtension);
                    break;

                default:
                    Debug.LogWarning("Unsupported file type: " + fileExtension);
                    break;
            }
        }

        // Handle PNG files and create a Sprite
        private static void HandlePNG(byte[] fileData)
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);

            // Create a Sprite
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            Debug.Log("Sprite created successfully!");
            OptionsMenuToManager manager = MonoSingleton<OptionsMenuToManager>.Instance;
            if(manager != null)
            {
                GameObject go = new GameObject("Image");
                go.transform.parent = manager.transform;
                go.AddComponent<Image>().sprite = sprite;
                go.transform.localPosition = Vector3.zero;
            }
        }

        // Handle WAV/OGG files and play the audio
        private static void HandleAudio(byte[] fileData, string fileExtension)
        {
            AudioClip audioClip = null;
            string tempPath = Path.Combine(Application.temporaryCachePath, "temp" + fileExtension);

            try
            {
                // Save the audio file temporarily
                File.WriteAllBytes(tempPath, fileData);

                // Load the audio clip from the temporary file
                audioClip = fileExtension == ".wav"
                    ? new WWW("file://" + tempPath).GetAudioClip(false, false, AudioType.WAV)
                    : new WWW("file://" + tempPath).GetAudioClip(false, false, AudioType.OGGVORBIS);

                if (audioClip != null)
                {
                    // Play the audio clip
                    AudioSource audioSource = new GameObject("AudioPlayer").AddComponent<AudioSource>();
                    audioSource.clip = audioClip;
                    audioSource.Play();

                    Debug.Log("Audio is playing!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to process audio file: " + ex.Message);
            }
            finally
            {
                // Cleanup temporary file
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }
    }
}
