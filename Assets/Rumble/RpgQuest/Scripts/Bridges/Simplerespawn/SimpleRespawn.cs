using UnityEngine;

using System.Collections;
using UnityEngine;
using Invector.vCharacterController;
using Invector;
using System;

public interface ICameraResetter { void ResetCamera(Transform target); }


public class SimpleRespawn : MonoBehaviour
{
    [Header("Invector Components (will be auto-resolved for runtime clones)")]
    public vHealthController health;
    public vThirdPersonController thirdPersonController;
    public vThirdPersonInput input;

    [Header("Respawn Settings")]
    public Transform respawnPoint;
    public string respawnTag = "RespawnPoint";
    public float deathAnimDelay = 0.5f;
    public float invulnAfterRespawn = 1.0f;
   // public GameObject playerInstance;//
    public static event System.Action<GameObject> OnAnyRespawn;
    void Awake()
    {
        if (!health) health = GetComponent<vHealthController>();
        if (!thirdPersonController) thirdPersonController = GetComponent<vThirdPersonController>();
        if (!input) input = GetComponent<vThirdPersonInput>();

        Debug.Log($"[SimpleRespawn] Awake - health: {health != null}, thirdPersonController: {thirdPersonController != null}, input: {input != null}");
    }

    public void OnPlayerDead(GameObject _)
    {
        Debug.Log($"[SimpleRespawn] OnPlayerDead received. Health.isDead: {health?.isDead}, ThirdPerson.isDead: {thirdPersonController?.isDead}");
        StartCoroutine(CoRespawn());

    }

  /// <summary>
    /// Resolve commonly-required Invector references on the runtime clone.
    /// Call this immediately after instantiating the player clone.
    /// </summary>
    public void ResolveForClone(GameObject cloneRoot)
    {
        if (cloneRoot == null)
        {
            Debug.LogError("[SimpleRespawn] ResolveForClone called with null cloneRoot.");
            return;
        }

        // find the components on clone (search children, include inactive)
        var h = cloneRoot.GetComponentInChildren<vHealthController>(true);
        var tp = cloneRoot.GetComponentInChildren<vThirdPersonController>(true);
        var inp = cloneRoot.GetComponentInChildren<vThirdPersonInput>(true);

        // Log and assign
        if (h != null)
        {
            health = h;
            Debug.Log($"[SimpleRespawn] Resolved health -> {h.name}");
        }
        else Debug.LogWarning("[SimpleRespawn] Could not resolve vHealthController on clone.");

        if (tp != null)
        {
            thirdPersonController = tp;
            Debug.Log($"[SimpleRespawn] Resolved vThirdPersonController -> {tp.name}");
        }
        else Debug.LogWarning("[SimpleRespawn] Could not resolve vThirdPersonController on clone.");

        if (inp != null)
        {
            input = inp;
            Debug.Log($"[SimpleRespawn] Resolved vThirdPersonInput -> {inp.name}");
        }
        else Debug.LogWarning("[SimpleRespawn] Could not resolve vThirdPersonInput on clone.");

        // Optionally subscribe to health events (if your workflow expects it)
        TryWireHealthEvents();
    }



