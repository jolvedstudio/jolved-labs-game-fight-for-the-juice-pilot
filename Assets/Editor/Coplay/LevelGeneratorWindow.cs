using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// User-facing window for the 2D_Pack Corgi level generator.
/// Menu: Tools > Coplay > 2D_Pack Level Generator
///
/// Parameters are grouped into tabs (Floors, Props, Lighting, Effects, Collectibles)
/// so the growing option set stays organised.
/// </summary>
public class LevelGeneratorWindow : EditorWindow
{
    // ---- General ----
    int _seed = 20260612;

    // ---- Floors ----
    int _numberOfFloors = 3;
    bool _floorGaps = true;

    // ---- Props ----
    bool _pipes = true;

    // ---- Lighting ----
    bool _useRealLights = false;
    bool _lightIntensityVariation = true;
    bool _lightFlicker = false;
    float _globalLightIntensity = 0.55f;
    Color _pointLightColor = new Color(0.75f, 0.88f, 1f, 1f);
    Color _globalLightColor = new Color(0.7f, 0.78f, 0.95f, 1f);

    // ---- Effects ----
    bool _fog = false;
    float _fogIntensity = 0.5f;
    bool _particles = false;

    // ---- Collectibles ----
    bool _healthItems = false;
    bool _coins = false;

    // ---- Characters: Player ----
    int _playerIndex = 0;             // index into the Player catalog keys
    string[] _playerKeys;             // cached catalog key list

    // ---- Characters: Enemies ----
    bool _spawnEnemies = false;
    int _enemiesPerFloor = 2;
    // One toggle per catalog entry (key order from BuildCorgiLevel.EnemyCatalog).
    System.Collections.Generic.Dictionary<string, bool> _enemySelection;

    static readonly string[] Tabs = { "Floors", "Props", "Lighting", "Effects", "Collectibles", "Characters" };
    int _tab = 0;
    Vector2 _scroll;
    string _lastResult = "";

