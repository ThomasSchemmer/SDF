using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Particle
{
    public Vector2 position;
    public Vector2 velocity;
    public Vector2 origin;
    public uint currentNode;


    public Particle(Vector2 position, Vector2 velocity, Vector2 origin) {
        this.position = position;
        this.velocity = velocity;
        this.origin = origin;
        currentNode = 0;
    }

}
