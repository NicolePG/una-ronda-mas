using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Crea el prefab persistente con referencias a los clips de Audio.</summary>
public static class AudioManagerPrefabCreator
{
    private const string AudioRoot = "Assets/_Project/Audio";
    private const string ResourcesFolder = "Assets/_Project/Resources";
    private const string PrefabPath = ResourcesFolder + "/AudioManager.prefab";

    [MenuItem("Una Ronda Más/Audio/Crear o actualizar AudioManager")]
    public static void CreateOrUpdate()
    {
        string effects = AudioRoot + "/Effects/";
        string music = AudioRoot + "/Music/";
        string voices = AudioRoot + "/Anounncer voice the dude/";

        AudioClip menuMusic = LoadClip(music + "Main menu Music.mp3");
        AudioClip gameplayMusic = LoadClip(music + "Gameplay music.mp3");
        AudioClip menuClick = LoadClip(effects + "Menu click.wav");
        AudioClip menuHover = LoadClip(effects + "Menu hover.wav");
        AudioClip obstacleHit = LoadClip(effects + "Damage hit obstacle.ogg");
        AudioClip waterHit = LoadClip(effects + "Damage water splash.wav");
        AudioClip laneChange = LoadClip(effects + "Movement Change Lane.wav");
        AudioClip jump = LoadOptionalClip(effects + "Movement jump.flac");
        AudioClip slideA = LoadClip(effects + "Movement Slide 01.wav");
        AudioClip slideB = LoadClip(effects + "Movement Slide 02.wav");
        AudioClip star = LoadClip(effects + "Points pick up star.wav");
        AudioClip goldenStar = LoadClip(effects + "Points pick up golden.wav");
        AudioClip badPrize = LoadClip(effects + "Reward bad.wav");
        AudioClip goodPrize = LoadClip(effects + "Reward good.wav");
        AudioClip roundStart = LoadClip(effects + "Round Start.wav");
        AudioClip elimination = LoadClip(effects + "Round elimination.wav");
        AudioClip victory = LoadClip(effects + "Round victory.wav");

        AudioClip[] hitVoices = LoadVoices(voices, "hurt", "embarrassing", "leave_a_mark",
            "insurance", "oh_man", "felt_that_one", "viewer_discretion", "painful", "crack",
            "too_bad");
        AudioClip[] victoryVoices = LoadVoices(voices, "stellar", "awesome", "entertained",
            "i_think_you_won", "chosen_one", "doing_the_thing");
        AudioClip[] gameOverVoices = LoadVoices(voices, "game_over", "vibe_killer",
            "bringin_me_down", "snoozefest");
        AudioClip[] generalVoices = LoadRemainingVoices(voices, hitVoices, victoryVoices, gameOverVoices);

        if (jump == null)
            Debug.LogWarning("AudioManager: el clip de salto .flac no se pudo importar y se omitirá. Convierte Movement jump.flac a WAV u OGG para habilitarlo.");

        if (menuMusic == null || gameplayMusic == null || menuClick == null || menuHover == null ||
            obstacleHit == null || waterHit == null || laneChange == null ||
            slideA == null || slideB == null || star == null || goldenStar == null ||
            badPrize == null || goodPrize == null || roundStart == null || elimination == null || victory == null)
        {
            Debug.LogError("AudioManager: faltan clips requeridos. Revisa los nombres y la importación de los archivos de Audio.");
            return;
        }

        ProjectPaths.EnsureFolder(ResourcesFolder);
        var root = new GameObject("AudioManager");
        AudioListener audioListener = root.AddComponent<AudioListener>();
        AudioSource musicSource = AddAudioSource(root, true);
        AudioSource sfxSource = AddAudioSource(root, false);
        AudioSource voiceSource = AddAudioSource(root, false);
        AudioManager manager = root.AddComponent<AudioManager>();
        SerializedObject serialized = new SerializedObject(manager);
        SetObject(serialized, "audioListener", audioListener);
        SetObject(serialized, "musicSource", musicSource);
        SetObject(serialized, "sfxSource", sfxSource);
        SetObject(serialized, "voiceSource", voiceSource);
        SetObject(serialized, "menuMusic", menuMusic);
        SetObject(serialized, "gameplayMusic", gameplayMusic);
        SetObject(serialized, "menuClick", menuClick);
        SetObject(serialized, "menuHover", menuHover);
        SetObject(serialized, "obstacleHit", obstacleHit);
        SetObject(serialized, "waterHit", waterHit);
        SetObject(serialized, "laneChange", laneChange);
        SetObject(serialized, "jump", jump);
        SetObject(serialized, "star", star);
        SetObject(serialized, "goldenStar", goldenStar);
        SetObject(serialized, "badPrize", badPrize);
        SetObject(serialized, "goodPrize", goodPrize);
        SetObject(serialized, "roundStart", roundStart);
        SetObject(serialized, "elimination", elimination);
        SetObject(serialized, "victory", victory);
        SetArray(serialized, "slideClips", new[] { slideA, slideB });
        SetArray(serialized, "hitVoices", hitVoices);
        SetArray(serialized, "victoryVoices", victoryVoices);
        SetArray(serialized, "gameOverVoices", gameOverVoices);
        SetArray(serialized, "generalVoices", generalVoices);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        if (prefab == null)
        {
            Debug.LogError($"AudioManager: no se pudo guardar el prefab en {PrefabPath}.");
            return;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"AudioManager creado en {PrefabPath}. Voces agrupadas: {hitVoices.Length} de golpes, {victoryVoices.Length} de victoria y {gameOverVoices.Length} de eliminación.");
    }

    private static AudioSource AddAudioSource(GameObject root, bool loop)
    {
        AudioSource source = root.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        return source;
    }

    private static AudioClip LoadClip(string path)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) Debug.LogError($"AudioManager: no se encontró o no se pudo importar {path}.");
        return clip;
    }

    private static AudioClip LoadOptionalClip(string path)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static AudioClip[] LoadVoices(string folder, params string[] tokens)
    {
        var matches = new List<AudioClip>();
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder.TrimEnd('/') }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string file = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            foreach (string token in tokens)
            {
                if (!file.Contains(token)) continue;
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null) matches.Add(clip);
                break;
            }
        }

        matches.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return matches.ToArray();
    }

    private static AudioClip[] LoadRemainingVoices(string folder, params AudioClip[][] excludedGroups)
    {
        var excluded = new HashSet<AudioClip>();
        foreach (AudioClip[] group in excludedGroups)
            foreach (AudioClip clip in group) excluded.Add(clip);

        var remaining = new List<AudioClip>();
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder.TrimEnd('/') }))
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
            if (clip != null && !excluded.Contains(clip)) remaining.Add(clip);
        }

        remaining.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return remaining.ToArray();
    }

    private static void SetObject(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"AudioManager: no existe el campo serializado {propertyName}.");
            return;
        }
        property.objectReferenceValue = value;
    }

    private static void SetArray(SerializedObject serialized, string propertyName, AudioClip[] clips)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"AudioManager: no existe el campo serializado {propertyName}.");
            return;
        }
        property.arraySize = clips.Length;
        for (int i = 0; i < clips.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
    }
}
