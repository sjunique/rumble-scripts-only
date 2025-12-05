using UnityEngine;


/// <summary>
/// Reads the inspector rotation in order to figure out the threhold
/// of the hover vehicle.
/// </summary>
public class InspectorRotation : MonoBehaviour
{
    public float X = 0;
    public float Y = 0;
    public float Z = 0;

    void Update()
    {
        {
            Vector3 angle = transform.eulerAngles;
            X = angle.x;
            Y = angle.y;
            Z = angle.z;

            if (Vector3.Dot(transform.up, Vector3.up) >= 0f)
            {
                if (angle.x >= 0f && angle.x <= 90f)
                {
                    X = angle.x;
                }
                if (angle.x >= 270f && angle.x <= 360f)
                {
                    X = angle.x - 360f;
                }
            }
            if (Vector3.Dot(transform.up, Vector3.up) < 0f)
            {
                if (angle.x >= 0f && angle.x <= 90f)
                {
                    X = 180 - angle.x;
                }
                if (angle.x >= 270f && angle.x <= 360f)
                {
                    X = 180 - angle.x;
                }
            }

            if (angle.y > 180)
            {
                Y = angle.y - 360f;
            }

            if (angle.z > 180)
            {
                Z = angle.z - 360f;
            }

            
        }
    }
}