using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Phase1SceneSetup
{
    private const string ScenePath = "Assets/Scenes/Game.unity";

    // Phase 1 プレースホルダー用のティア色（パステル寄り）
    private static readonly Color[] TierColors =
    {
        new(0.97f, 0.62f, 0.75f), // T1 マカロン: ピンク
        new(0.55f, 0.35f, 0.24f), // T2 チョコシュー: 茶
        new(0.96f, 0.82f, 0.53f), // T3 カップケーキ: クリーム
        new(0.79f, 0.89f, 0.65f), // T4 ロールケーキ: 抹茶
        new(1.00f, 0.95f, 0.88f), // T5 ショートケーキ: 白
        new(0.76f, 0.61f, 0.83f), // T6 デコレーション: 紫
        new(0.95f, 0.58f, 0.54f), // T7 ホールケーキ: サーモン
        new(0.91f, 0.30f, 0.24f), // T8 グランドいちご: 赤
    };

    [MenuItem("PoittoSweets/Phase 1 シーンセットアップ")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Phase1] Play 中は実行できません。■ ボタンで Play を停止してから実行してください");
            return;
        }
        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        SweetData[] tiers = LoadTiers();
        if (tiers.Length != 8)
        {
            Debug.LogError($"[Phase1] SweetData が 8 件見つかりません（{tiers.Length} 件）。先に「プロジェクト初期セットアップ」を実行してください");
            return;
        }

        // TMP Essential Resources 未インポート時は TMP_Settings へのアクセス自体が NRE を投げる
        bool tmpReady;
        try { tmpReady = TMP_Settings.defaultFontAsset != null; }
        catch { tmpReady = false; }
        if (!tmpReady)
        {
            Debug.LogError("[Phase1] TMP Essential Resources が未インポートです。ダイアログの「Import TMP Essentials」（または Window → TextMeshPro → Import TMP Essential Resources）を実行してから、もう一度このメニューを実行してください");
            return;
        }

        CreateSweetPrefabs(tiers);

        // 再実行できるよう、生成済みオブジェクトは一度削除する
        foreach (string name in new[] { "Stage", "Launcher", "GameManager", "HUD" })
        {
            GameObject old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }

        BuildStage();
        SetupCamera();
        SweetSpawner spawner = BuildLauncher(tiers);
        BuildGameManagerAndHud(tiers);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[Phase1] シーンセットアップ完了。Game ビューを縦長（1080x1920）にして Play で確認してください");
    }

    private static SweetData[] LoadTiers()
    {
        return AssetDatabase.FindAssets("t:SweetData", new[] { "Assets/ScriptableObjects/SweetData" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<SweetData>)
            .Where(d => d != null)
            .OrderBy(d => d.tier)
            .ToArray();
    }

    private static void CreateSweetPrefabs(SweetData[] tiers)
    {
        PhysicsMaterial physMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/SweetPhysics.physicMaterial");
        if (physMat == null)
        {
            physMat = new PhysicsMaterial("SweetPhysics")
            {
                bounciness = 0.3f,
                dynamicFriction = 0.4f,
                staticFriction = 0.4f,
                bounceCombine = PhysicsMaterialCombine.Average,
            };
            AssetDatabase.CreateAsset(physMat, "Assets/Materials/SweetPhysics.physicMaterial");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Materials/Sweets"))
            AssetDatabase.CreateFolder("Assets/Materials", "Sweets");

        for (int i = 0; i < tiers.Length; i++)
        {
            SweetData data = tiers[i];
            Material mat = GetOrCreateMaterial($"Assets/Materials/Sweets/{data.name}.mat", "Universal Render Pipeline/Lit", TierColors[i]);

            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            temp.name = data.name;
            temp.transform.localScale = Vector3.one * (data.radius * 2f);
            temp.GetComponent<Renderer>().sharedMaterial = mat;

            var col = temp.GetComponent<SphereCollider>();
            col.sharedMaterial = physMat;

            var rb = temp.AddComponent<Rigidbody>();
            rb.mass = data.mass;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.angularDamping = 1.5f; // 転がり抵抗。無いと平面上でほぼ永久に転がり続ける

            temp.AddComponent<SweetController>().data = data;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, $"Assets/Prefabs/Sweets/{data.name}.prefab");
            Object.DestroyImmediate(temp);

            data.prefab = prefab;
            EditorUtility.SetDirty(data);
        }
    }

    private static Material GetOrCreateMaterial(string path, string shaderName, Color color)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find(shaderName));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void BuildStage()
    {
        var stage = new GameObject("Stage");

        Material counterMat = GetOrCreateMaterial("Assets/Materials/Counter.mat", "Universal Render Pipeline/Lit", new Color(0.85f, 0.83f, 0.80f));
        Material lineMat = GetOrCreateMaterial("Assets/Materials/LaunchLine.mat", "Universal Render Pipeline/Unlit", new Color(0.91f, 0.30f, 0.24f));

        // カウンター（奥行き 6.4）。奥側を 2° 低く傾け、着地後のスイーツが奥へ落ち着くようにする
        GameObject counter = CreateBox(stage.transform, "Counter", new Vector3(0, -0.25f, 0.7f), new Vector3(5f, 0.5f, 6.4f), counterMat, visible: true, collider: true);
        counter.transform.rotation = Quaternion.Euler(2f, 0f, 0f);

        // 透明壁（左右・奥・手前）
        CreateBox(stage.transform, "WallLeft", new Vector3(-2.6f, 1.5f, 0.7f), new Vector3(0.2f, 4f, 6.4f), null, visible: false, collider: true);
        CreateBox(stage.transform, "WallRight", new Vector3(2.6f, 1.5f, 0.7f), new Vector3(0.2f, 4f, 6.4f), null, visible: false, collider: true);
        CreateBox(stage.transform, "WallBack", new Vector3(0, 1.5f, 4.0f), new Vector3(5.4f, 4f, 0.2f), null, visible: false, collider: true);
        CreateBox(stage.transform, "WallFront", new Vector3(0, 1.5f, -2.6f), new Vector3(5.4f, 4f, 0.2f), null, visible: false, collider: true);

        // 発射ライン（見た目のみ。傾斜した天面より少し上に浮かせる）
        CreateBox(stage.transform, "LaunchLine", new Vector3(0, 0.12f, -1.8f), new Vector3(5f, 0.02f, 0.05f), lineMat, visible: true, collider: false);
    }

    private static GameObject CreateBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, bool visible, bool collider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        if (visible)
            go.GetComponent<Renderer>().sharedMaterial = mat;
        else
        {
            Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(go.GetComponent<MeshFilter>());
        }
        if (!collider) Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        return go;
    }

    private static void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = new Vector3(0, 6.0f, -7.5f);
        cam.transform.rotation = Quaternion.Euler(38f, 0f, 0f);
        cam.fieldOfView = 60f;
    }

    private static SweetSpawner BuildLauncher(SweetData[] tiers)
    {
        var go = new GameObject("Launcher");
        go.transform.position = new Vector3(0, 1.1f, -2.0f);

        var spawner = go.AddComponent<SweetSpawner>();
        spawner.tiers = tiers;

        Material lineMat = GetOrCreateMaterial("Assets/Materials/GuideLine.mat", "Universal Render Pipeline/Unlit", Color.white);
        var line = go.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = line.endWidth = 0.04f;
        line.sharedMaterial = lineMat;

        var input = go.AddComponent<InputHandler>();
        input.spawner = spawner;
        input.directionLine = line;
        return spawner;
    }

    private static void BuildGameManagerAndHud(SweetData[] tiers)
    {
        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        gmGo.AddComponent<MergeSystem>().tiers = tiers;

        var hud = new GameObject("HUD");
        var canvas = hud.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = hud.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        gm.scoreText = CreateText(hud.transform, "ScoreText", "0", 96,
            anchor: new Vector2(0.5f, 1f), pos: new Vector2(0, -60), size: new Vector2(600, 120),
            new Color(0.35f, 0.25f, 0.20f));

        CreateText(hud.transform, "NextLabel", "NEXT", 40,
            anchor: new Vector2(1f, 1f), pos: new Vector2(-95, -40), size: new Vector2(200, 50),
            new Color(0.35f, 0.25f, 0.20f));

        var nextGo = new GameObject("NextImage");
        nextGo.transform.SetParent(hud.transform, false);
        var image = nextGo.AddComponent<Image>();
        var rt = image.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-40, -100);
        rt.sizeDelta = new Vector2(110, 110);
        gm.nextImage = image;

        gm.gameOverText = CreateText(hud.transform, "GameOverText", "GAME OVER\nTAP TO RETRY", 90,
            anchor: new Vector2(0.5f, 0.5f), pos: Vector2.zero, size: new Vector2(900, 400),
            new Color(0.91f, 0.30f, 0.24f));
        gm.gameOverText.gameObject.SetActive(false);
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize,
        Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        var rt = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return tmp;
    }
}
