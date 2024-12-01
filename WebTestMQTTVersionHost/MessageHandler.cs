using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object= UnityEngine.Object;

namespace WebTestMQTTVersionHost
{
    internal class MessageHandler : MonoBehaviour
    {
        public void HandleMessage(MessageDataJson messageData)
        {
            WebTestMQTTVersionHostPlugin.Log.LogInfo("Changing shit");
            if (messageData.Type == "text")
            {
                WebTestMQTTVersionHostPlugin.Log.LogInfo("Changing text");
                HandleTextData(messageData);
            }
            if(messageData.Type == "setting")
            {
                WebTestMQTTVersionHostPlugin.Log.LogInfo("Changing setting");
                HandleSettingData(messageData);
            }
            if(messageData.Type == "graphics")
            {
                WebTestMQTTVersionHostPlugin.Log.LogInfo("Changing graphics");
                WebTestMQTTVersionHostPlugin.Log.LogInfo(MonoSingleton<GraphicsOptions>.Instance == null ? "fucking" : "work");
                DebugGraphicsOptionsStatus();
                WebTestMQTTVersionHostPlugin.Log.LogInfo("graphicssssss");
                InvokeGraphicsOptionsMethod(messageData.Message, messageData.Value);
            }
            if (messageData.Type == "file")
            {
                HandleFileData(messageData);
            }
            if(messageData.Type == "Control")
            {
                WebTestMQTTVersionHostPlugin.Log.LogInfo("Changing control");
                HandleControlSettingData(messageData);
            }
        }
        
        
        public void SetActionBinding(string actionName, string path)
        {
            InputManager inputManager = MonoSingleton<InputManager>.Instance;
            JsonBindingMap map = JsonConvert.DeserializeObject<JsonBindingMap>(File.ReadAllText(inputManager.savedBindingsFile.FullName));
            ModifyJumpAction(map);
            File.WriteAllText(inputManager.savedBindingsFile.FullName, JsonConvert.SerializeObject(WebTestMQTTVersionHostPlugin.map, Formatting.Indented));
            inputManager.InputSource = new PlayerInput();
            inputManager.defaultActions = InputActionAsset.FromJson(inputManager.InputSource.Actions.asset.ToJson());
            if (inputManager.savedBindingsFile.Exists)
            {
                map.ApplyTo(inputManager.InputSource.Actions.asset);
            }
            inputManager.anyButtonListener = InputManager.onAnyInput.Subscribe(InputManager.ButtonPressListener.Instance);
            map.ApplyTo(inputManager.InputSource.Actions.asset);
            inputManager.InputSource.ValidateBindings(inputManager.InputSource.Actions.KeyboardMouseScheme);
        }
        public void ResetToDefault(InputAction action, InputControlScheme controlScheme)
        {
            InputManager inputManager = MonoSingleton<InputManager>.Instance;
            InputAction inputAction = inputManager.defaultActions.FindAction(action.name, false);
            inputManager.InputSource.Disable();
            action.WipeAction(controlScheme.bindingGroup);
            for (int i = 0; i < inputAction.bindings.Count; i++)
            {
                Debug.Log(inputAction.bindings[i].path);
                if (inputAction.BindingHasGroup(i, controlScheme.bindingGroup))
                {
                    InputBinding inputBinding = inputAction.bindings[i];
                    if (!inputBinding.isPartOfComposite)
                    {
                        if (inputBinding.isComposite)
                        {
                            InputActionSetupExtensions.CompositeSyntax compositeSyntax = action.AddCompositeBinding("2DVector", null, null);
                            for (int j = i + 1; j < inputAction.bindings.Count; j++)
                            {
                                if (!inputAction.bindings[j].isPartOfComposite)
                                {
                                    break;
                                }
                                InputBinding inputBinding2 = inputAction.bindings[j];
                                compositeSyntax.With(inputBinding2.name, inputBinding2.path, controlScheme.bindingGroup, null);
                            }
                        }
                        else
                        {
                            action.AddBinding(inputBinding).WithGroup(controlScheme.bindingGroup);
                        }
                    }
                }
            }
            Action<InputAction> action2 = inputManager.actionModified;
            if (action2 != null)
            {
                action2(action);
            }
            inputManager.SaveBindings(inputManager.InputSource.Actions.asset);
            inputManager.InputSource.Enable();
        }
        public void ModifyJumpAction(JsonBindingMap bindingMap)
        {
            // The name of the action you want to modify
            string actionName = "Jump";

            // Create a new JsonBinding for <Keyboard>/g
            JsonBinding newBinding = new JsonBinding
            {
                path = "<Keyboard>/g",
                isComposite = false
            };

            // Check if the action exists in modifiedActions
            if (bindingMap.modifiedActions.ContainsKey(actionName))
            {
                // Clear existing bindings for "Jump" and replace with the new one
                bindingMap.modifiedActions[actionName].Clear();
                bindingMap.modifiedActions[actionName].Add(newBinding);
            }
            else
            {
                // If the action doesn't exist, add it to the dictionary
                bindingMap.modifiedActions[actionName] = new List<JsonBinding> { newBinding };
            }
            
            WebTestMQTTVersionHostPlugin.map = bindingMap;
            
        }
        

