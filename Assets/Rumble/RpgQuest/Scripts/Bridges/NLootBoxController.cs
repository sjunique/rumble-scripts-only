 
using UnityEngine;
using System;


using UnityEngine;

namespace RpgQuest
{
    public class NLootBoxController : MonoBehaviour
    {
        [Header("Loot Box Settings")]
        public GameObject sphere;
        public GameObject replacementPrefab;
        public GameObject activationFXPrefab;
        public AudioClip activationSound;
        public float activationDistance = 3f;
        public KeyCode activationKey = KeyCode.E;

        [Header("Player binding")]
        public Transform player;              // optional explicit assignment
        public string playerTag = "Player";   // tag of your player clone
        public bool useSpawnBroadcaster = true;

        bool _activated;

        void OnEnable()
        {
            // optional: hook up to your spawn broadcaster
            if (useSpawnBroadcaster)
                PlayerSpawnBroadcaster.OnPlayerSpawned += OnPlayerSpawned;
        }

        void OnDisable()
        {
            if (useSpawnBroadcaster)
                PlayerSpawnBroadcaster.OnPlayerSpawned -= OnPlayerSpawned;
        }

        void Start()
        {
            if (sphere == null)
                Debug.LogError("LootBoxController: Sphere reference is missing on " + name);
        }

        void Update()
        {
            // Make sure we have a player reference, even if spawned late
            TryResolvePlayer();
            if (_activated || player == null) return;

            // Distance + key press (you can remove the key if you want auto-open)
            if (Vector3.Distance(player.position, transform.position) <= activationDistance &&
                Input.GetKeyDown(activationKey))
            {
                ActivateLoot();
            }
        }

        void TryResolvePlayer()
        {
            if (player != null) return;

            // 1) Use PlayerSpawnBroadcaster if available
            if (useSpawnBroadcaster && PlayerSpawnBroadcaster.Last != null)
            {
                player = PlayerSpawnBroadcaster.Last;
                //Debug.Log("[LootBox] Bound player from PlayerSpawnBroadcaster: " + player.name);
                return;
            }

            // 2) Fallback: tag search – works for addressable clone too
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null)
            {
                player = go.transform;
                //Debug.Log("[LootBox] Bound player via tag search: " + player.name);
            }
        }

        void OnPlayerSpawned(Transform t)
        {
            player = t;
            //Debug.Log("[LootBox] PlayerSpawnBroadcaster.OnPlayerSpawned: " + player.name);
        }

        void ActivateLootprev()
        {
            if (_activated) return;
            _activated = true;

            Vector3 spawnPos = sphere != null ? sphere.transform.position : transform.position;

            // 1) FX
            if (activationFXPrefab != null)
                Instantiate(activationFXPrefab, spawnPos, Quaternion.identity);

            if (activationSound != null)
                AudioSource.PlayClipAtPoint(activationSound, spawnPos);

            // 2) Quest logic
            var qc = GetComponent<QuestCollectible>();
            if (qc != null)
                qc.Collect();

            // 3) Replacement
            if (replacementPrefab != null)
                Instantiate(replacementPrefab, spawnPos, Quaternion.identity);

            // 4) Remove sphere
            if (sphere != null)
                Destroy(sphere);
        }
        

void ActivateLoot()
{
    if (_activated) return;
    _activated = true;

    Vector3 spawnPos = sphere != null ? sphere.transform.position : transform.position;

    // 1) PLAY VFX & AUDIO
    if (activationFXPrefab != null)
    {
        var fx = Instantiate(activationFXPrefab, spawnPos, Quaternion.identity);
        Destroy(fx, 2f); // or whatever lifetime your effect needs
    }

    if (activationSound != null)
        AudioSource.PlayClipAtPoint(activationSound, spawnPos);

    // 2) QUEST PROGRESSION
    var qc = GetComponent<QuestCollectible>();
    if (qc != null)
        qc.Collect();

    // 3) SPAWN REPLACEMENT (crystal, opened chest, etc.)
    if (replacementPrefab != null)
        Instantiate(replacementPrefab, spawnPos, Quaternion.identity);

    // 4) DESTROY THE LOOT OBJECT AFTER A SHORT DELAY
    //    (so the VFX has time to run one frame at least)
    Destroy(gameObject, 0.25f);   // tweak delay as you like
}





        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, activationDistance);
        }
    }
}



// namespace RpgQuest
// {
//     public class NLootBoxController : MonoBehaviour
//     {
//         [Header("Loot Box Settings")]
//         public GameObject sphere;
//         public GameObject replacementPrefab;
//         public GameObject activationFXPrefab;
//         public AudioClip activationSound;
//         public float activationDistance = 3f;
//         public KeyCode activationKey = KeyCode.E;

//         private Transform _player;
//         private bool _activated = false;
//         private bool _playerSearchCompleted = false;

//         void OnEnable()
//         {
//             // Subscribe to player spawn events
//             PlayerSpawnBroadcaster.OnPlayerSpawned += OnPlayerSpawned;
            
