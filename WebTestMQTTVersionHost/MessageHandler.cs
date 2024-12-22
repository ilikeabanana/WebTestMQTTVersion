using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Object= UnityEngine.Object;
using Random= UnityEngine.Random;

namespace WebTestMQTTVersionHost
{
    internal class MessageHandler : MonoBehaviour
    {
        public void HandleMessage(MessageDataJson messageData)
        {
            if(messageData.Value == "infinite")
            {
                messageData.Value = Mathf.Infinity.ToString();
            }
            else if(messageData.Value == "-infinite")
            {
                messageData.Value = Mathf.NegativeInfinity.ToString();
            }
            WebTestMQTTVersionHostPlugin.Log.LogInfo("Changing shit");
            if (messageData.Type == "text")
            {
                WebTestMQTTVersionHostPlugin.Log.LogInfo("Changing text");
                StartCoroutine(waitalil(messageData));
            }
            if(messageData.Type == "setting")
            {
                MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has changed {messageData.Message} to {messageData.Value}");
                WebTestMQTTVersionHostPlugin.Log.LogInfo("Changing setting");
                HandleSettingData(messageData);
            }
            if(messageData.Type == "graphics")
            {
                MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has changed {messageData.Message} to {messageData.Value}");
                WebTestMQTTVersionHostPlugin.Log.LogInfo("Changing graphics");
                WebTestMQTTVersionHostPlugin.Log.LogInfo(MonoSingleton<GraphicsOptions>.Instance == null ? "fucking" : "work");
                DebugGraphicsOptionsStatus();
                WebTestMQTTVersionHostPlugin.Log.LogInfo("graphicssssss");
                InvokeGraphicsOptionsMethod(messageData.Message, messageData.Value);
            }
            if (messageData.Type == "file")
            {
                WebTestMQTTVersionHostPlugin.Log.LogInfo("It is a file");
                HandleFileData(messageData);
            }
            if(messageData.Type == "Control")
            {
                WebTestMQTTVersionHostPlugin.Log.LogInfo("Changing control");
                HandleControlSettingData(messageData);
            }
            if(messageData.Type == "WeaponToggle")
            {
                MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has put weapon {messageData.Message} to {messageData.Value}");
                ChangeWeapon(messageData);
            }
        }
        public void ChangeWeapon(MessageDataJson messageData)
        {
            bool onOrOff = bool.Parse(messageData.Value);
            MonoSingleton<PrefsManager>.Instance.SetInt("weapon." + messageData.Message, onOrOff ? 1 : 0);
            StartCoroutine(WaitALil());
        }
        IEnumerator WaitALil()
        {

            yield return new WaitForSeconds(0.1f);
            MonoSingleton<GunSetter>.Instance.ResetWeapons();
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

            //Wondering why im using variables? Go to WebTestMQTTVersionHostPlugin Line 39
            WebTestMQTTVersionHostPlugin.Path = JsonConvert.DeserializeObject<JsonBindingMQTT>(controlData.Value);
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
            WebTestMQTTVersionHostPlugin.Log.LogInfo("Turning Base64 into bytes");
            byte[] bytes = FileHandler.Base64ToFile(messageData.Value);
            WebTestMQTTVersionHostPlugin.Log.LogInfo("Converted Base64 into bytes!");
            FileHandler.ProcessFile(bytes, messageData.FileName);
        }
        IEnumerator waitalil(MessageDataJson messageData)
        {
            yield return new WaitForSeconds(0.1f);
            HandleTextData(messageData);
        }
        public void HandleTextData(MessageDataJson messageData)
        {
            string command = messageData.Message;
            switch (command)
            {
                case "DupeEnemies":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has duped enemies");
                    foreach (var item in FindObjectsOfType<EnemyIdentifier>())
                    {
                        Instantiate(item, item.transform.position, Quaternion.identity);
                    }
                    break;
                case "BuffEnemies":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has buffed enemies");
                    foreach (var item in FindObjectsOfType<EnemyIdentifier>())
                    {
                        item.BuffAll();
                    }
                    break;
                case "DupeEnemy":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has duped a random enemy");
                    EnemyIdentifier enemy = FindObjectsOfType<EnemyIdentifier>().Where((x) => !x.dead).FirstOrDefault();
                    if(enemy != null)
                    {
                        Instantiate(enemy, enemy.transform.position, Quaternion.identity);
                    }
                    break;
                case "BuffEnemy":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has buffed random enemy");
                    EnemyIdentifier aenemy = FindObjectsOfType<EnemyIdentifier>().Where((x) => !x.dead).FirstOrDefault();
                    if (aenemy != null)
                    {
                        aenemy.BuffAll();
                    }
                    break;
                case "SendToLevel":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has send you to {messageData.Value}");
                    WebTestMQTTVersionHostPlugin.Log.LogInfo("Sending player to level " + messageData.Value);
                    HandleLeveling(messageData.Value);
                    break;
                case "SpawnPKEY":
                    WebTestMQTTVersionHostPlugin.Log.LogInfo("Spawning PKEY");
                    HandleSpawnPKEY(messageData.Value);
                    break;
                case "Yeet":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has yeeted you with force {messageData.Value}");
                    MonoSingleton<NewMovement>.Instance.Launch(GenerateRandomVector3(-180, -180, -180, 180, 180, 180), float.Parse(messageData.Value));
                    break;
                case "SetSpeed":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has put your speed to {messageData.Value}");
                    MonoSingleton<NewMovement>.Instance.walkSpeed = float.Parse(messageData.Value);
                    break;
                case "SetJump":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has put your jump power to {messageData.Value}");
                    MonoSingleton<NewMovement>.Instance.jumpPower = float.Parse(messageData.Value);
                    break;
                case "SetDamage":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has set your damage multiplier to {messageData.Value}");
                    WebTestMQTTVersionHostPlugin.Instance.DMGMultiPLR = float.Parse(messageData.Value);
                    break;
                case "SetDamageEnemy":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has set enemy damage multiplier to {messageData.Value}");
                    WebTestMQTTVersionHostPlugin.Instance.DMGMultiENEMY = float.Parse(messageData.Value);
                    break;
                case "Dmg":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has damaged you with {messageData.Value}");
                    MonoSingleton<NewMovement>.Instance.GetHurt((int)float.Parse(messageData.Value), false); 
                    break;
                case "Heal":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has healed you with {messageData.Value}");
                    MonoSingleton<NewMovement>.Instance.GetHealth((int)float.Parse(messageData.Value), false);
                    break;
                case "NoFist":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has removed your fists");
                    MonoSingleton<FistControl>.Instance.NoFist();
                    break;
                case "NoWeapons":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has removed your weapons");
                    MonoSingleton<GunControl>.Instance.NoWeapon();
                    break;
                case "YesFist":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has readded your fists");
                    MonoSingleton<FistControl>.Instance.YesFist();
                    break;
                case "YesWeapons":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has readded your weapons");
                    MonoSingleton<GunControl>.Instance.YesWeapon();
                    break;
                case "Time":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has set time to {messageData.Value}");
                    Time.timeScale = float.Parse(messageData.Value);
                    break;
                case "Scale":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has scaled everything by {messageData.Value}");
                    foreach (var item in FindAllObjectsInScene())
                    {
                        if (IsDescendantOf(item.transform, MonoSingleton<NewMovement>.instance.transform)) continue;
                        if (item.GetComponent<RectTransform>() != null) continue;
                        item.transform.localScale += new Vector3(float.Parse(messageData.Value), float.Parse(messageData.Value), float.Parse(messageData.Value));
                    }
                    break;
                case "Move":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has moved everything by {messageData.Value}");
                    foreach (var item in FindAllObjectsInScene())
                    {
                        if (IsDescendantOf(item.transform, MonoSingleton<NewMovement>.instance.transform)) continue;
                        if (item.GetComponent<RectTransform>() != null) continue;
                        item.transform.position += new Vector3(float.Parse(messageData.Value), float.Parse(messageData.Value), float.Parse(messageData.Value));
                    }
                    break;
                case "KillAll":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage($"{messageData.User} has killed all enemies");
                    foreach (var item in FindObjectsOfType<EnemyIdentifier>())
                    {
                        item.InstaKill();
                    }
                    break;
                case "SendHudMessage":
                    MonoSingleton<HudMessageReceiver>.instance.SendHudMessage(messageData.Value);
                    break;
            }
        }
        public static GameObject FindObjectEvenIfDisabled(string rootName, string objPath = null, int childNum = 0, bool useChildNum = false)
        {
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            var rootObject = System.Array.Find(rootObjects, obj => obj.name == rootName);

            if (rootObject == null)
                return null;

            var targetObject = rootObject;

            if (!string.IsNullOrEmpty(objPath))
                targetObject = rootObject.transform.Find(objPath)?.gameObject;

            if (targetObject != null && useChildNum && targetObject.transform.childCount > childNum)
                targetObject = targetObject.transform.GetChild(childNum).gameObject;

            return targetObject;
        }
        public static List<GameObject> FindAllObjectsInScene()
        {
            var allObjects = new List<GameObject>();
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                CollectAllChildren(root, allObjects);
            }