        public void HandleControlSettingData(MessageDataJson controlData)
        {
            HudMessageReceiver.Instance.SendHudMessage($"Someone set {controlData.Message} to {controlData.Value}");
            var inman = MonoSingleton<InputManager>.Instance;
            if (inman == null)
            {
                Debug.LogError("Input Manager is null!");
                return;
            }

            var inputSource = inman.InputSource;
            if (inputSource == null)
            {
                Debug.LogError("Input Source is null!");
                return;
            }

            var jumpAction = inputSource.Jump?.Action;
            if (jumpAction == null)
            {
                Debug.LogError("Jump Action is null!");
                return;
            }

            // Detailed logging of current bindings
            Debug.Log("Current Jump Action Bindings:");
            foreach (var binding in jumpAction.bindings)
            {
                Debug.Log($"Path: {binding.path}, Groups: {binding.groups}");
            }

            WebTestMQTTVersionHostPlugin.Path = controlData.Value;
            WebTestMQTTVersionHostPlugin.Name = controlData.Message;
            WebTestMQTTVersionHostPlugin.LaunchLoggerthing = true;
            //SetActionBinding("Jump", "<Keyboard>/g");
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
                    WebTestMQTTVersionHostPlugin.Log.LogError("OptionsMenuToManager instance is null.");
                    return;
                }
                WebTestMQTTVersionHostPlugin.Log.LogInfo("Successfully retrieved OptionsMenuToManager instance.");

                // Search for field or property
                FieldInfo field = optionsMenuType.GetField(
                    settingData.Message,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
                );
                PropertyInfo property = optionsMenuType.GetProperty(
                    settingData.Message,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
                );

                if (field == null && property == null)
                {
                    WebTestMQTTVersionHostPlugin.Log.LogError($"Field or Property '{settingData.Message}' not found in OptionsMenuToManager!");
                    return;
                }

                // Retrieve the target variable (field or property)
                object target = field != null ? field.GetValue(instance) : property.GetValue(instance);

                if (target == null)
                {
                    WebTestMQTTVersionHostPlugin.Log.LogError($"Field or Property '{settingData.Message}' is null!");
                    return;
                }

