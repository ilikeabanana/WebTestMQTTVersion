using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MQTTnet.Client;
using MQTTnet;
using System.Security.Cryptography;
using System.Text;
using System;
using UnityEngine;
using Newtonsoft.Json;
using System.Threading.Tasks;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;

namespace WebTestMQTTVersionHost
{
    // TODO Review this file and update to your own requirements.

    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class WebTestMQTTVersionHostPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.michi.WebTestMQTTVersionHost";
        private const string PluginName = "WebTestMQTTVersionHost";
        private const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        private const string BrokerAddress = "127.0.0.1"; // Replace with your broker address
        private const string Topic = "messages/actors";
        private const string EncryptionKey = "MySecureKey123!"; // Replace with your encryption password
        MessageHandler handler;
        public static bool LaunchLoggerthing = false;
        public static string Name = "";
        public static string Path = "";
        private IMqttClient mqttClient;
        public static JsonBindingMap map;
        JustATestLogger thingthatisntactualyatestanymore;
        public static WebTestMQTTVersionHostPlugin Instance {  get; private set; }


        private void Start()
        {
            Instance = this;
            handler = gameObject.AddComponent<MessageHandler>();
            thingthatisntactualyatestanymore = gameObject.AddComponent<JustATestLogger>();
            Logger.LogInfo("Starting Host Mod...");
            ConnectToMqttBroker();
        }

        private async void ConnectToMqttBroker()
        {
            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(BrokerAddress)
                .Build();

            mqttClient.ConnectedAsync += async e =>
            {
                Logger.LogInfo("Connected to MQTT broker.");
                await mqttClient.SubscribeAsync(Topic);
            };

            mqttClient.DisconnectedAsync += e =>
            {
                Logger.LogInfo("Disconnected from MQTT broker.");
                return Task.CompletedTask;
            };

            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                string encryptedMessage = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                string decryptedMessage = DecryptMessage(encryptedMessage, EncryptionKey);
                MessageDataJson data = JsonConvert.DeserializeObject<MessageDataJson>(decryptedMessage);

                Logger.LogInfo($"Received message: message: {data.Message}, Type: {data.Type}, Value: {data.Value}");
                handler.HandleMessage(data);
                return Task.CompletedTask;
            };

            await mqttClient.ConnectAsync(options);
        }

        private static string DecryptMessage(string encryptedText, string key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16];

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }

        private void OnDestroy()
        {
            mqttClient?.DisconnectAsync();
        }
        private void Awake()
        {

            // Apply all of our patches
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");

            Log = Logger;
        }

        void Update()
        {
            if (LaunchLoggerthing)
            {
                LaunchLoggerthing = false;
                thingthatisntactualyatestanymore.ChangeKey(Name, Path);
            }
        }
    }
    [System.Serializable]
    public class SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    public class PKEYValueData
    {
        public string PKEY;
        public SerializableVector3 Vector3Pos;
        public bool Relative;
    }


    public class MessageDataJson
    {
        public string Message { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public string FileName { get; set; }
    }
    [HarmonyPatch(typeof(Slider), nameof(Slider.UpdateDrag))]
    public class UnrestrictedSliderDragPatch
    {
        static bool Prefix(Slider __instance, PointerEventData eventData, Camera cam)
        {
            RectTransform rectTransform = __instance.m_HandleContainerRect ?? __instance.m_FillContainerRect;
            if (rectTransform != null && rectTransform.rect.size[(int)__instance.axis] > 0f)
            {
                Vector2 zero = Vector2.zero;
                MultipleDisplayUtilities.GetRelativeMousePositionForDrag(eventData, ref zero);

                Vector2 vector;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, zero, cam, out vector);

                vector -= rectTransform.rect.position;
                float sliderSize = rectTransform.rect.size[(int)__instance.axis];

                // Calculate normalized value based on total mouse movement, not just slider bounds
                float normalizedValue = (vector - __instance.m_Offset)[(int)__instance.axis] / sliderSize;

                // Allow going beyond 0-1
                __instance.m_Value = Mathf.Lerp(__instance.minValue, __instance.maxValue, normalizedValue);

                __instance.UpdateVisuals();
                UISystemProfilerApi.AddMarker("Slider.value", __instance);
                __instance.m_OnValueChanged.Invoke(__instance.m_Value);
            }

            return false; // Prevent original method from running
        }
    }
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.SaveBindings))]
    public class SaveTheBindings
    {
        public static bool Prefix(InputManager __instance)
        {
            if(WebTestMQTTVersionHostPlugin.map != null)
            {
                File.WriteAllText(__instance.savedBindingsFile.FullName, JsonConvert.SerializeObject(WebTestMQTTVersionHostPlugin.map, Formatting.Indented));
                return false;
            }
            return true;
        }
    }
}