    [MenuItem("Tools/Coplay/2D_Pack Level Generator")]
    public static void Open()
    {
        var w = GetWindow<LevelGeneratorWindow>("Level Generator");
        w.minSize = new Vector2(400, 460);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Corgi 2D_Pack Level Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Seed (shared across all tabs).
        EditorGUILayout.BeginHorizontal();
        _seed = EditorGUILayout.IntField("Seed", _seed);
        if (GUILayout.Button("Randomize", GUILayout.Width(90)))
            _seed = Random.Range(1, int.MaxValue);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        _tab = GUILayout.Toolbar(_tab, Tabs);
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        switch (_tab)
        {
            case 0: DrawFloorsTab(); break;
            case 1: DrawPropsTab(); break;
            case 2: DrawLightingTab(); break;
            case 3: DrawEffectsTab(); break;
            case 4: DrawCollectiblesTab(); break;
            case 5: DrawCharactersTab(); break;
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Level", GUILayout.Height(34)))
            Generate();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(110));
        EditorGUILayout.TextArea(_lastResult, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    void DrawFloorsTab()
    {
        _numberOfFloors = EditorGUILayout.IntSlider(
            new GUIContent("Number Of Floors", "How many stacked floors to generate (2..6)."),
            _numberOfFloors, 2, 6);
        _floorGaps = EditorGUILayout.Toggle(
            new GUIContent("Floor Gaps", "Leave jumpable 1-tile gaps in the platform decks (decks only, never the background)."),
            _floorGaps);
        EditorGUILayout.HelpBox("The background is always fully filled (no voids). Decks get Platform End caps automatically.", MessageType.None);
    }

    void DrawPropsTab()
    {
        _pipes = EditorGUILayout.Toggle(
            new GUIContent("Pipes", "Build decorative pipe networks (horizontal + vertical runs, bends, valves, T-junctions)."),
            _pipes);
    }

    void DrawLightingTab()
    {
        _useRealLights = EditorGUILayout.Toggle(
            new GUIContent("Use Real 2D Lights",
                "OFF = fake lights baked into the sprites (Lit panels).\n" +
                "ON  = swap Lit panels for UnLit ones and add URP Light2D lights."),
            _useRealLights);
        _lightIntensityVariation = EditorGUILayout.Toggle(
            new GUIContent("Light Intensity Variation", "Randomize each light's brightness a bit so floors aren't uniformly lit."),
            _lightIntensityVariation);
        _lightFlicker = EditorGUILayout.Toggle(
            new GUIContent("Light Flicker", "SOME lights flicker (not all) for an unstable industrial feel."),
            _lightFlicker);
        _pointLightColor = EditorGUILayout.ColorField(
            new GUIContent("Point Light Color", "Colour of the per-floor point lights (and fake glow tint)."),
            _pointLightColor);
        EditorGUILayout.Space();
        _globalLightIntensity = EditorGUILayout.Slider(
            new GUIContent("Global Light Intensity", "Ambient fill for Real-Lights mode. 0 = no global fill (only point lights)."),
            _globalLightIntensity, 0f, 1.5f);
        _globalLightColor = EditorGUILayout.ColorField(
            new GUIContent("Global Light Color", "Colour of the ambient global light (Real-Lights mode)."),
            _globalLightColor);
    }

    void DrawEffectsTab()
    {
        _fog = EditorGUILayout.Toggle(
            new GUIContent("Fog", "Soft drifting haze in front of the background for depth."),
            _fog);
        using (new EditorGUI.DisabledScope(!_fog))
        {
            _fogIntensity = EditorGUILayout.Slider(
                new GUIContent("Fog Intensity", "0 = thin/sparse haze, 1 = dense/opaque haze."),
                _fogIntensity, 0f, 1f);
        }
        EditorGUILayout.Space();
        _particles = EditorGUILayout.Toggle(
            new GUIContent("Particles", "Ambient steam/spark emitters along the floors."),
            _particles);
    }

    void DrawCollectiblesTab()
    {
        _healthItems = EditorGUILayout.Toggle(
            new GUIContent("Health Items", "Place Corgi stimpack health pickups along the floors."),
            _healthItems);
        _coins = EditorGUILayout.Toggle(
            new GUIContent("Coins / Power Units", "Place Corgi coin pickups in arcs/rows along the floors."),
            _coins);
        EditorGUILayout.HelpBox("Collectibles use Corgi Engine pickable prefabs (PickableItem), so pickup/feedback works at runtime.", MessageType.None);
    }

    void DrawCharactersTab()
    {
        // ---- Player picklist (single-select from the Player catalog) ----
        if (_playerKeys == null)
            _playerKeys = new System.Collections.Generic.List<string>(BuildCorgiLevel.PlayerCatalog.Keys).ToArray();

        EditorGUILayout.LabelField("Player", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("The player character the LevelManager spawns. Prefabs from Assets/Game/Prefabs/Players.", MessageType.None);
        _playerIndex = EditorGUILayout.Popup(
            new GUIContent("Player Character", "Which player prefab to spawn at the level start."),
            _playerIndex, _playerKeys);

        EditorGUILayout.Space();

        // ---- Enemies / NPCs (multi-select) ----
        // Lazily build the selection map from the catalog (keeps key order).
        if (_enemySelection == null)
        {
            _enemySelection = new System.Collections.Generic.Dictionary<string, bool>();
            foreach (var kv in BuildCorgiLevel.EnemyCatalog)
                _enemySelection[kv.Key] = true; // default: all selected
        }

        EditorGUILayout.LabelField("Enemies & NPCs", EditorStyles.boldLabel);
        _spawnEnemies = EditorGUILayout.Toggle(
            new GUIContent("Spawn Enemies", "Master ON/OFF for the enemy / NPC pass."),
            _spawnEnemies);

        using (new EditorGUI.DisabledScope(!_spawnEnemies))
        {
            EditorGUILayout.HelpBox("Multi-select from the Corgi-wired prefabs in Assets/Game/Prefabs/Enemies. " +
                                    "If none are ticked, ALL types are used. The Dude NPC has a talk/dialogue zone " +
                                    "(walk up + press Interact).", MessageType.None);

            // Multi-select picklist (one toggle per available enemy/NPC type).
            var keys = new System.Collections.Generic.List<string>(_enemySelection.Keys);
            foreach (var key in keys)
                _enemySelection[key] = EditorGUILayout.ToggleLeft(key, _enemySelection[key]);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(90)))
                foreach (var k in keys) _enemySelection[k] = true;
            if (GUILayout.Button("Select None", GUILayout.Width(90)))
                foreach (var k in keys) _enemySelection[k] = false;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            _enemiesPerFloor = EditorGUILayout.IntSlider(
                new GUIContent("Enemies Per Floor", "Approx number of enemies/NPCs placed on each floor (1..6)."),
                _enemiesPerFloor, 1, 6);
        }
    }

    void Generate()
    {
        var cfg = new BuildCorgiLevel.BuildConfig
        {
            Seed = _seed,
            UseRealLights = _useRealLights,
            NumberOfFloors = _numberOfFloors,
            FloorGaps = _floorGaps,
            Pipes = _pipes,
            LightIntensityVariation = _lightIntensityVariation,
            LightFlicker = _lightFlicker,
            GlobalLightIntensity = _globalLightIntensity,
            PointLightColor = _pointLightColor,
            GlobalLightColor = _globalLightColor,
            Fog = _fog,
            FogIntensity = _fogIntensity,
            Particles = _particles,
            HealthItems = _healthItems,
            Coins = _coins,
            PlayerType = (_playerKeys != null && _playerIndex >= 0 && _playerIndex < _playerKeys.Length)
                ? _playerKeys[_playerIndex] : "Mech Trooper",
            SpawnEnemies = _spawnEnemies,
            EnemiesPerFloor = _enemiesPerFloor,
            EnemyTypes = BuildSelectedEnemyTypes(),
        };
        _lastResult = BuildCorgiLevel.Build(cfg);
        Debug.Log("[LevelGenerator]\n" + _lastResult);
    }

    System.Collections.Generic.List<string> BuildSelectedEnemyTypes()
    {
        var list = new System.Collections.Generic.List<string>();
        if (_enemySelection == null) return list;
        foreach (var kv in _enemySelection)
            if (kv.Value) list.Add(kv.Key);
        return list;
    }
}