                // Handle variable update based on type
                try
                {
                    if (target is Slider slider)
                    {
                        if (float.TryParse(settingData.Value, out float sliderValue))
                        {
                            if (slider.maxValue < sliderValue) 
                                slider.maxValue = sliderValue;
                            if (slider.minValue > sliderValue)
                                slider.minValue = sliderValue;
                            slider.value = sliderValue; // Update slider value
                            slider.onValueChanged.Invoke(sliderValue); // Trigger onValueChanged
                        }
                        else
                        {
                            WebTestMQTTVersionHostPlugin.Log.LogError($"Invalid value '{settingData.Value}' for Slider '{settingData.Message}'");
                        }
                    }
                    else if (target is TMP_Dropdown dropdown)
                    {
                        if (int.TryParse(settingData.Value, out int dropdownValue))
                        {
                            dropdown.value = dropdownValue; // Update dropdown value
                            dropdown.onValueChanged.Invoke(dropdownValue); // Trigger onValueChanged
                        }
                        else
                        {
                            WebTestMQTTVersionHostPlugin.Log.LogError($"Invalid value '{settingData.Value}' for TMP_Dropdown '{settingData.Message}'");
                        }
                    }
                    else if (target is Toggle toggle)
                    {
                        if (bool.TryParse(settingData.Value, out bool toggleValue))
                        {
                            toggle.isOn = toggleValue; // Update toggle value
                            toggle.onValueChanged.Invoke(toggleValue); // Trigger onValueChanged
                        }
                        else
                        {
                            WebTestMQTTVersionHostPlugin.Log.LogError($"Invalid value '{settingData.Value}' for Toggle '{settingData.Message}'");
                        }
                    }
                    else
                    {
                        WebTestMQTTVersionHostPlugin.Log.LogError($"Unsupported type '{target.GetType()}' for '{settingData.Message}'");
                    }
                }
                catch (Exception ex)
                {
                    WebTestMQTTVersionHostPlugin.Log.LogError($"Error updating '{settingData.Message}': {ex.Message}");
                }

