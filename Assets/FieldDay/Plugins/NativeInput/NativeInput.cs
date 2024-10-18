#if !UNITY_EDITOR && UNITY_WEBGL
#define USE_JSLIB
#endif // !UNITY_EDITOR && UNITY_WEBGL

using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

using AOT;
using UnityEngine.Scripting;
using System.Collections.Generic;

namespace NativeUtils
{
    static public class NativeInput {
        public delegate void PositionCallback(float normalizedX, float normalizedY);
        public delegate void KeyCallback(KeyCode keyCode);

        private delegate void NativeKeyCallback(int keyCode);

        #if USE_JSLIB

        [DllImport("__Internal")]
        static private extern void NativeWebInput_RegisterClick(PositionCallback callbackDown, PositionCallback callbackUp);

        [DllImport("__Internal")]
        static private extern void NativeWebInput_DeregisterClick();

        #endif // USE_JSLIB

        [MonoPInvokeCallback(typeof(PositionCallback)), Preserve]
        static private void BridgeClickDownCallback(float x, float y) {
            if (OnMouseDown != null) {
                OnMouseDown(x, y);
            }
            if (EventSystemIntegration.System != null) {
                EventSystemIntegration.OnPointerDown(EventSystemIntegration.System, EventSystemIntegration.EventData, x, y);
            }
        }

        [MonoPInvokeCallback(typeof(PositionCallback)), Preserve]
        static private void BridgeClickUpCallback(float x, float y) {
            if (OnMouseUp != null) {
                OnMouseUp(x, y);
            }
            if (EventSystemIntegration.System != null) {
                EventSystemIntegration.OnPointerUp(EventSystemIntegration.System, EventSystemIntegration.EventData, x, y);
            }
        }

        [MonoPInvokeCallback(typeof(NativeKeyCallback)), Preserve]
        static private void BridgeKeyCallback(int keyCode) {
            if (OnKeyDown != null) {
                OnKeyDown((KeyCode) keyCode);
            }
        }

        static public event PositionCallback OnMouseDown;
        static public event PositionCallback OnMouseUp;
        static public event KeyCallback OnKeyDown;

        static public void Initialize() {
            #if USE_JSLIB
            NativeWebInput_RegisterClick(BridgeClickDownCallback, BridgeClickUpCallback);
            #else
            if (s_InstantiatedCallback == null) {
                GameObject go = new GameObject("[NativeWebInputMock]");
                go.hideFlags = HideFlags.DontSave;
                GameObject.DontDestroyOnLoad(go);
                s_InstantiatedCallback = go.AddComponent<NativeInputMockCallback>();
            }
            #endif // USE_JSLIB
        }

        static public void Shutdown() {
            #if USE_JSLIB
            NativeWebInput_DeregisterClick();
            #else
            if (s_InstantiatedCallback != null) {
                GameObject.Destroy(s_InstantiatedCallback.gameObject);
                s_InstantiatedCallback = null;
            }
            #endif // USE_JSLIB
        }

        #if !USE_JSLIB

        static private NativeInputMockCallback s_InstantiatedCallback;

        private sealed class NativeInputMockCallback : MonoBehaviour {
            private void Awake() {
                useGUILayout = false;
            }

            private void LateUpdate() {
                if (Input.GetMouseButtonDown(0)) {
                    Vector2 mousePos = Input.mousePosition;
                    BridgeClickDownCallback(mousePos.x / Screen.width, mousePos.y / Screen.height);
                } else if (Input.GetMouseButtonUp(0)) {
                    Vector2 mousePos = Input.mousePosition;
                    BridgeClickUpCallback(mousePos.x / Screen.width, mousePos.y / Screen.height);
                }
            }

            private void OnGUI() {
                Event evt = Event.current;
                if (evt.type == EventType.KeyDown) {
                    if (OnKeyDown != null) {
                        OnKeyDown(evt.keyCode);
                    }
                }
            }
        }

        #endif // !USE_JSLIB

        #region Event System

        /// <summary>
        /// Sets the event system through which native pointer events can be funneled.
        /// </summary>
        static public void SetEventSystem(EventSystem system) {
            EventSystemIntegration.System = system;
            EventSystemIntegration.EventData = system ? new PointerEventData(system) : null;
        }

        /// <summary>
        /// Enables/disables native pointer event funneling.
        /// </summary>
        static public void SetEventSystemEnabled(bool enabled) {
            EventSystemIntegration.Enabled = enabled;
        }

        static internal class EventSystemIntegration {

            static public EventSystem System;
            static public PointerEventData EventData;
            static private List<RaycastResult> s_RaycastCache = new List<RaycastResult>(16);
            static public bool Enabled = true;

            static public void OnPointerDown(EventSystem evtSystem, PointerEventData ptrData, float x, float y) {
                if (!Enabled) {
                    return;
                }

                ptrData.button = PointerEventData.InputButton.Left;
                GetNativePointerData(evtSystem, ptrData, x, y);
                GameObject press = ExecuteEvents.ExecuteHierarchy(ptrData.pointerCurrentRaycast.gameObject, ptrData, NativeMouseDownHandler);
                ptrData.pointerPress = press;
            }

            static public void OnPointerUp(EventSystem evtSystem, PointerEventData ptrData, float x, float y) {
                if (!Enabled) {
                    return;
                }

                GetNativePointerData(evtSystem, ptrData, x, y);
                GameObject pressed = ptrData.pointerPress;
                if (pressed != null) {
                    ExecuteEvents.Execute(pressed, ptrData, NativeMouseUpHandler);
                }

                GameObject released = ExecuteEvents.GetEventHandler<INativePointerClickHandler>(ptrData.pointerCurrentRaycast.gameObject);
                if (pressed != null && released == pressed) {
                    ptrData.pointerClick = released;
                    ExecuteEvents.Execute(released, ptrData, NativeMouseClickHandler);
                }
            }

            static public void GetNativePointerData(EventSystem evtSystem, PointerEventData ptrData, float x, float y) {
                ptrData.position = new Vector2(x * Screen.width, y * Screen.height);
                evtSystem.RaycastAll(ptrData, s_RaycastCache);
                RaycastResult ptrOver = default;
                for (int i = 0; i < s_RaycastCache.Count; i++) {
                    if (s_RaycastCache[i].gameObject) {
                        ptrOver = s_RaycastCache[i];
                        break;
                    }
                }
                ptrData.pointerCurrentRaycast = ptrOver;
                s_RaycastCache.Clear();
            }

            static private readonly ExecuteEvents.EventFunction<INativePointerDownHandler> NativeMouseDownHandler = (INativePointerDownHandler handler, BaseEventData evtData) => {
                handler.OnNativePointerDown(ExecuteEvents.ValidateEventData<PointerEventData>(evtData));
            };

            static private readonly ExecuteEvents.EventFunction<INativePointerDownHandler> NativeMouseUpHandler = (INativePointerDownHandler handler, BaseEventData evtData) => {
                handler.OnNativePointerDown(ExecuteEvents.ValidateEventData<PointerEventData>(evtData));
            };

            static private readonly ExecuteEvents.EventFunction<INativePointerClickHandler> NativeMouseClickHandler = (INativePointerClickHandler handler, BaseEventData evtData) => {
                handler.OnNativePointerClick(ExecuteEvents.ValidateEventData<PointerEventData>(evtData));
            };

            #endregion // Event System
        }
    }
}