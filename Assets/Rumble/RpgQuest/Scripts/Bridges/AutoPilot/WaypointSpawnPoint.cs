using UnityEngine;
 

public class WaypointSpawnPoint : MonoBehaviour
{
    public enum Role { Any, Start, Mid, End }
    public Role role = Role.Any;
}