                WebTestMQTTVersionHostPlugin.Log.LogInfo($"Successfully updated {settingData.Message} with value {settingData.Value}");
            }
            catch (Exception ex)
            {
                WebTestMQTTVersionHostPlugin.Log.LogError($"Failed to handle setting data: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static object InvokeGraphicsOptionsMethod(string methodName, string value = null)
        {
            // Log attempt to invoke the method
            WebTestMQTTVersionHostPlugin.Log.LogInfo($"Attempting to find and modify variable: {methodName}");

            // Get the GraphicsOptions instance
            var graphicsOptions = MonoSingleton<GraphicsOptions>.Instance;

            if (graphicsOptions == null)
            {
                WebTestMQTTVersionHostPlugin.Log.LogError($"GraphicsOptions Instance is NULL!");
                WebTestMQTTVersionHostPlugin.Log.LogError("GraphicsOptions Instance is NULL!");

                // Try to find GraphicsOptions in the scene
                var allGraphicsOptions = Object.FindObjectsOfType<GraphicsOptions>();
                WebTestMQTTVersionHostPlugin.Log.LogInfo($"Total GraphicsOptions components found: {allGraphicsOptions.Length}");
                if (allGraphicsOptions.Length > 0)
                {
                    graphicsOptions = allGraphicsOptions[0];
                }
                else
                {
                    return null;
                }
            }

            // Log type information
            WebTestMQTTVersionHostPlugin.Log.LogInfo($"GraphicsOptions Type: {graphicsOptions.GetType().FullName}");

            // Look for the field or property with the given name
            FieldInfo field = graphicsOptions.GetType().GetField(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
            );
            PropertyInfo property = graphicsOptions.GetType().GetProperty(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
            );

            if (field == null && property == null)
            {
                WebTestMQTTVersionHostPlugin.Log.LogError($"Field or Property '{methodName}' not found in GraphicsOptions!");
                return null;
            }

            // Get the target object (field or property)
            object target = field != null ? field.GetValue(graphicsOptions) : property.GetValue(graphicsOptions);

            if (target == null)
            {
                WebTestMQTTVersionHostPlugin.Log.LogError($"Field or Property '{methodName}' is null!");
                return null;
            }

            // Handle the variable based on its type
            try
            {
                if (target is Slider slider)
                {
                    if (float.TryParse(value, out float sliderValue))
                    {
                        if (slider.maxValue < sliderValue)
                            slider.maxValue = sliderValue;
                        if (slider.minValue > sliderValue)
                            slider.minValue = sliderValue;
                        slider.value = sliderValue; // Update value
                        slider.onValueChanged.Invoke(sliderValue); // Trigger event
                    }
                    else
                    {
                        WebTestMQTTVersionHostPlugin.Log.LogError($"Invalid value '{value}' for Slider '{methodName}'");
                    }
                }
                else if (target is TMP_Dropdown dropdown)
                {
                    if (int.TryParse(value, out int dropdownValue))
                    {
                        dropdown.value = dropdownValue; // Update value
                        dropdown.onValueChanged.Invoke(dropdownValue); // Trigger event
                    }
                    else
                    {
                        WebTestMQTTVersionHostPlugin.Log.LogError($"Invalid value '{value}' for TMP_Dropdown '{methodName}'");
                    }
                }
                else if (target is Toggle toggle)
                {
                    if (bool.TryParse(value, out bool toggleValue))
                    {
                        toggle.isOn = toggleValue; // Update value
                        toggle.onValueChanged.Invoke(toggleValue); // Trigger event
                    }
                    else
                    {
                        WebTestMQTTVersionHostPlugin.Log.LogError($"Invalid value '{value}' for Toggle '{methodName}'");
                    }
                }
                else
                {
                    WebTestMQTTVersionHostPlugin.Log.LogError($"Unsupported type '{target.GetType()}' for '{methodName}'");
                }
            }
            catch (Exception ex)
            {
                WebTestMQTTVersionHostPlugin.Log.LogError($"Error updating '{methodName}': {ex.Message}");
                return null;
            }

            return target; // Return the modified component
        }


        public void CheckWhichItIs(string method, object param, GraphicsOptions options)
        {
            if(method == "Dithering")
            {
                float paramfloat = (float)param;
                options.dithering.value = paramfloat;
            }
        }
        public static void DebugGraphicsOptionsStatus()
        {
            var graphicsOptions = MonoSingleton<GraphicsOptions>.Instance;

            WebTestMQTTVersionHostPlugin.Log.LogInfo("--- GraphicsOptions Debug ---");
            WebTestMQTTVersionHostPlugin.Log.LogInfo($"MonoSingleton Instance: {graphicsOptions != null}");

            var allGraphicsOptions = Object.FindObjectsOfType<GraphicsOptions>();
            WebTestMQTTVersionHostPlugin.Log.LogInfo($"Total GraphicsOptions in scene: {allGraphicsOptions.Length}");

            if (allGraphicsOptions.Length > 0)
            {
                foreach (var option in allGraphicsOptions)
                {
                    WebTestMQTTVersionHostPlugin.Log.LogInfo($"Found GraphicsOptions Component: {option.name}");
                }
            }
        }
        // Debugging method to print available methods
        public static void PrintAvailableMethods()
        {
            var graphicsOptions = Object.FindObjectOfType<GraphicsOptions>();

            if (graphicsOptions == null)
            {
                WebTestMQTTVersionHostPlugin.Log.LogError("No GraphicsOptions component found!");
                return;
            }

            WebTestMQTTVersionHostPlugin.Log.LogInfo("Available Methods in GraphicsOptions:");
            foreach (var method in graphicsOptions.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static))
            {
                WebTestMQTTVersionHostPlugin.Log.LogInfo($"- {method.Name}");
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
                case "SendToLevel":
                    WebTestMQTTVersionHostPlugin.Log.LogInfo("Sending player to level " + messageData.Value);
                    HandleLeveling(messageData.Value);
                    break;
            }
        }
        public void HandleLeveling(string levelText)
        {

           SceneHelper.LoadScene(levelText, false);
            
        }
        
    }
}
public class Binding
{
    public string Action { get; set; }
    public string Id { get; set; }
    public string Path { get; set; }
    public string Interactions { get; set; }
    public string Processors { get; set; }
}

public class Root
{
    public List<Binding> Bindings { get; set; }
}