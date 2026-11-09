using System;
using System.Collections;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using UnityEngine;

namespace SkeegMainMenu;

/// <summary>
///  the mod basically works by using the truck script as an entry point to decide if the level should be changed
///  if it should be changed it disables the truck and starts a coroutine that waits until the menu level is declared generated
///  the coroutine uses a passed startroom item to determine where it should make a skeeg and disable the lobby landscape
///  then it just uses reflection to set the ambient color and music 
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class SkeegMainMenu : BaseUnityPlugin {
    public static SkeegMainMenu Instance; 
    internal new static ManualLogSource Logger;
    public Harmony Harmoneeg;
    public AssetBundle eegs;
    public GameObject skeegAsset;
    public AudioClip eegNoises;

    public string assetBundleName = "skeeg";
    
    public string gameobjectName = "Eegvil skeeg.";
    public Vector3 assetPosition = Vector3.zero;

    public float cameraFov = 95;
    public Vector3 playerParentPosition = Vector3.zero;
    public Vector3 playerParentRotation = Vector3.zero;
    
    public Vector3 playerPosition = new(0f, 1.4f, -8f);
    public Vector3 playerEulerAngles = new(-25, 0, 0);
    
    public Color backgroundColor = Color.black;

    public float playerSpeenSpeed = -30f;
    public float skeegahedronSpeenSpeed = 45f;
    
    private void Awake() {
        if (Instance != null) {
            Destroy(this);
            return;
        }
        Instance = this;
        
        Logger = base.Logger;
        Logger.LogInfo($"Loading {MyPluginInfo.PLUGIN_GUID}.");
        Harmoneeg = new Harmony($"at.kibbyc.{MyPluginInfo.PLUGIN_GUID}");
        try {
            LoadAssetBundle(Path.Join(Path.GetDirectoryName(Info.Location), assetBundleName));
            LoadAssetsFromBundle();
        }
        catch (Exception e) {
            Logger.LogError("Asset loading failed, Canceling loading mod.");
            Logger.LogError(e);
            return;
        }
        
        Logger.LogInfo("Patching..");
        Harmoneeg.PatchAll(typeof(SkeegMainMenu));
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }


    private void LoadAssetBundle(string bundlePath) {
        Logger.LogInfo($"Loading AssetBundle from {bundlePath}..");
        eegs = AssetBundle.LoadFromFile(bundlePath);
        Logger.LogInfo("AssetBundle loaded!");
        
        Logger.LogInfo($"found {eegs.GetAllAssetNames().Length} assets");
        Logger.LogInfo("\n" + string.Join("\n", eegs.GetAllAssetNames()));
    }

    private void LoadAssetsFromBundle() {
        Logger.LogInfo("Loading assets");
        skeegAsset = eegs.LoadAsset<GameObject>("Skeeg");
        eegNoises = eegs.LoadAsset<AudioClip>("SkeegMusic");
        Logger.LogInfo("Loaded assets");
    }
    
    // pretty much the entrypoint for this entire operation
    // the script is only present on the truck of the main level 
    [HarmonyPatch(typeof(TruckMenuAnimated), nameof(TruckMenuAnimated.Start)), HarmonyPrefix] 
    static bool No____Dont___DoThat(TruckMenuAnimated __instance) {
        Instance.StartCoroutine(
            Instance.KeegRoutine(__instance.transform.parent.parent)
        );
        
        __instance.gameObject.SetActive(false);
        return false;
    }

    // disabled performance analytics because its only ran a single time
    // ReSharper disable Unity.PerformanceAnalysis
    IEnumerator KeegRoutine(Transform startRoom) { // repo devs pls fix 🥺🥺 why do menu worlds have levelpoints
        if (!LevelGenerator.Instance) {
            Logger.LogError("Level generator is null, cant safely disable lobby.");
            yield break;
        }
        
        // waits a frame until the level is done loading
        // done to avoid unintended behavior, more on that below 
        while (!LevelGenerator.Instance.Generated) yield return null; 
        
        InitializeSkeegs(startRoom);
    }

    // this is done after the levelgenerator has finished generating because otherwise it'd throw an error because it has no levelpoints
    // doing it before also caused issues with multiplayer lobbies not getting a skeeg.
    private static void InitializeSkeegs(Transform startRoom) {
        Logger.LogInfo("Initializing skeeg (and lobotomizing the truck animator)!!");
        
        if (!startRoom.gameObject.TryGetComponent<StartRoom>(out _))
            Logger.LogWarning("Startroom doesnt have startroom component");
        
        LoadSkeegModel(Instance.skeegAsset, startRoom);
        DoSkeegMusic(Instance.eegNoises);
        DoCameraOverrides();
        SetEnviornmentColor();
        // the lobby landscape has a script which restores from the halloween env on disable, this runs it
        startRoom.Find("Lobby Landscape").gameObject.SetActive(false);
    }
    
    private static void LoadSkeegModel(GameObject skeeger, Transform startRoom) {
        GameObject skeeg = Instantiate(skeeger, startRoom);
        skeeg.transform.position = Instance.assetPosition;
        skeeg.name = Instance.gameobjectName;
        Logger.LogInfo("Skeeg initialized.");
        TrySpinNestedSpeener(skeeg);
    }

    // try to apply a spinner to the rotating keegsahedron
    private static void TrySpinNestedSpeener(GameObject skeeg) {
        try {
            skeeg.transform.Find("Keedge").Find("Speener").gameObject.AddComponent<EegSpinner>().eegspinning = Instance.skeegahedronSpeenSpeed;
        }
        catch (Exception e) {
            Logger.LogWarning("You've changed the keeg assetbundle without keeping the spinner at \"asset/Keedge/Speener\"! this resulted in failing to put a eegspinner onto the speener element.");
            Logger.LogWarning(e);
        }
    }

    public static void DoCameraOverrides() {
        Logger.LogInfo("Setting camera info");

        if (!CameraNoPlayerTarget.instance) {
            Logger.LogError("There's no mock camera in the menu scene, somethings wrong! cant do camera overrides.");
            return;
        }
        CameraNoPlayerTarget target = CameraNoPlayerTarget.instance;
        
        target.GetComponent<Camera>().fieldOfView = Instance.cameraFov;
        
        Transform cameraTargetParent = target.transform.parent;
        cameraTargetParent.localPosition = Instance.playerParentPosition;
        cameraTargetParent.localEulerAngles = Instance.playerParentRotation;
        
        Transform cameraTargetTransform = target.transform;
        cameraTargetTransform.position = Instance.playerPosition;
        cameraTargetTransform.localEulerAngles = Instance.playerEulerAngles;
        
        // finally the cherry on the cake
        cameraTargetParent.gameObject.AddComponent<EegSpinner>().eegspinning = Instance.playerSpeenSpeed;
    }
    
    public static void SetEnviornmentColor() {
        Logger.LogInfo("Doing enviornment overrides.");
        if (!EnvironmentDirector.Instance) {
            Logger.LogError("Level generator instance was null, cant do environment overrides."); 
            return;
        }
        
        // this originally set the level colors in the levelgenerator
        // that needed to run before the level generation was finished or else it'd load the level with the old colors
        // so i just set the colors directly in the environment director
        
        // i should probably be checking if the environment director finished setting up here
        
        //LevelGenerator.Instance.Level.AmbientColor = Color.black;
        //LevelGenerator.Instance.Level.FogColor = Color.black;
        
        ref Color color = ref AccessTools.FieldRefAccess<EnvironmentDirector, Color>(nameof(EnvironmentDirector.FogColor))(EnvironmentDirector.Instance);
        color = Instance.backgroundColor; // Color.black;
    }
    
    private static void DoSkeegMusic(AudioClip skeegMusic = null) {
        skeegMusic ??= Instance.eegNoises;
        Logger.LogInfo("Setting skeeg music");
        if (!ConstantMusic.instance) {
            Logger.LogError("Constant music instance was null, cant do music overrides."); 
            return; 
        }
        
        // this originally set the audio in the levelgenerator.level
        // but i went with this approach because the levelgenerator level constantmusicpreset approach required you to set the audioclip on every preset and idk how many might be added later in the game
        // so i went with reflection to just set the clip and reload it directly.
        // ironically reflection here is the most compatible imo
        
        // set clip
        ref AudioClip clip = ref AccessTools.FieldRefAccess<ConstantMusic, AudioClip>(nameof(ConstantMusic.clip))(ConstantMusic.instance);
        clip = skeegMusic;
        
        // update music here
        ref bool setup = ref AccessTools.FieldRefAccess<ConstantMusic, bool>(nameof(ConstantMusic.setup))(ConstantMusic.instance);
        setup = false;
        
        // this makes constantmusic to set the clip to the audiosource in the update loop 
    }
    
    // stolen from RepoXR (how shameless) // disable the animation because there's a rotation offset specified
    [HarmonyPatch(typeof(CameraMainMenu), nameof(CameraMainMenu.Awake)), HarmonyPostfix]
    private static void DisableMainMenuAnimation(CameraMainMenu __instance) => __instance.introLerp = 1;
    
    [HarmonyPatch(typeof(MenuPageMain), nameof(MenuPageMain.ButtonSocialMarketing)), HarmonyPrefix]
    static bool No____Dont___DoThat3() { // dont ask what happened to dont do that 2
        Application.OpenURL("https://www.youtube.com/watch?v=wE4YL4Ehqhg");
        return false;
    }
}
