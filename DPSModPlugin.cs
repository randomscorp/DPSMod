using BepInEx;
using BepInEx.Configuration;
using GlobalEnums;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using BepInEx.Logging;

namespace DPSMod
{
    [Harmony]
    [BepInPlugin("io.github.randomscorp.dpsmod","DPS Mod","1.0")]
    public partial class DPSModPlugin : BaseUnityPlugin
    {

        internal static GameObject GO;
        public static ConfigEntry<float> timeToReset;
        public static ConfigEntry<float> timeBetweenUpdates;
        public static ConfigEntry<TextAnchor> alignment;
        public static ManualLogSource logger;
        private void Awake()
        {
            DPSModPlugin.timeToReset = Config.Bind("Gameplay","Time in seconds to reset the dps counter",5f);
            DPSModPlugin.timeBetweenUpdates = Config.Bind("Gameplay","Time in seconds to update the display, set 0 for real time",1f);
            DPSModPlugin.alignment = Config.Bind("UI","Text position", TextAnchor.UpperRight);

            GO = new GameObject() { name="DPS Display"};
            GO.layer = ((int)PhysLayers.UI);
            
            GO.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            GO.AddComponent<CanvasScaler>();
            GameObject.DontDestroyOnLoad(GO);
            
            GO.AddComponent<CanvasRenderer>();

            var text = GO.AddComponent<Text>();
            text.fontSize = 30;
            text.font = Font.GetDefault();
            text.text = "PLACEHOLDER";
            text.alignment = DPSModPlugin.alignment.Value;

            GO.AddComponent<DPSDisplay>();

            new Harmony("io.github.dpsmod").PatchAll();
        }

        [HarmonyPatch(typeof(HealthManager), nameof(HealthManager.TakeDamage))]
        [HarmonyPrefix]
        private static void SaveHealthBefore0(HealthManager __instance, HitInstance hitInstance)
        {
            DPSModPlugin.GO.GetComponent<DPSDisplay>().hp_before += __instance.hp;
        }

        [HarmonyPatch(typeof(HealthManager), nameof(HealthManager.TakeDamage))]
        [HarmonyPostfix]
        private static void DamageDealt(HealthManager __instance, HitInstance hitInstance)
        {
            var display = DPSModPlugin.GO.GetComponent<DPSDisplay>();
            display.hp_after += __instance.hp;
            display.timeSinceLastDamage = 0;
        }
    }

    internal class DPSDisplay : MonoBehaviour
    {
        //public float damageDone = 0f;
        public float timeSinceLastDamage = 0f;
        private float timeDoingDamage = 0f;

        public float timeSinceLastUpdate = 0f;

        public double hp_before = 0f;
        public double hp_after = 0f;

        private Text text;

        void Awake()
        {
            this.text = this.gameObject.GetComponent<Text>();
        }

        void FixedUpdate()
        {
            if (timeSinceLastDamage >= DPSModPlugin.timeToReset.Value)
            {
                hp_before = 0;
                hp_after = 0;
                timeDoingDamage = 0;
            }
            else
            {
                timeDoingDamage += Time.deltaTime;
            }
            timeSinceLastDamage += Time.deltaTime;
            timeSinceLastUpdate +=Time.deltaTime;
            if (timeSinceLastUpdate > DPSModPlugin.timeBetweenUpdates.Value)
            {
                text.text ="DPS: " + ( timeDoingDamage ==0?"0": ((hp_before -hp_after) / timeDoingDamage).ToString("F2"));
                timeSinceLastUpdate = 0;
            }
        }
    }
}