    private void TryWireHealthEvents()
    {
        if (health == null)
        {
            Debug.LogWarning("[SimpleRespawn] TryWireHealthEvents: health is null, skipping.");
            return;
        }

        try
        {
            // Many Invector versions have onDead or onChangeHealth events - try subscribing safely
            var evt = health.GetType().GetEvent("onDead");
            if (evt != null)
            {
                // create a delegate that points to this.HandleDeath (example method)
                // if you already subscribe in editor, skip this section
                Debug.Log("[SimpleRespawn] onDead event detected but not auto-subscribed (ensure your existing handlers remain).");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[SimpleRespawn] Exception while wiring health events: " + ex);
        }
    }




    IEnumerator CoRespawn()
    {
        Debug.Log("[SimpleRespawn] CoRespawn started");

        if (deathAnimDelay > 0f)
        {
            Debug.Log($"[SimpleRespawn] Waiting deathAnimDelay: {deathAnimDelay}s");
            yield return new WaitForSeconds(deathAnimDelay);
        }

        var t = ResolveRespawnTransform();
        if (!t)
        {
            Debug.LogError("[SimpleRespawn] No respawn point found.");
            yield break;
        }

        // Temporary invulnerability
        if (health)
        {
            health.isImmortal = true;
            Debug.Log("[SimpleRespawn] Set health immortal: true");
        }

        ForceReviveLatches();

        // Move to respawn point
        transform.SetPositionAndRotation(t.position, t.rotation);
        ZeroPhysics();

        // CRITICAL: Sync both death states
        if (health)
        {
            Debug.Log($"[SimpleRespawn] Before reset - Health.isDead: {health.isDead}, ThirdPerson.isDead: {thirdPersonController?.isDead}");

            // Reset health first
            health.ResetHealth();
            health.isDead = false;

            Debug.Log($"[SimpleRespawn] After health reset - Health.isDead: {health.isDead}, currentHealth: {health.currentHealth}");
        }

        // SYNC vThirdPersonController death state
        if (thirdPersonController)
        {
            thirdPersonController.isDead = false;
            thirdPersonController.enabled = true;

            // CRITICAL: Reset movement-specific controller state
            thirdPersonController.StopCharacter();
            thirdPersonController.ResetInputAnimatorParameters();

            // Force reset locomotion
            thirdPersonController.input = Vector2.zero;
            // thirdPersonController.ChangeHealth(100); // Ensure health is synced

            Debug.Log($"[SimpleRespawn] ThirdPersonController updated - isDead: {thirdPersonController.isDead}, input: {thirdPersonController.input}");
        }

        // TARGETED INPUT FIX - Focus on WASD/movement specifically
        yield return StartCoroutine(ResetMovementInput());

        // Force animation state refresh
        ForceAnimationReset();
        yield return StartCoroutine(UnlockMovementCompletely());

        Debug.Log($"[SimpleRespawn] Final states - Health.isDead: {health?.isDead}, ThirdPerson.isDead: {thirdPersonController?.isDead}");

        // Invulnerability period
        if (invulnAfterRespawn > 0f)
        {
            Debug.Log($"[SimpleRespawn] Invulnerability period: {invulnAfterRespawn}s");
            yield return new WaitForSeconds(invulnAfterRespawn);
        }

        if (health)
        {
            health.isImmortal = false;
            Debug.Log("[SimpleRespawn] Set immortal: false");
        }
        OnAnyRespawn?.Invoke(gameObject);
        Debug.Log("[SimpleRespawn] CoRespawn completed");

        EnsureCameraBindingForMovement();

        InputContextFixer.EnsureGameplayMap(gameObject);
        var tpc = GetComponent<Invector.vCharacterController.vThirdPersonController>();



        // 4. Ensure the TPC script takes input again
      //  tpc.enabled = true;
        
        // Optional: Reset any velocity
     

        // inside SimpleRespawn, after you finish your CoRespawn steps
        void NudgeInvectorForMovement()
        {
            var cam = Camera.main;
            var input = GetComponent<vThirdPersonInput>();
            var tpc = GetComponent<vThirdPersonController>();
            if (input && cam) input.cameraMain = cam;
            if (tpc && cam)
            {
                tpc.lockMovement = false;
                tpc.customAction = false;
                tpc.UpdateMoveDirection(cam.transform);
                tpc.ResetInputAnimatorParameters();
            }
        }

        if (tpc)
        {
            tpc.lockMovement = false;
            tpc.lockRotation = false;
            tpc.customAction = false;
            tpc.isDead = false;
            // Refresh move direction off the (now valid) camera
            var cam = Camera.main ? Camera.main.transform : null;
            if (cam) tpc.UpdateMoveDirection(cam);
        }

    }





    IEnumerator ResetMovementInput()
    {
        Debug.Log("[SimpleRespawn] Starting targeted movement input reset");

        if (!input) yield break;

        // APPROACH 1: Unlock all input systematically
        input.lockInput = false;
        input.lockMoveInput = false;
        input.SetLockBasicInput(false);
        input.SetLockAllInput(false);

        Debug.Log($"[SimpleRespawn] Input unlocked - lockInput: {input.lockInput}, lockMoveInput: {input.lockMoveInput}");

        // APPROACH 2: Reset animator movement parameters specifically
        var animator = GetComponentInChildren<Animator>();
        if (animator && animator.isActiveAndEnabled)
        {
            // Reset all movement-related animator parameters
            ResetMovementAnimatorParameters(animator);
        }

        // APPROACH 3: Force controller to accept input
        if (thirdPersonController)
        {
            // Ensure controller is ready for input
            thirdPersonController.ResetInputAnimatorParameters();
            thirdPersonController.input = Vector2.zero;

            // Force update move direction
            if (input.cameraMain)
            {
                thirdPersonController.UpdateMoveDirection(input.cameraMain.transform);
            }
        }

        // APPROACH 4: Small delay and re-check
        yield return new WaitForSeconds(0.1f);

        // Final verification
        if (thirdPersonController)
        {
            Debug.Log($"[SimpleRespawn] Movement check - input: {thirdPersonController.input}, isDead: {thirdPersonController.isDead}, enabled: {thirdPersonController.enabled}");
        }

        Debug.Log("[SimpleRespawn] Targeted movement input reset completed");
    }

    void ResetMovementAnimatorParameters(Animator animator)
    {
        Debug.Log("[SimpleRespawn] Resetting movement animator parameters");

        // Reset ALL movement parameters to default state
        string[] movementFloats = {
            "InputHorizontal", "InputVertical", "InputMagnitude",
            "VerticalVelocity", "HorizontalSpeed", "MoveSet_ID",
            "Speed", "Direction", "Turn"
        };

        string[] movementBools = {
            "isStrafing", "isSprinting", "isGrounded", "isCrouching",
            "isSliding", "isRolling", "isJumping", "isMoving"
        };

        string[] movementTriggers = {
            "ResetState", "Jump", "Roll", "Land"
        };

        // Reset float parameters
        foreach (string param in movementFloats)
        {
            if (HasParameter(animator, param))
            {
                animator.SetFloat(param, 0f);
                Debug.Log($"[SimpleRespawn] Reset float '{param}' to 0");
            }
        }

        // Reset bool parameters (set to false except isGrounded)
        foreach (string param in movementBools)
        {
            if (HasParameter(animator, param))
            {
                bool value = (param == "isGrounded") ? true : false;
                animator.SetBool(param, value);
                Debug.Log($"[SimpleRespawn] Set bool '{param}' to {value}");
            }
        }

        // Reset triggers
        foreach (string param in movementTriggers)
        {
            if (HasParameter(animator, param))
            {
                animator.ResetTrigger(param);
                Debug.Log($"[SimpleRespawn] Reset trigger '{param}'");
            }
        }

        // Force animator state machine reset
        animator.Rebind();
        animator.Update(0.1f);

        Debug.Log($"[SimpleRespawn] Animator movement parameters reset - isInitialized: {animator.isInitialized}");
    }

    void ForceAnimationReset()
    {
        var animator = GetComponentInChildren<Animator>();
        if (animator)
        {
            Debug.Log("[SimpleRespawn] Force resetting animator death states");

            // Only focus on death parameters here
            string[] deathBools = { "isDead", "Dead", "dead", "IsDead" };
            string[] deathTriggers = { "Death", "death", "die", "OnDeath" };

            foreach (string param in deathBools)
            {
                if (HasParameter(animator, param))
                {
                    animator.SetBool(param, false);
                    Debug.Log($"[SimpleRespawn] Set death bool '{param}' to false");
                }
            }

            foreach (string param in deathTriggers)
            {
                if (HasParameter(animator, param))
                {
                    animator.ResetTrigger(param);
                    Debug.Log($"[SimpleRespawn] Reset death trigger '{param}'");
                }
            }

            // Trigger a respawn animation if available
            if (HasParameter(animator, "Respawn"))
            {
                animator.SetTrigger("Respawn");
                Debug.Log("[SimpleRespawn] Triggered Respawn animation");
            }
        }
    }

    // Safe parameter check method
    private bool HasParameter(Animator animator, string paramName)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    Transform ResolveRespawnTransform()
    {
        if (respawnPoint)
        {
            Debug.Log($"[SimpleRespawn] Using assigned respawnPoint: {respawnPoint.name}");
            return respawnPoint;
        }

        var tagged = GameObject.FindWithTag(respawnTag);
        if (tagged)
        {
            Debug.Log($"[SimpleRespawn] Found tagged respawn: {tagged.name}");
            return tagged.transform;
        }

        var named = GameObject.Find(respawnTag);
        if (named)
        {
            Debug.Log($"[SimpleRespawn] Found named respawn: {named.name}");
            return named.transform;
        }

        Debug.LogError($"[SimpleRespawn] No respawn transform found with tag or name: {respawnTag}");
        return null;
    }

    void ForceReviveLatches()
    {
        Debug.Log("[SimpleRespawn] ForceReviveLatches started");

        // Re-enable Animator
        var anim = GetComponentInChildren<Animator>(true);
        if (anim)
        {
            Debug.Log($"[SimpleRespawn] Animator found: {anim.name}, enabled: {anim.enabled}");
            if (!anim.enabled)
            {
                anim.enabled = true;
                Debug.Log("[SimpleRespawn] Animator re-enabled");
            }
        }
        else
        {
            Debug.LogWarning("[SimpleRespawn] No Animator found");
        }

        // Re-enable colliders
        var colliders = GetComponentsInChildren<Collider>(true);
        Debug.Log($"[SimpleRespawn] Found {colliders.Length} colliders");
        foreach (var col in colliders)
        {
            if (!col.enabled)
            {
                col.enabled = true;
                Debug.Log($"[SimpleRespawn] Re-enabled collider: {col.name}");
            }
        }

        // Re-enable rigidbodies
        var rigidbodies = GetComponentsInChildren<Rigidbody>(true);
        Debug.Log($"[SimpleRespawn] Found {rigidbodies.Length} rigidbodies");
        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            Debug.Log($"[SimpleRespawn] Reset rigidbody: {rb.name}, isKinematic: {rb.isKinematic}");
        }

        // Character controller
        var cc = GetComponent<CharacterController>();
        if (cc)
        {
            cc.enabled = true;
            Debug.Log("[SimpleRespawn] CharacterController re-enabled");
        }

        Debug.Log("[SimpleRespawn] ForceReviveLatches completed");
    }

