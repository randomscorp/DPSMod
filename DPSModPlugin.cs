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
    [BepInPlugin("io.github.randomscorp.dpsmod","DPS Mod","1.1")]
    public partial class DPSModPlugin : BaseUnityPlugin
    {
        internal static GameObject dpsCanvas;
        public static ConfigEntry<float> timeToReset;
        public static ConfigEntry<float> timeBetweenUpdates;
        public static ConfigEntry<int> textFontSize;
        public static ConfigEntry<TextAnchor> alignment;
        public static DPSDisplay displayMonoB;
        public static ConfigEntry<bool> showTimeInComboat;

        private void Awake()
        {
            timeToReset = Config.Bind("Gameplay","Time to reset",5f,"Time in seconds to reset the counters after doing no damage");
            timeBetweenUpdates = Config.Bind("Gameplay","Time to update",1f,"Time to update the DPS counter");
            alignment = Config.Bind("UI","Text Position", TextAnchor.UpperRight,"UI position, updates in real time");
                alignment.SettingChanged += (_, _) => { DPSModPlugin.displayMonoB.text.alignment = DPSModPlugin.alignment.Value; };
            textFontSize= Config.Bind("UI", "Text Size", 20, "The font size used to render the display");
                textFontSize.SettingChanged += (_, _) => { DPSModPlugin.displayMonoB.text.fontSize = DPSModPlugin.textFontSize.Value; };

            showTimeInComboat = Config.Bind("UI", "Show Time in combat counter?", true);

            dpsCanvas = new GameObject() { name="DPS Canvas"};
                GameObject.DontDestroyOnLoad(dpsCanvas);
                dpsCanvas.layer = ((int)PhysLayers.UI);
                dpsCanvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                dpsCanvas.AddComponent<RectTransform>();

            var text = dpsCanvas.AddComponent<Text>();
                text.fontSize = textFontSize.Value;
                text.font = Font.GetDefault();
                text.text = "PLACEHOLDER";
                text.alignment = alignment.Value;
                DPSModPlugin.displayMonoB = dpsCanvas.AddComponent<DPSDisplay>();

            new Harmony("io.github.dpsmod").PatchAll();
        }

        [HarmonyPatch(typeof(HealthManager), nameof(HealthManager.TakeDamage))]
        [HarmonyPrefix]
        private static void SaveHealthBefore0(HealthManager __instance, HitInstance hitInstance)
        {
            DPSModPlugin.displayMonoB.hpBeforeEachHit = __instance.hp;
        }

        [HarmonyPatch(typeof(HealthManager), nameof(HealthManager.TakeDamage))]
        [HarmonyPostfix]
        private static void DamageDealt(HealthManager __instance, HitInstance hitInstance)
        {
            var display = DPSModPlugin.displayMonoB;
            double damageDone = DPSModPlugin.displayMonoB.hpBeforeEachHit - __instance.hp;
         
            DPSModPlugin.displayMonoB.totalDamageDone += damageDone >= 0 ? damageDone: DPSModPlugin.displayMonoB.hpBeforeEachHit;

            display.timeSinceLastDamage = 0;
        }
    }

    public class DPSDisplay : MonoBehaviour
    {
        public float timeSinceLastDamage;
        private float timeDoingDamage = 0f;

        public float timeSinceLastUpdate = 0f;

        public double hpBeforeEachHit = 0f;
        public double totalDamageDone = 0f;

        public Text text;

        void Awake()
        {
            this.text = this.gameObject.GetComponent<Text>();
            timeSinceLastDamage = DPSModPlugin.timeToReset.Value;
        }

        void FixedUpdate()
        {
            if (timeSinceLastDamage >= DPSModPlugin.timeToReset.Value)
            {
                totalDamageDone= 0f;
                timeDoingDamage = 0;
            }
            else
            {
                timeDoingDamage += Time.deltaTime;
            }
            timeSinceLastDamage += Time.deltaTime;
            timeSinceLastUpdate +=Time.deltaTime;

            if(timeSinceLastUpdate > DPSModPlugin.timeBetweenUpdates.Value)
            {
                text.text = $"DPS: {(timeDoingDamage == 0 ? "0" : (totalDamageDone / timeDoingDamage).ToString("F2"))}";
                timeSinceLastUpdate=0;
            }
            else
            {
                text.text = text.text.Split(System.Environment.NewLine)[0];
            }
            text.text += System.Environment.NewLine +
                    $"Total damage: {totalDamageDone}";
                if (DPSModPlugin.showTimeInComboat.Value) text.text += System.Environment.NewLine + $"Time in combat: {timeDoingDamage.ToString("F2")}";
        }
    }
}