using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CustomGravityRigidbody : MonoBehaviour
{
    Rigidbody rb;
    Material mat;
    float floatDelay;
    Vector3 lastPos;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        mat = GetComponent<Renderer>().material;
        lastPos = transform.position;
    }

    private void FixedUpdate() {
        float r = !rb.IsSleeping() && floatDelay <= 0.0001f ? 1 : 0;
        float g = !rb.IsSleeping() && floatDelay <= 1f && floatDelay > 0.0001f ? 1 : 0;
        float b = rb.IsSleeping() ? 1 : 0;
        mat.color = new Color(r, g, b);
        if (rb.IsSleeping()) {
            floatDelay = 0;
            lastPos = transform.position;
            rb.velocity = Vector3.zero;
            return;
        }

        if (Vector3.Distance(lastPos, transform.position) < 0.01f) {
            floatDelay += Time.deltaTime;
            if (floatDelay > 1f) {
                rb.velocity = Vector3.zero;
                return;
            }
        } else {
            floatDelay = 0f;
            lastPos = transform.position;
        }

        rb.AddForce(CustomGravity.GetGravity(rb.position), ForceMode.Acceleration);
    }
}