    void ZeroPhysics()
    {
        Debug.Log("[SimpleRespawn] ZeroPhysics started");

        // root rigidbody
        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;       // instead of rb.linearVelocity
            rb.angularVelocity = Vector3.zero;
            Debug.Log("[SimpleRespawn] Root rigidbody velocities zeroed");
        }

        // children rigidbodies
        var childRbs = GetComponentsInChildren<Rigidbody>();
        foreach (var crb in childRbs)
        {
            crb.linearVelocity = Vector3.zero;
            crb.angularVelocity = Vector3.zero;
        }
        Debug.Log($"[SimpleRespawn] {childRbs.Length} child rigidbody velocities zeroed");

        transform.up = Vector3.up;
        Debug.Log("[SimpleRespawn] Transform upright reset");
    }


    IEnumerator UnlockMovementCompletely()
    {
        // give 2 frames for late listeners (ragdoll, death anim, etc.) to finish
        yield return null;
        yield return null;

        if (input)
        {
            // input-side
            input.SetLockAllInput(false);
            input.SetLockBasicInput(false);
            input.lockInput = false;
            input.lockMoveInput = false;
        }

        if (thirdPersonController)
        {
            // controller-side (these are the usual culprits)
            thirdPersonController.isDead = false;
            thirdPersonController.customAction = false;     // cancels actions that freeze locomotion
            thirdPersonController.lockMovement = false;     // hard unlock
            thirdPersonController.lockRotation = false;

            // make sure physics/capsule are ready to move
            var caps = thirdPersonController.GetComponent<CapsuleCollider>();
            if (caps) caps.enabled = true;

            var rb = thirdPersonController.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.detectCollisions = true;
                // clear any accidental freezes (keep rotation freeze if your controller uses it)
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }

            // reset cached input & force a fresh move direction with the current camera
            thirdPersonController.input = Vector2.zero;
            if (input && input.cameraMain)
                thirdPersonController.UpdateMoveDirection(input.cameraMain.transform);

            // refresh animator locomotion params
            thirdPersonController.ResetInputAnimatorParameters();
            thirdPersonController.StopCharacter();
        }

        Debug.Log("[SimpleRespawn] Movement fully unlocked.");
    }


    // SimpleRespawn.cs — add:
    void EnsureCameraBindingForMovement()
    {
        // 1) Choose the gameplay camera
        Camera cam = Camera.main; // if you toggle SceneCamera on/off, this will be that camera
        if (!cam)
        {
            // fallback: find any enabled camera named like your Scene camera
            var sc = GameObject.Find("SceneCamera");
            if (sc) cam = sc.GetComponent<Camera>();
        }

        // 2) Bind to Invector input/controller
        if (input) input.cameraMain = cam;

        var tpc = GetComponent<vThirdPersonController>();
        if (tpc && cam)
        {
            // force a fresh move direction baked from the current camera
            tpc.UpdateMoveDirection(cam.transform);
            tpc.ResetInputAnimatorParameters();
        }
    }


//         var tpcs = playerInstance.GetComponent<vThirdPersonController>();
//         var bb = tpc.animatorStateInfos;
//         Debug.Log($"[SimpleRespawn] Animator states count: {bb}");
//         tpcs.enabled = true;

//     Animator anim = playerInstance.GetComponent<Animator>();
//         if (anim != null)
//         {
//             anim.SetBool("IsDead", false);
//             // Also force an animation state update so it's not stuck mid-death animation
//             anim.Update(Time.deltaTime); 
//         }
//    if (playerInstance.TryGetComponent<Rigidbody>(out Rigidbody rb))
//         {
//             rb.velocity = Vector3.zero;
//             rb.angularVelocity = Vector3.zero;
//         }





}

