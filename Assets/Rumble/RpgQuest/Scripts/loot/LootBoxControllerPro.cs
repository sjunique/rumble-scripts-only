 
using UnityEngine;
using RpgQuest.Loot;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;           // ← needed for Unity coroutines (non-generic IEnumerator)
using System.Collections.Generic;   // ← keep this for List<T>, etc.

// key can be AssetReference, string label, etc.


namespace RpgQuest
{
    /// <summary>
    /// Editor- and play-friendly loot box controller.
    /// Replaces the single-spawn logic with a Loot Table assortment.
    /// </summary>
    public class LootBoxControllerPro : MonoBehaviour
    {

        public AssetReference playerRef;



        //---patch-start---//
        [Header("Player Lookup")]
        public string playerTag = "Player";
        public bool autoFindPlayer = true;
        public float reacquireEvery = 1.0f;   // seconds
        public bool preferInvectorController = true; // choose the real player among clones
                                                     //---patch-end---//





        public bool debugLogs = true;
        [Header("Activation (same feel as your current box)")]
        public Transform triggerCenter;                  // if null, uses this.transform
        public float activationDistance = 3f;
        public KeyCode activationKey = KeyCode.None;     // None => open on proximity, else press key near box
        public bool oneShot = true;

        [Header("VFX / SFX / Replace")]
        public GameObject activationFXPrefab;
        public AudioClip activationSound;
        public GameObject replacementPrefab;             // e.g., opened crate/chest
        public Transform replaceAt;                      // where to put replacement; defaults to triggerCenter
        public bool destroyClosedVisual = true;          // destroy the closed mesh after open

        [Header("Loot Table")]
        public LootTableSO lootTable;

        public enum GrantMode { ScatterWorldPickups, DirectToInventory }
        [Header("Grant Mode")]
        public GrantMode grantMode = GrantMode.ScatterWorldPickups;

        [Tooltip("If DirectToInventory is used, look for a component on Player implementing ILootReceiver")]
        public string lootReceiverComponentName = "PlayerInventoryBridge"; // your bridge script name if any

        [Header("Scatter Settings")]
        public float scatterRadius = 1.25f;
        public float upwardForce = 2.0f;                 // small pop
        public LayerMask groundMask = ~0;
        public float groundRayMax = 1000f;
        public float heightOffset = 0.1f;

        [Header("Gizmos")]
        public Color gizmoColor = new Color(1f, 0.84f, 0.2f, 0.3f);
        float _nextFindAt;
        bool _opened;
        Transform _player;




        //---note-start---//
        //If you spawn the player via Addressables
/*
If your player is spawned from a central “GameFlow/Boot” scene, call PlayerSpawnSignals.Announce(playerTr) 
immediately after the player appears. From then on, any loot box (even in later-loaded scenes) will auto-bind in OnEnable().

*/

        IEnumerator SpawnPlayerAddressable(Vector3 pos, Quaternion rot)
        {
            var handle = Addressables.InstantiateAsync(playerRef, pos, rot);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var playerGO = handle.Result;
                var playerTr = playerGO.transform;

                // Hook any boxes already present
                foreach (var box in FindObjectsOfType<LootBoxControllerPro>(true))
                    box.SetPlayer(playerTr);

                // Announce for boxes that will load later
                PlayerSpawnSignals.Announce(playerTr);
            }
            else
            {
                Debug.LogError("[Spawner] Failed to spawn player via Addressables.");
            }
        }









        void OnEnable()
        {
            // If player already spawned earlier, bind immediately
            if (_player == null && PlayerSpawnSignals.CurrentPlayer)
                SetPlayer(PlayerSpawnSignals.CurrentPlayer);

            PlayerSpawnSignals.OnPlayerReady += SetPlayer;
        }

        void OnDisable()
        {
            PlayerSpawnSignals.OnPlayerReady -= SetPlayer;
        }




        public void SetPlayer(Transform t)    // call this from your spawner after it instantiates the player
        {
            _player = t;
        }



        void TryFindPlayerNow()
        {
            // already have a valid one?
            if (_player && _player.gameObject.activeInHierarchy) return;

            // 1) tag search (handles clones)
            var tagged = GameObject.FindGameObjectsWithTag(playerTag);
            Transform best = null;

            if (preferInvectorController)
            {
                // Prefer objects that look like the controllable character
                foreach (var go in tagged)
                    if (go.GetComponent<Invector.vCharacterController.vThirdPersonController>() != null)
                    { best = go.transform; break; }
            }
            if (!best && tagged.Length > 0) best = tagged[0].transform;

            // 2) fallback: any vThirdPersonController without relying on tag
            if (!best)
            {
                var inv = FindObjectOfType<Invector.vCharacterController.vThirdPersonController>();
                if (inv) best = inv.transform;
            }

            if (best) _player = best;
        }

        void Awake()
        {


            if (!triggerCenter) triggerCenter = transform;
            if (autoFindPlayer) TryFindPlayerNow();


            // if (!triggerCenter) triggerCenter = transform;

            // var p = GameObject.FindGameObjectWithTag("Player");
            // if (p) _player = p.transform;
        }



        void Update()
        {
            if (_opened && oneShot) return;

            // re-acquire periodically until we have a player
            if (autoFindPlayer && (Time.unscaledTime >= _nextFindAt))
            {
                _nextFindAt = Time.unscaledTime + Mathf.Max(0.2f, reacquireEvery);
                if (_player == null) TryFindPlayerNow();
            }

            if (_player == null) return;

            float dist = Vector3.Distance(_player.position, triggerCenter.position);
            if (dist > activationDistance) return;

            if (activationKey == KeyCode.None || Input.GetKeyDown(activationKey))
                OpenNow();
        }
        void UpdateSSS()
        {
            if (_opened && oneShot) return;
            if (_player == null) return;

            var dist = Vector3.Distance(_player.position, triggerCenter.position);
            if (dist > activationDistance) return;

            if (activationKey == KeyCode.None || Input.GetKeyDown(activationKey))
            {
                OpenNow();
            }
        }

