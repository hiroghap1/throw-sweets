using UnityEditor;
using UnityEngine;

public static class ProjectSetup
{
    // PLAN.md §3.4 / §4 / §7.3 の初期値。直径は前ティアの約 1.3 倍、スコアは倍々 + 最大ティア 1000
    private static readonly (int tier, string assetName, string display, float radius, float mass, int score, float weight)[] Tiers =
    {
        (1, "Sweet_T01_Macaron",        "マカロン",             0.30f,  1.0f,    0, 5f),
        (2, "Sweet_T02_ChocoPuff",      "チョコシュー",          0.39f,  1.5f,   10, 3f),
        (3, "Sweet_T03_Cupcake",        "カップケーキ",          0.51f,  2.2f,   20, 2f),
        (4, "Sweet_T04_RollCake",       "ロールケーキ",          0.66f,  3.3f,   40, 0f),
        (5, "Sweet_T05_Shortcake",      "ショートケーキ",        0.86f,  5.0f,   80, 0f),
        (6, "Sweet_T06_DecorationCake", "デコレーションケーキ",   1.11f,  7.5f,  160, 0f),
        (7, "Sweet_T07_WholeCake",      "ホールケーキ",          1.45f, 11.0f,  320, 0f),
        (8, "Sweet_T08_GrandCake",      "グランドいちごケーキ",   1.88f, 17.0f, 1000, 0f),
    };

    [MenuItem("PoittoSweets/プロジェクト初期セットアップ")]
    public static void Run()
    {
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
            if (AssetDatabase.LoadAssetAtPath<SweetData>(path) != null) continue;

            var data = ScriptableObject.CreateInstance<SweetData>();
            data.tier = t.tier;
            data.displayName = t.display;
            data.radius = t.radius;
            data.mass = t.mass;
            data.scoreOnMerge = t.score;
            data.dropWeight = t.weight;
            AssetDatabase.CreateAsset(data, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ProjectSetup] 完了: SweetData {created} 件作成 / 縦画面固定 / Unity ロゴ非表示");
    }
}
