using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class QueenOyobeeAuthoring
{
    private const string Folder = "Assets/Sprites/Bosses/FreezerVenus/";
    private const string Sheet = Folder + "QueenOyobee.png";
    private const string Prefab = "Assets/Prefabs/Bosses/FreezerVenus/FreezerVenus.prefab";

    [MenuItem("Tools/Bosses/Configure Queen Oyobee Transformation")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before configuring Queen Oyobee.");
        // Keep the supplied sheet intact and remove only its two flat background colors.
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            source.LoadImage(File.ReadAllBytes(Folder + "QueenOyobeeSource.png"));
            Color32 background = source.GetPixels32()[source.width - 1];
            Color32[] pixels = source.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 c = pixels[i];
                if ((c.r == 255 && c.g == 0 && c.b == 255) ||
                    (c.r == background.r && c.g == background.g && c.b == background.b)) pixels[i].a = 0;
            }
            source.SetPixels32(pixels);
            source.Apply();
            File.WriteAllBytes(Sheet, source.EncodeToPNG());
        }
        finally { UnityEngine.Object.DestroyImmediate(source); }
        AssetDatabase.ImportAsset(Sheet, ImportAssetOptions.ForceSynchronousImport);
        Import(Folder + "QueenOyobeeSource.png", false);
        Import(Folder + "FreezerVenusTransform.png", false);
        var importer = Import(Sheet, true);
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var authoredRects = provider.GetSpriteRects().ToDictionary(r => r.name);
        var old = authoredRects.ToDictionary(r => r.Key, r => r.Value.spriteID);
        var rects = new List<SpriteRect>();
        void Add(string name, int x, int top, int width, int height, float pivotY = 0.5f)
        {
            // Preserve hand-authored cuts, pivots and identifiers on repeat configuration.
            if (authoredRects.TryGetValue(name, out var authored))
            {
                rects.Add(authored);
                return;
            }
            rects.Add(new SpriteRect
            {
                name = name, rect = new Rect(x, 442 - top - height, width, height),
                alignment = SpriteAlignment.Custom, pivot = new Vector2(0.5f, pivotY),
                spriteID = old.TryGetValue(name, out var id) ? id : new GUID(Hash128.Compute("QueenOyobee/" + name).ToString())
            });
        }
        for (int i = 0; i < 4; i++) Add("Idle" + i, i * 64, 36, 64, 80, 0.35f);
        for (int i = 0; i < 3; i++) Add("Crown" + i, i * 33, 0, 33, 34);
        for (int i = 0; i < 2; i++)
        {
            Add("Crownless" + i, i * 64, 244, 64, 78, 28f / 78f);
            Add("Hurt" + i, i * 64, 324, 64, 80, 0.35f);
            Add("Snow" + i, 19 + i * 32, 212, 32, 32);
        }
        Add("Scepter0", 19, 120, 29, 71);
        Add("Scepter1", 51, 120, 29, 71);
        Add("Scepter3", 83, 138, 30, 38);
        Add("Scepter4", 118, 157, 66, 32);
        Add("Shadow", 11, 192, 46, 16);
        Add("FreezeSparkle", 1, 406, 35, 35);
        foreach (var authored in authoredRects.Values)
            if (!rects.Any(r => r.name == authored.name)) rects.Add(authored);
        provider.SetSpriteRects(rects.ToArray());
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
            rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(Sheet).OfType<Sprite>().ToDictionary(s => s.name);
        AssetDatabase.ImportAsset("Assets/Sounds/QueenOyobee.mp3", ImportAssetOptions.ForceSynchronousImport);
        var go = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var queen = go.GetComponent<QueenOyobeeController>();
            if (queen == null) queen = go.AddComponent<QueenOyobeeController>();
            queen.transformationSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "FreezerVenusTransform.png");
            queen.idleFrames = Frames(sprites, "Idle", 4);
            queen.hurtFrames = Frames(sprites, "Hurt", 2);
            queen.crownlessFrames = Frames(sprites, "Crownless", 2);
            queen.crownFrames = Frames(sprites, "Crown", 3);
            queen.snowFrames = Frames(sprites, "Snow", 2);
            queen.snowDestructionFrames = Enumerable.Range(2, 4).Select(i => sprites["Snow" + i]).ToArray();
            queen.scepterFrames = new[] { sprites["Scepter0"], sprites["Scepter1"],
                sprites.TryGetValue("Scepter2", out var legacyScepter) ? legacyScepter : null,
                sprites["Scepter3"], sprites["Scepter4"] };
            queen.shadowSprite = sprites["Shadow"];
            queen.scepterShakeSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/QueenOyobee ScepterShake.wav");
            queen.scepterShakeSfxGain = 3f;
            queen.iceCreateSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/QueenOyobee IceCreate.wav");
            queen.iceCreateSfxGain = 3f;
            queen.iceThrowSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/QueenOyobee IceThrow.wav");
            queen.iceThrowSfxGain = 3f;
            queen.music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/QueenOyobee.mp3");
            PrefabUtility.SaveAsPrefabAsset(go, Prefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(go); }
        CalibrateMusic();
        AssetDatabase.SaveAssets();
    }

    private static Sprite[] Frames(Dictionary<string, Sprite> sprites, string prefix, int count)
        => Enumerable.Range(0, count).Select(i => sprites[prefix + i]).ToArray();

    private static TextureImporter Import(string path, bool multiple)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = multiple ? SpriteImportMode.Multiple : SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 512;
        foreach (string platform in new[] { "Standalone", "Android", "iPhone", "WebGL", "Windows Store Apps", "Server" })
            importer.ClearPlatformTextureSettings(platform);
        importer.SaveAndReimport();
        return importer;
    }

    [MenuItem("Tools/Audio/Calibrate Queen Oyobee Music")]
    public static void CalibrateMusic()
    {
        var reference = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Robot Amusement Park Loop.wav");
        var music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/QueenOyobee.mp3");
        float referenceRms = GameMusicController.CalculateRepresentativeRms(reference);
        float rms = GameMusicController.CalculateRepresentativeRms(music);
        if (referenceRms <= 0f || rms <= 0f)
            throw new InvalidOperationException("Could not measure Queen Oyobee music or the stage calibration reference.");
        var go = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var queen = go.GetComponent<QueenOyobeeController>();
            if (queen == null) throw new InvalidOperationException("Configure Queen Oyobee before calibrating.");
            queen.musicVolume = Mathf.Clamp01(referenceRms * 0.4f / rms);
            PrefabUtility.SaveAsPrefabAsset(go, Prefab);
            Debug.Log("Queen Oyobee music calibrated: " + queen.musicVolume + "; RMS: " + rms);
        }
        finally { PrefabUtility.UnloadPrefabContents(go); }
    }
}
