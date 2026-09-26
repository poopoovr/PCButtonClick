using BepInEx;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection;

namespace PCButtonClick
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);
        bool wasDown;

        void Update()
        {
            try
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                bool isDown = (GetAsyncKeyState(0x01) & 0x8000) != 0;
                bool clicked = isDown && !wasDown;
                wasDown = isDown;

                if (!clicked || Mouse.current == null) return;
                
                Camera cam = null;
                foreach (Camera c in Camera.allCameras)
                    if (c.isActiveAndEnabled && c.stereoTargetEye == StereoTargetEyeMask.None && c.targetTexture == null) { cam = c; break; }
                
                cam = cam ?? Camera.main ?? (Camera.allCamerasCount > 0 ? Camera.allCameras[0] : null);
                if (cam == null) return;

                Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
                ray.origin += ray.direction * 0.1f;

                RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, Physics.AllLayers, QueryTriggerInteraction.Collide);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider == null) continue;
                    MonoBehaviour[] scripts = hit.collider.GetComponentsInParent<MonoBehaviour>();
                    bool isBtn = false;

                    foreach (var s in scripts)
                        if (s != null && (s.GetType().Name.Contains("Button") || s.GetType().Name.Contains("Pressable"))) { isBtn = true; break; }

                    if (isBtn)
                    {
                        GameObject poopoovr = new GameObject("poopoovr");
                        poopoovr.transform.position = hit.point;
                        Collider col = poopoovr.AddComponent<SphereCollider>();
                        col.isTrigger = true;
                        poopoovr.AddComponent<Rigidbody>().isKinematic = true;
                        poopoovr.AddComponent<GorillaTriggerColliderHandIndicator>();

                        hit.collider.SendMessageUpwards("OnTriggerEnter", col, SendMessageOptions.DontRequireReceiver);
                        foreach (var s in scripts)
                            if (s != null) s.GetType().GetMethod("OnTriggerEnter", (BindingFlags)54)?.Invoke(s, new object[] { col });

                        Destroy(poopoovr, 0.1f);
                        break;
                    }
                }
            }
            catch { }
        }
    }
}
