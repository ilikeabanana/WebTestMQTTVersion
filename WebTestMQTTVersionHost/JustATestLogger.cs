using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

namespace WebTestMQTTVersionHost
{
    internal class JustATestLogger : MonoBehaviour
    {

        // Token: 0x0600034C RID: 844 RVA: 0x000186D4 File Offset: 0x000168D4
        private void Awake()
        {
            this.inman = MonoSingleton<InputManager>.Instance;
            this.opm = MonoSingleton<OptionsManager>.Instance;
        }

        // Token: 0x0600034D RID: 845 RVA: 0x00018728 File Offset: 0x00016928
        private void Start()
        {
            bool @bool = MonoSingleton<PrefsManager>.Instance.GetBool("scrollVariations", false);
            bool bool2 = MonoSingleton<PrefsManager>.Instance.GetBool("scrollWeapons", false);
        }

        // Token: 0x0600034E RID: 846 RVA: 0x000187C0 File Offset: 0x000169C0
        /*
        private void OnEnable()
        {
            this.Rebuild(MonoSingleton<InputManager>.Instance.InputSource.Actions.KeyboardMouseScheme);
            InputManager instance = MonoSingleton<InputManager>.Instance;
            instance.actionModified = (Action<InputAction>)Delegate.Combine(instance.actionModified, new Action<InputAction>(this.OnActionChanged));
        }

        // Token: 0x0600034F RID: 847 RVA: 0x00018810 File Offset: 0x00016A10
        private void OnDisable()
        {
            if (MonoSingleton<InputManager>.Instance)
            {
                InputManager instance = MonoSingleton<InputManager>.Instance;
                instance.actionModified = (Action<InputAction>)Delegate.Remove(instance.actionModified, new Action<InputAction>(this.OnActionChanged));
            }
        }

        // Token: 0x06000350 RID: 848 RVA: 0x000188AC File Offset: 0x00016AAC
        public void OnActionChanged(InputAction action)
        {
            this.Rebuild(MonoSingleton<InputManager>.Instance.InputSource.Actions.KeyboardMouseScheme);
        }

        // Token: 0x06000351 RID: 849 RVA: 0x000188C8 File Offset: 0x00016AC8
        public void ResetToDefault()
        {
            this.inman.ResetToDefault();
        }

        // Token: 0x06000352 RID: 850 RVA: 0x000188D8 File Offset: 0x00016AD8
        private void Rebuild(InputControlScheme controlScheme)
        {
            MonoSingleton<InputManager>.Instance.InputSource.ValidateBindings(MonoSingleton<InputManager>.Instance.InputSource.Actions.KeyboardMouseScheme);

            InputActionMap[] array = new InputActionMap[]
            {
            this.inman.InputSource.Actions.Movement,
            this.inman.InputSource.Actions.Weapon,
            this.inman.InputSource.Actions.Fist,
            this.inman.InputSource.Actions.HUD
            };
            foreach (InputActionMap inputActionMap in array)
            {
                foreach (InputAction inputAction in inputActionMap)
                {
                    if ((!(inputAction.expectedControlType != "Button") || !(inputAction.expectedControlType != "Vector2")) && inputAction != this.inman.InputSource.Look.Action && inputAction != this.inman.InputSource.WheelLook.Action)
                    {
                        bool flag = true;
                        Debug.Log(inputAction.name);
                    }
                }
            }
        }*/
        public void ChangeKey(string actionName, JsonBindingMQTT path)
        {
            Debug.Log(actionName + " binding path: " + path.path);
            InputManager inputManager = MonoSingleton<InputManager>.Instance;

            // Dispose of the existing button listener to prevent potential conflicts
            if (inputManager.anyButtonListener != null)
            {
                inputManager.anyButtonListener.Dispose();
            }

            inputManager.InputSource.Disable();

            // Find the action
            InputAction targetAction = inputManager.InputSource.Actions.FindAction(actionName);
            if (targetAction != null)
            {
                // Wipe the action for the Keyboard & Mouse scheme
                targetAction.WipeAction(inputManager.InputSource.Actions.KeyboardMouseScheme.bindingGroup);

                // Handle composite and single inputs differently
                if (path.isComposite && path.compositePath != null && path.compositePath.Length > 0)
                {
                    // Create a composite binding
                    var compositeBinding = targetAction.AddCompositeBinding("Dpad");

                    string[] directions = new[] { "Up", "Down", "Left", "Right" };
                    for (int i = 0; i < Mathf.Min(path.compositePath.Length, directions.Length); i++)
                    {
                        if (!string.IsNullOrEmpty(path.compositePath[i]))
                        {
                            compositeBinding.With(directions[i], path.compositePath[i]);
                        }
                    }
                }
                else
                {
                    // For single inputs, add the binding directly
                    targetAction.AddBinding(path.path);
                }
            }

            // Re-enable the input source
            inputManager.InputSource.Enable();

            // Reestablish the button listener
            inputManager.anyButtonListener = InputManager.onAnyInput.Subscribe(InputManager.ButtonPressListener.Instance);
        }
        // Token: 0x06000353 RID: 851 RVA: 0x00018BF4 File Offset: 0x00016DF4
        private void LateUpdate()
        {
        }

