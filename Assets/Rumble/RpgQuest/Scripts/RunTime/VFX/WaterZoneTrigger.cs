
using UnityEngine.Rendering;
using UnityEngine;
[RequireComponent(typeof(BoxCollider))]
public class WaterZoneTrigger : MonoBehaviour 
{

   

 [SerializeField] private GameObject underwaterPrefab;
    private GameObject underwaterInstance;


 //   [SerializeField] private GameObject underwaterParent;
    [Header("Mobile Optimization")]
[SerializeField] private float activationDistance = 30f;
[SerializeField] private float checkInterval = 1f;

private Transform playerTransform;
    private float nextCheckTime;

private void Start()
{
    // if (underwaterPrefab == null)
    // {
    //     Debug.LogError("Underwater prefab not assigned!", this);
    // }
    playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    underwaterPrefab.SetActive(false);
}

private void Update()
{
    if (Time.time > nextCheckTime)
    {
        nextCheckTime = Time.time + checkInterval;
        
        // Distance check for mobile efficiency
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance < activationDistance && !underwaterPrefab.activeSelf)
        {
            underwaterPrefab.SetActive(true);
            //EnableGPUInstancing();
        }
        else if (distance >= activationDistance && underwaterPrefab.activeSelf)
        {
            underwaterPrefab.SetActive(false);
        }
    }
} 
    // private void OnTriggerEnter(Collider other)
    // {
    //     if (other.CompareTag("Player"))
    //     {
    //         Debug.Log("Player entered water zone.underwaterParent.SetActive(true)");
    //         // Activate underwater effects");
    //         underwaterParent.SetActive(true);
    //         EnableGPUInstancing();
    //     }
    // }

  private void OnTriggerEnter(Collider other)
    {
     //    Debug.Log($"Trigger entered by: {other.name}");
        if (other.CompareTag("Player") && underwaterInstance == null)
        {
            underwaterInstance = Instantiate(underwaterPrefab);
            underwaterInstance.SetActive(true);
         //   Debug.Log("Underwater prefab instantiated", underwaterInstance);
           EnableGPUInstancing();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
           // underwaterParent.SetActive(false);

            underwaterInstance.SetActive(false);
           // Debug.Log("Player exited water zone. Underwater effects deactivated.");
        }
    }

  //Add to EnableGPUInstancing() method:
private void EnableGPUInstancing()
{
    if (underwaterPrefab == null) return;

    foreach (Transform child in underwaterPrefab.transform)
    {
        var renderer = child.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            // Enable GPU instancing
            renderer.sharedMaterial.enableInstancing = true;

                // Graphics device check (corrected syntax)
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2 ||
          //  if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
                {
                    renderer.sharedMaterial.shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                }
            
            // Shadow handling (corrected enum)
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // Additional optimization for your GTX 1080 Ti
            if (SystemInfo.graphicsDeviceName.Contains("NVIDIA"))
            {
                // NVIDIA-specific optimizations
                renderer.allowOcclusionWhenDynamic = true;
            }
        }
    }
}
}