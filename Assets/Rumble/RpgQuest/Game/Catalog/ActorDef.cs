using UnityEngine;

 
using UnityEngine.AddressableAssets;

// [CreateAssetMenu(menuName="Game/Actor Def")]
// public class ActorDef : ScriptableObject
// {
//     public int Id;
//     public GameObject PreviewPrefab;     // stays (for SelectionScreen preview)
//     public GameObject RuntimePrefab;     // stays (for legacy/manual instantiation)

//     [Header("Addressables (use one of these)")]
//     public AssetReferenceGameObject RuntimeRef;  // <-- NEW: drag the same prefab that’s in your Addressables group
// }

 

[CreateAssetMenu(menuName = "Game/Actor Definition")]
public class ActorDef : ScriptableObject
{
    public string id;                    // "eve", "adam", "car_sci_fi"
    public GameObject previewPrefab;     // NO scripts, just mesh/animator
    public GameObject runtimePrefab;     // full Invector/vehicle prefab
[Header("Addressables (alternative)")]
 public string AddressKey; // e.g. "Eve", "CrawfishRover"
 
  public AssetReferenceGameObject RuntimeRef; 

}
