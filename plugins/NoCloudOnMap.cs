using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace NoMapClouds
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.Nubydy.nomapclouds";
        public const string PluginName = "No Clouds On Map";
        public const string PluginVersion = "1.0.0";
        internal static ManualLogSource Log;
        private Harmony harmony;
        private void Awake()
        {
            Log = Logger;

            harmony = new Harmony(PluginGUID);
            harmony.PatchAll();

            Log.LogInfo("No Clouds On Map loaded.");
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }
    }

    [HarmonyPatch(typeof(Minimap), "Start")]
    public static class MinimapStartPatch
    {
        private static Texture2D transparentTexture;

        private static Texture2D GetTransparentTexture()
        {
            // Makes the transparent texture
            if (transparentTexture == null)
            {
                transparentTexture = new Texture2D(
                    1,
                    1,
                    TextureFormat.RGBA32,
                    false
                );
                transparentTexture.name = "NoMapClouds_Transparent";

                transparentTexture.SetPixel(
                    0,
                    0,
                    new Color(0f, 0f, 0f, 0f)
                );
                transparentTexture.Apply(false, true);
            }
            return transparentTexture;
        }
        private static Material GetShader(Minimap minimap, string fieldName)
        {
            try
            {
                FieldInfo field = AccessTools.Field(
                    typeof(Minimap),
                    fieldName
                );

                if (field == null)
                {
                    Plugin.Log.LogWarning(
                        $"Could not find Minimap field: {fieldName}"
                    );
                    return null;
                }

                return field.GetValue(minimap) as Material;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(
                    $"Error accessing {fieldName}: {ex}"
                );

                return null;
            }
        }
        private static void RemoveClouds(Minimap minimap)
        {
            if (minimap == null)
                return;

            Texture2D transparent = GetTransparentTexture();
            // Small minimap
            Material smallShader = GetShader(
                minimap,
                "m_mapSmallShader"
            );

            if (smallShader != null)
            {
                smallShader.SetTexture(
                    "_CloudTex",
                    transparent
                );

                Plugin.Log.LogInfo(
                    "Clouds removed from small minimap."
                );
            }

            // Large map
            Material largeShader = GetShader(
                minimap,
                "m_mapLargeShader"
            );

            if (largeShader != null)
            {
                largeShader.SetTexture(
                    "_CloudTex",
                    transparent
                );

                Plugin.Log.LogInfo(
                    "Clouds removed from large map."
                );
            }
        }

        public static void Postfix(Minimap __instance)
        {
            RemoveClouds(__instance);

            __instance.StartCoroutine(
                RemoveCloudsDelayed(__instance)
            );
        }

        private static IEnumerator RemoveCloudsDelayed(Minimap minimap)
        {
            yield return null;
            RemoveClouds(minimap);

            yield return new WaitForEndOfFrame();
            RemoveClouds(minimap);
            yield return new WaitForSeconds(1f);
            RemoveClouds(minimap);
        }
    }
}