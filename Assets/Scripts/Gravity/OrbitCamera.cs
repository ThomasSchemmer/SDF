using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrbitCamera : MonoBehaviour
{
    public Transform focus;
    public PlayerController player;
    public float maxAngle = 60;
    float distance = 2;

    float focusRadius = 1f;

    Vector3 focusPoint;
    Vector2 orbitAngles = new Vector2(20, 0);

    private void Awake() {
        focusPoint = focus.position;
        transform.localRotation = Quaternion.Euler(orbitAngles);
    }

    Quaternion debug = new Quaternion();

    private void LateUpdate() {
        UpdateFocusPoint();
        Quaternion goal = AutomaticRotation();

        Vector3 lookPosition = focusPoint - goal * Vector3.forward * distance;
        transform.SetPositionAndRotation(lookPosition, goal);
        debug = goal;
        if (Input.GetKeyDown(KeyCode.H))
            EditorApplication.isPaused = true;
    }

    private void OnDrawGizmos() {
        return;
        Color old = Gizmos.color;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + debug * Vector3.right * 2);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + debug * Vector3.forward * 2);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + debug * Vector3.up * 2);
        Gizmos.color = old;
        Gizmos.DrawSphere(focusPoint, 0.1f);
    }

    void UpdateFocusPoint() {
        Vector3 targetPoint = focus.position;
        if (focusRadius > 0) {
            float distance = Vector3.Distance(targetPoint, focusPoint);
            float t = 1f;
            if(distance > 0.01f) {
                t = Mathf.Pow(0.25f, Time.unscaledDeltaTime);
            }
            if(distance > focusRadius) {
                t = Mathf.Min(t, focusRadius / distance);
            }
            focusPoint = Vector3.Lerp(targetPoint, focusPoint, t);
        } else {
            focusPoint = targetPoint;
        }
    }


    Quaternion AutomaticRotation() {
        //Calculate orbit camera look, which is a translated transform of the player, influenced by the mouse y movement (rotated around right axis)
        orbitAngles = player.GetDesiredAngles();

        Vector3 cameraUp = Quaternion.AngleAxis(orbitAngles.y, player.transform.right) * focus.up;
        Vector3 cameraForward = Quaternion.AngleAxis(orbitAngles.y, player.transform.right) * focus.forward;

        Quaternion goal = Quaternion.LookRotation(cameraForward, cameraUp);

        return goal;
    }



    void ConstrainAngles() {
        orbitAngles.y = orbitAngles.y < 0 ? orbitAngles.y + 360 : orbitAngles.y;
        orbitAngles.y = orbitAngles.y > 360 ? orbitAngles.y - 360 : orbitAngles.y;
    }

    static float GetAngle(Vector3 dir) {
        float angle = Mathf.Acos(dir.z) * Mathf.Rad2Deg;
        return dir.x < 0f ? 360f - angle : angle;
    }
}
