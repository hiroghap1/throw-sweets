using UnityEditor;
using UnityEngine;

public static class ProjectSetup
{
    // PLAN.md §3.4 / §4 / §7.3 の初期値。直径は前ティアの約 1.2 倍、スコアは倍々 + 最大ティア 1000
    // 半径はフィールド幅 3.6（モック準拠の細長カウンター）に T8 が収まるスケール
    private static readonly (int tier, string assetName, string display, float radius, float mass, int score, float weight)[] Tiers =
    {
        (1, "Sweet_T01_Macaron",        "マカロン",             0.30f,  1.0f,    0, 5f),
        (2, "Sweet_T02_ChocoPuff",      "チョコシュー",          0.36f,  1.4f,   10, 3f),
        (3, "Sweet_T03_Cupcake",        "カップケーキ",          0.43f,  2.0f,   20, 2f),
        (4, "Sweet_T04_RollCake",       "ロールケーキ",          0.52f,  2.8f,   40, 0f),
        (5, "Sweet_T05_Shortcake",      "ショートケーキ",        0.62f,  3.9f,   80, 0f),
        (6, "Sweet_T06_DecorationCake", "デコレーションケーキ",   0.75f,  5.5f,  160, 0f),
        (7, "Sweet_T07_WholeCake",      "ホールケーキ",          0.90f,  7.7f,  320, 0f),
        (8, "Sweet_T08_GrandCake",      "グランドいちごケーキ",   1.08f, 10.8f, 1000, 0f),
    };

    [MenuItem("PoittoSweets/プロジェクト初期セットアップ")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[ProjectSetup] Play 中は実行できません。■ ボタンで Play を停止してから実行してください");
            return;
        }

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        // Unity 6 の Personal ライセンスで許可されている（PLAN.md §8.1）
        PlayerSettings.SplashScreen.showUnityLogo = false;

        const string dir = "Assets/ScriptableObjects/SweetData";
        if (!AssetDatabase.IsValidFolder(dir))
        {
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "SweetData");
        }

        int created = 0;
        foreach (var t in Tiers)
        {
            string path = $"{dir}/{t.assetName}.asset";
            var data = AssetDatabase.LoadAssetAtPath<SweetData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<SweetData>();
                AssetDatabase.CreateAsset(data, path);
                created++;
            }
            data.tier = t.tier;
            data.displayName = t.display;
            data.radius = t.radius;
            data.mass = t.mass;
            data.scoreOnMerge = t.score;
            data.dropWeight = t.weight;
            EditorUtility.SetDirty(data);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ProjectSetup] 完了: SweetData 新規 {created} 件 / 既存は最新値で更新 / 縦画面固定 / Unity ロゴ非表示");
    }
}