        public void OpenNow()
        {
            if (_opened && oneShot) return;
            _opened = true;

            Vector3 pos = triggerCenter.position;
            Quaternion rot = triggerCenter.rotation;

            // FX/SFX
            if (activationFXPrefab) Instantiate(activationFXPrefab, pos, rot);
            if (activationSound) AudioSource.PlayClipAtPoint(activationSound, pos);

            // Roll loot
            var rolled = lootTable ? lootTable.Roll() : new List<LootTableSO.Rolled>();
            if (rolled.Count == 0)
                Debug.LogWarning($"[LootBox] No loot rolled (table missing or empty) at {name}");

            // Grant loot



            if (debugLogs)
            {
                if (rolled.Count == 0) Debug.LogWarning($"[LootBox] No loot rolled at {name}");
                else foreach (var r in rolled)
                        Debug.Log($"[LootBox] Rolled {r.qty} x {r.def?.displayName} ({r.def?.itemId})");
            }


            switch (grantMode)
            {
                case GrantMode.DirectToInventory:
                    GiveToInventory(rolled);
                    break;
                case GrantMode.ScatterWorldPickups:
                    ScatterPickups(rolled, pos);
                    break;
            }

            // Replace visuals
            if (replacementPrefab)
            {
                var at = replaceAt ? replaceAt : triggerCenter;
                Instantiate(replacementPrefab, at.position, at.rotation);
            }
            if (destroyClosedVisual) Destroy(gameObject);
        }

        // --- Inventory bridge (optional) ---
        void GiveToInventory(List<LootTableSO.Rolled> loot)
        {
            if (_player == null)
            {
                Debug.LogWarning("[LootBox] No player found for DirectToInventory mode.");
                return;
            }

            var comp = string.IsNullOrEmpty(lootReceiverComponentName)
                ? null
                : _player.GetComponent(lootReceiverComponentName);

            if (comp == null)
            {
                Debug.LogWarning($"[LootBox] Loot receiver '{lootReceiverComponentName}' not found on Player. " +
                                 "Falling back to world scatter for now.");
                ScatterPickups(loot, triggerCenter.position);
                return;
            }

            // Expect a method: bool TryGiveItem(string itemId, int qty, LootItemDef def)
            var m = comp.GetType().GetMethod("TryGiveItem");
            if (m == null)
            {
                Debug.LogWarning("[LootBox] Loot receiver is missing TryGiveItem(string,int,LootItemDef).");
                ScatterPickups(loot, triggerCenter.position);
                return;
            }

            foreach (var r in loot)
            {
                if (r.def == null) continue;
                m.Invoke(comp, new object[] { r.def.itemId, r.qty, r.def });
            }
        }

        // --- World scatter ---
        void ScatterPickups(List<LootTableSO.Rolled> loot, Vector3 center)
        {
            var rng = new System.Random();
            foreach (var r in loot)
            {
                if (r.def == null) continue;

                // If no prefab, skip scatter for this entry
                var prefab = r.def.worldPickupPrefab;
                if (!prefab) continue;

                // spawn one object per stack (pickup handles its own quantity or use a text label)
                Vector3 pos = center + RandomOnDisk(rng) * scatterRadius;
                pos = ProjectToGround(pos) + Vector3.up * heightOffset;

                // in ScatterPickups(), before Instantiate():
                if (!prefab)
                {
                    if (debugLogs) Debug.LogWarning($"[LootBox] '{r.def?.displayName}' has no worldPickupPrefab, skipping scatter.");
                    continue;
                }
                if (debugLogs) Debug.Log($"[LootBox] Spawning {r.qty} x {r.def.displayName} at {pos}");




                var go = Instantiate(prefab, pos, Quaternion.identity);
                var rb = go.GetComponent<Rigidbody>();
                if (rb) rb.AddForce(Vector3.up * upwardForce, ForceMode.VelocityChange);

                // Optional: if your pickup has a known API, pass quantity
                // e.g., go.GetComponent<ItemPickup>()?.Init(r.def.itemId, r.qty);
            }
        }

        Vector3 RandomOnDisk(System.Random rng)
        {
            // uniform on disk
            float t = (float)rng.NextDouble() * Mathf.PI * 2f;
            float u = (float)rng.NextDouble() + (float)rng.NextDouble();
            float r = (u > 1f) ? 2f - u : u; // triangle -> uniform
            return new Vector3(Mathf.Cos(t), 0f, Mathf.Sin(t)) * r;
        }

        Vector3 ProjectToGround(Vector3 world)
        {
            // Terrain first
            Terrain nearest = null; float best = float.MaxValue;
            foreach (var t in Terrain.activeTerrains)
            {
                float d = (t.transform.position - world).sqrMagnitude;
                if (d < best) { best = d; nearest = t; }
            }
            if (nearest)
            {
                float y = nearest.SampleHeight(world) + nearest.transform.position.y;
                return new Vector3(world.x, y, world.z);
            }

            // Mesh raycast
            var ray = new Ray(world + Vector3.up * (groundRayMax * 0.5f), Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, groundRayMax, groundMask, QueryTriggerInteraction.Ignore))
                return hit.point;

            return world;
        }

        void OnDrawGizmosSelected()
        {
            if (!triggerCenter) triggerCenter = transform;
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(triggerCenter.position, activationDistance);
            if (grantMode == GrantMode.ScatterWorldPickups)
            {
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.4f);
                Gizmos.DrawWireSphere(triggerCenter.position, scatterRadius);
            }
        }
    }


}

