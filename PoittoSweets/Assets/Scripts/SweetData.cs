using UnityEngine;

[CreateAssetMenu(fileName = "Sweet_T00", menuName = "PoittoSweets/SweetData")]
public class SweetData : ScriptableObject
{
    [Header("識別")]
    public int tier;
    public string displayName;

    [Header("見た目・物理")]
    public GameObject prefab;
    public float radius = 0.5f;
    public float mass = 1f;

    [Header("ゲームバランス")]
    [Tooltip("このティアが合成で生成されたときの加算点（ティア 1 は 0）")]
    public int scoreOnMerge;
    [Tooltip("NEXT 抽選の重み。0 なら出現しない")]
    public float dropWeight = 1f;
}
