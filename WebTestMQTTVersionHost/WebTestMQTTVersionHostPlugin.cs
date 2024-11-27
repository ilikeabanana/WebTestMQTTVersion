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
        private IMqttClient mqttClient;

        private void Start()
        {
            handler = new MessageHandler();
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
    }
    public class MessageDataJson
    {
        public string Message { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }
}