        // Token: 0x06000354 RID: 852 RVA: 0x00018C2C File Offset: 0x00016E2C
        public void ScrollOn(bool stuff)
        {
            if (this.inman == null)
            {
                this.inman = MonoSingleton<InputManager>.Instance;
            }
            if (stuff)
            {
                MonoSingleton<PrefsManager>.Instance.SetBool("scrollEnabled", true);
                this.inman.ScrOn = true;
                return;
            }
            MonoSingleton<PrefsManager>.Instance.SetBool("scrollEnabled", false);
            this.inman.ScrOn = false;
        }

        // Token: 0x06000355 RID: 853 RVA: 0x00018C90 File Offset: 0x00016E90
        public void ScrollVariations(int stuff)
        {
            if (this.inman == null)
            {
                this.inman = MonoSingleton<InputManager>.Instance;
            }
            if (stuff == 0)
            {
                MonoSingleton<PrefsManager>.Instance.SetBool("scrollWeapons", true);
                MonoSingleton<PrefsManager>.Instance.SetBool("scrollVariations", false);
                this.inman.ScrWep = true;
                this.inman.ScrVar = false;
                return;
            }
            if (stuff == 1)
            {
                MonoSingleton<PrefsManager>.Instance.SetBool("scrollWeapons", false);
                MonoSingleton<PrefsManager>.Instance.SetBool("scrollVariations", true);
                this.inman.ScrWep = false;
                this.inman.ScrVar = true;
                return;
            }
            MonoSingleton<PrefsManager>.Instance.SetBool("scrollWeapons", true);
            MonoSingleton<PrefsManager>.Instance.SetBool("scrollVariations", true);
            this.inman.ScrWep = true;
            this.inman.ScrVar = true;
        }

        // Token: 0x06000356 RID: 854 RVA: 0x00018D68 File Offset: 0x00016F68
        public void ScrollReverse(bool stuff)
        {
            if (this.inman == null)
            {
                this.inman = MonoSingleton<InputManager>.Instance;
            }
            if (stuff)
            {
                MonoSingleton<PrefsManager>.Instance.SetBool("scrollReversed", true);
                this.inman.ScrRev = true;
                return;
            }
            MonoSingleton<PrefsManager>.Instance.SetBool("scrollReversed", false);
            this.inman.ScrRev = false;
        }

        // Token: 0x04000529 RID: 1321
        private InputManager inman;

        // Token: 0x0400052A RID: 1322
        [HideInInspector]
        public OptionsManager opm;

        // Token: 0x0400052B RID: 1323
        public List<ActionDisplayConfig> actionConfig;

    }
}
public class JsonBindingMQTT
{
    public string path;
    public bool isComposite;
    public string[] compositePath;
}