            return allObjects;
        }

        private static void CollectAllChildren(GameObject obj, List<GameObject> collectedObjects)
        {
            collectedObjects.Add(obj);

            foreach (Transform child in obj.transform)
            {
                CollectAllChildren(child.gameObject, collectedObjects);
            }
        }
        public static bool IsDescendantOf(Transform potentialChild, Transform potentialParent)
        {
            // Walk up the hierarchy from the potential child
            Transform current = potentialChild;

            while (current != null)
            {
                if (current == potentialParent)
                    return true; // Match found

                current = current.parent; // Move up the hierarchy
            }

            return false; // No match found
        }
        public static Vector3 GenerateRandomVector3(float minX, float maxX, float minY, float maxY, float minZ, float maxZ)
        {
            float x = Random.Range(minX, maxX);
            float y = Random.Range(minY, maxY);
            float z = Random.Range(minZ, maxZ);

            return new Vector3(x, y, z);
        }
        public void HandleSpawnPKEY(string jsonData)
        {
            // Deserialize the JSON data into PKEYValueData object
            PKEYValueData pkeyData = JsonConvert.DeserializeObject<PKEYValueData>(jsonData);

            if (pkeyData != null)
            {
                // Log the event
                WebTestMQTTVersionHostPlugin.Log.LogInfo($"Spawning PKEY {pkeyData.PKEY} at {pkeyData.Vector3Pos}, relative: {pkeyData.Relative}, amount: {pkeyData.amount}, delay: {pkeyData.delay}");

                // Start the spawning process
                StartCoroutine(SpawnPKEYAddressableCoroutine(pkeyData));
            }
            else
            {
                WebTestMQTTVersionHostPlugin.Log.LogWarning("Failed to deserialize PKEY data.");
            }
        }