//             // Check if player already exists
//             if (PlayerSpawnBroadcaster.Last != null)
//             {
//                 _player = PlayerSpawnBroadcaster.Last;
//                 _playerSearchCompleted = true;
//                 Debug.Log("LootBoxController: Found existing player: " + _player.name);
//             }
//             else
//             {
//                 // Fallback: try traditional search
//                 StartCoroutine(FindPlayerFallback());
//             }
//         }

//         void OnDisable()
//         {
//             // Unsubscribe from events
//             PlayerSpawnBroadcaster.OnPlayerSpawned -= OnPlayerSpawned;
//         }

//         void Start()
//         {
//             if (sphere == null)
//                 Debug.LogError("LootBoxController: Sphere reference is missing");
//         }

//         void Update()
//         {
//             // Don't proceed until we have a player or have confirmed there isn't one
//             if (!_playerSearchCompleted) return;
            
//             if (_activated || _player == null) return;

//             // Check proximity (optional: add && Input.GetKeyDown(activationKey) for manual open)
//             if (Vector3.Distance(_player.position, transform.position) <= activationDistance)
//             {
//                 ActivateLoot();
//             }
//         }

//         private void OnPlayerSpawned(Transform playerTransform)
//         {
//             _player = playerTransform;
//             _playerSearchCompleted = true;
// //            Debug.Log("LootBoxController: Player spawned event received: " + _player.name);
//         }

//         private System.Collections.IEnumerator FindPlayerFallback()
//         {
//             int attempts = 0;
//             int maxAttempts = 10;
            
//             while (_player == null && attempts < maxAttempts)
//             {
//                 var go = GameObject.FindGameObjectWithTag("Player");
//                 if (go != null) 
//                 {
//                     _player = go.transform;
//                     Debug.Log("LootBoxController: Found player via fallback search: " + _player.name);
//                     break;
//                 }
                
//                 attempts++;
//                 Debug.Log($"LootBoxController: Player not found, attempt {attempts}/{maxAttempts}");
//                 yield return new WaitForSeconds(0.5f);
//             }
            
//             _playerSearchCompleted = true;
            
//             if (_player == null)
//             {
//                 Debug.LogError("LootBoxController: No GameObject with tag 'Player' found after all attempts");
//             }
//         }

//        private void ActivateLoot()
// {
//     _activated = true;
//     Vector3 spawnPos = sphere.transform.position;

//     // 1) play FX first
//     if (activationFXPrefab != null)
//         Instantiate(activationFXPrefab, spawnPos, Quaternion.identity);

//     // 2) play sound
//     if (activationSound != null)
//         AudioSource.PlayClipAtPoint(activationSound, spawnPos);

//     // 3) trigger QuestCollectible
//     var qc = GetComponent<QuestCollectible>();
//     if (qc != null) qc.Collect();

//     // 4) spawn replacement object (reward / opened chest)
//     if (replacementPrefab != null)
//         Instantiate(replacementPrefab, spawnPos, Quaternion.identity);

//     // 5) clean up visuals
//     Destroy(sphere, 0.3f); // small delay allows FX to render one frame
// }

//         // Optional: Visual feedback in editor
//         void OnDrawGizmosSelected()
//         {
//             Gizmos.color = Color.yellow;
//             Gizmos.DrawWireSphere(transform.position, activationDistance);
//         }
//     }
// }


// /*
// namespace RpgQuest
// {
//     public class NLootBoxController : MonoBehaviour
//     {
//         [Header("Loot Box Settings")]
//         public GameObject sphere;
//         public GameObject replacementPrefab;
//         public GameObject activationFXPrefab;
//         public AudioClip activationSound;
//         public float activationDistance = 3f;
//         public KeyCode activationKey = KeyCode.E;

//         private Transform _player;
//         private bool _activated = false;

//         void Start()
//         {
//             var go = GameObject.FindGameObjectWithTag("Player");
//             if (go != null) _player = go.transform;
//             else Debug.LogError("LootBoxController: No GameObject with tag 'Player' found");

//             if (sphere == null)
//                 Debug.LogError("LootBoxController: Sphere reference is missing");
//         }

//         void Update()
//         {
//             if (_activated || _player == null) return;

//             // check proximity (optional: add && Input.GetKeyDown(activationKey) for manual open)
//             if (Vector3.Distance(_player.position, transform.position) <= activationDistance)
//             {
//                 ActivateLoot();
//             }
//         }

//         private void ActivateLoot()
//         {
//             _activated = true;
//             Vector3 spawnPos = sphere.transform.position;

//             // 1) play FX
//             if (activationFXPrefab != null)
//                 Instantiate(activationFXPrefab, spawnPos, Quaternion.identity);

//             // 2) play sound
//             if (activationSound != null)
//                 AudioSource.PlayClipAtPoint(activationSound, spawnPos);

//             // 3) trigger QuestCollectible logic if present
//             var qc = GetComponent<QuestCollectible>();
//             if (qc != null)
//             {
//                 qc.Collect(); // updates quest and destroys this GO
//             }

//             // 4) spawn replacement
//             if (replacementPrefab != null)
//             {
//                 Instantiate(replacementPrefab, spawnPos, Quaternion.identity);
//             }

//             // 5) remove the old sphere
//             Destroy(sphere);

//             // (Don't Destroy(gameObject) here, let QuestCollectible handle it if needed)
//         }
//     }
// }

// */
