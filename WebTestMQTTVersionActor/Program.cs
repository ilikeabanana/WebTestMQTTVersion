using System;
using System.Security.Cryptography;
using System.Text;
using MQTTnet;
using MQTTnet.Client;

class Program
{
    private const string BrokerAddress = "127.0.0.1"; // Replace with your broker address
    private const string Topic = "messages/actors";
    private const string EncryptionKey = "MySecureKey123!"; // Replace with your encryption password

    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Actor Client...");

        var factory = new MqttFactory();
        var mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(BrokerAddress)
            .Build();

        mqttClient.ConnectedAsync += async e =>
        {
            Console.WriteLine("Connected to MQTT broker.");
        };

        mqttClient.DisconnectedAsync += async e =>
        {
            Console.WriteLine("Disconnected from MQTT broker.");
        };

        await mqttClient.ConnectAsync(options);

        while (true)
        {
            Console.Write("Enter message to send to the host: ");
            string message = Console.ReadLine();

            string encryptedMessage = EncryptMessage(message, EncryptionKey);

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(Topic)
                .WithPayload(encryptedMessage)
                .Build();

            await mqttClient.PublishAsync(mqttMessage);
            Console.WriteLine("Message sent: " + encryptedMessage);
        }
    }

    private static string EncryptMessage(string plainText, string key)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
            aes.IV = new byte[16];

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
            return Convert.ToBase64String(encryptedBytes);
        }
    }
}
