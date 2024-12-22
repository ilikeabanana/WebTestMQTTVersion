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
using UnityEngine.InputSystem;

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

        private const string BrokerAddress = "1f88b25c6dec42949e774d673013789d.s1.eu.hivemq.cloud"; // Replace with your broker address
        private const int BrokerPort = 8883; // Default MQTT port
        private const string BrokerUsername = "Bananaman"; // Optional username
        private const string BrokerPassword = "HMkw2X4inSwj@m5"; // Optional password
        private const string Topic = "messages/actors";
        private const string EncryptionKey = "MySecureKey123!"; // Replace with your encryption password
        MessageHandler handler;
        /*Incase you are wondering why the hell im using variables instead of a void heres a short explanation
         * I first tried making it use voids but it for some reason didnt work no matter what i did
         * Eventually i did a test (which is why the handler for controls is called "JustATestLogger" i was to lazy to change the name
         * That DID work so i tried calling it back in MessageHandler. It didnt work.
         * So i tried making a void in this class and calling it in MessageHandler. 
         * That did NOT work
         * So thats how this became a thing
         * It might be bad but like. i couldnt figure out anything else so...
         * (DM/ping me if you can made it better)
         */
        public static bool LaunchLoggerthing = false;
        public static string Name = "";
        public static JsonBindingMQTT Path = null;
        private IMqttClient mqttClient;
        public static JsonBindingMap map;
        JustATestLogger thingthatisntactualyatestanymore;
        public float DMGMultiPLR = 1;
        public float DMGMultiENEMY = 1;
        public static WebTestMQTTVersionHostPlugin Instance {  get; private set; }


        private async void Start()
        {
            Instance = this;
            handler = gameObject.AddComponent<MessageHandler>();
            thingthatisntactualyatestanymore = gameObject.AddComponent<JustATestLogger>();
            Logger.LogInfo("Starting Host Mod...");
            await InitializeMqttClientAsync(); 
        }

        private async Task InitializeMqttClientAsync()
        {
            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();

            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(BrokerAddress, BrokerPort)
                .WithTlsOptions(new MqttClientTlsOptions
                {
                    UseTls = true,
                    AllowUntrustedCertificates = false,
                    CertificateValidationHandler = context => true
                });

            // Add credentials if username and password are provided
            if (!string.IsNullOrEmpty(BrokerUsername) && !string.IsNullOrEmpty(BrokerPassword))
            {
                optionsBuilder.WithCredentials(BrokerUsername, BrokerPassword);
            }

            var options = optionsBuilder.Build();

            try
            {
                mqttClient.ConnectedAsync += async e =>
                {
                    Logger.LogInfo($"Connected to MQTT broker at {BrokerAddress}:{BrokerPort}.");
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
            catch (Exception ex)
            {
                Logger.LogError($"Failed to connect to MQTT Broker: {ex.Message}");
            }
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
                InputAction action = MonoSingleton<InputManager>.instance.InputSource.Actions.FindAction(Name);
                if (action == null) return;
                if (Path == null) return;
                if (Path.path == null) return;
                thingthatisntactualyatestanymore.ChangeKey(action, Path, MonoSingleton<InputManager>.instance.InputSource.Actions.KeyboardMouseScheme);
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
        public float delay;
        public float amount;
    }
    public class JsonBindingMQTT
    {
        public string path;
        public bool isComposite;
        public string[] compositePath;
    }


    public class MessageDataJson
    {
        public string User { get; set; }
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

    [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHurt))]
    public class PLRDAMAGE
    {
        public static bool Prefix(ref int damage)
        {
            damage *= (int)WebTestMQTTVersionHostPlugin.Instance.DMGMultiPLR;
            return true;
        }
    }
    [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.DeliverDamage))]
    public class EnemyDAMAGE
    {
        public static bool Prefix(ref float multiplier)
        {
            multiplier *= (int)WebTestMQTTVersionHostPlugin.Instance.DMGMultiENEMY;
            return true;
        }
    }
    [Serializable]
    public class MeshData
    {
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector2[] uvs;
        public int[] triangles;
    }

    [Serializable]
    public class MaterialData
    {
        public string name;
        public string shaderName;
        public string textureName;
    }

    [Serializable]
    public class ModelData
    {
        public MeshData[] meshes;
        public MaterialData[] materials;
    }
}