        private IEnumerator SpawnPKEYAddressableCoroutine(PKEYValueData pkeyData)
        {
            Vector3 basePosition = new Vector3(pkeyData.Vector3Pos.x, pkeyData.Vector3Pos.y, pkeyData.Vector3Pos.z);

            for (int i = 0; i < pkeyData.amount; i++)
            {
                // Determine position based on the Relative flag
                Vector3 spawnPosition = pkeyData.Relative ? GetRelativePosition(basePosition) : basePosition;

                // Spawn the addressable
                SpawnAddressable(pkeyData.PKEY, spawnPosition);

                // Log the iteration
                WebTestMQTTVersionHostPlugin.Log.LogInfo($"Spawned instance {i + 1}/{pkeyData.amount} of {pkeyData.PKEY}");

                // Wait for the specified delay before spawning the next
                yield return new WaitForSeconds(pkeyData.delay);
            }
        }

        public Vector3 GetRelativePosition(Vector3 position)
        {
            position = MonoSingleton<NewMovement>.instance.transform.position + position;
            return position; // Placeholder logic
        }

        public void SpawnAddressable(string pkey, Vector3 position)
        {
            // Log the spawn event for debugging
            WebTestMQTTVersionHostPlugin.Log.LogInfo($"Spawning addressable {pkey} at {position}");

            // Load the addressable asset asynchronously using the PKEY
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(pkey);

            // Register a completed callback to handle the asset when it's loaded
            handle.Completed += (op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    // Instantiate the loaded addressable at the given position
                    GameObject spawnedObject = Instantiate(op.Result, position, Quaternion.identity);
                    WebTestMQTTVersionHostPlugin.Log.LogInfo($"Successfully spawned {pkey} at {position}");
                }
                else
                {
                    // Handle any loading errors here
                    WebTestMQTTVersionHostPlugin.Log.LogError($"Failed to load addressable {pkey}. Error: {op.OperationException?.Message}");
                }
            };
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