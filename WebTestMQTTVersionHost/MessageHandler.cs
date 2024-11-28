using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WebTestMQTTVersionHost
{
    internal class MessageHandler
    {
        public void HandleMessage(MessageDataJson messageData)
        {
            if(messageData.Type == "text")
            {
                HandleTextData(messageData);
            }
            if(messageData.Type == "setting")
            {
                WebTestMQTTVersionHostPlugin.Log.LogInfo("Its a setting");
                HandleSettingData(messageData);
            }
            if (messageData.Type == "file")
            {
                HandleFileData(messageData);
            }
        }

        public void HandleSettingData(MessageDataJson settingData)
        {
            try
            {
                // Get the type of the runtime instance
                var optionsMenuType = MonoSingleton<OptionsMenuToManager>.Instance.GetType();
                WebTestMQTTVersionHostPlugin.Log.LogInfo($"Retrieved runtime type: {optionsMenuType.Name}");

                // Get the singleton instance
                var instance = MonoSingleton<OptionsMenuToManager>.Instance;
                if (instance == null)
                {
                    Debug.LogError("OptionsMenuToManager instance is null.");
                    return;
                }
                WebTestMQTTVersionHostPlugin.Log.LogInfo("Successfully retrieved OptionsMenuToManager instance.");

                // Log all methods for debugging
                var methods = optionsMenuType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                WebTestMQTTVersionHostPlugin.Log.LogInfo($"Found {methods.Length} methods in {optionsMenuType.Name}:");

                foreach (var m in methods)
                {
                    WebTestMQTTVersionHostPlugin.Log.LogInfo($"Method: {m.Name}, Parameter Count: {m.GetParameters().Length}");
                }

                // Find the method by name
                var method = methods.FirstOrDefault(m => m.Name == settingData.Message && m.GetParameters().Length == 1);
                if (method == null)
                {
                    Debug.LogError($"No method found with name {settingData.Message} and one parameter.");
                    return;
                }
                WebTestMQTTVersionHostPlugin.Log.LogInfo($"Found method: {method.Name}");

                // Get the parameter info and type
                var parameterType = method.GetParameters()[0].ParameterType;
                WebTestMQTTVersionHostPlugin.Log.LogInfo($"Method parameter type: {parameterType.FullName}");

                // Convert the value to the correct type
                var convertedValue = Convert.ChangeType(settingData.Value, parameterType);
                WebTestMQTTVersionHostPlugin.Log.LogInfo($"Converted value: {convertedValue}");

                // Invoke the method
                method.Invoke(instance, new[] { convertedValue });
                WebTestMQTTVersionHostPlugin.Log.LogInfo($"Successfully invoked {settingData.Message} with value {settingData.Value}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to handle setting data: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void HandleFileData(MessageDataJson messageData)
        {
            
            byte[] bytes = FileHandler.Base64ToFile(messageData.Value);
            FileHandler.ProcessFile(bytes, messageData.FileName);
        }

        public void HandleTextData(MessageDataJson messageData)
        {
            string command = messageData.Message;
            switch (command)
            {
                case "DupeEnemies":
                    WebTestMQTTVersionHostPlugin.Log.LogInfo("Duplicating enemies");
                    break;
                case "BuffEnemies":
                    WebTestMQTTVersionHostPlugin.Log.LogInfo("Buffing enemies");
                    break;
                case "SetScreenShake":
                    MonoSingleton<PrefsManager>.Instance.SetFloat("screenShake", int.Parse(messageData.Value));
                    break;
            }
        }

        
    }
}
