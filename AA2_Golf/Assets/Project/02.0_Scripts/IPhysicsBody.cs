using UnityEngine;

public interface IPhysicsBody
{
    Vector3 Velocity { get; set; }
    Transform Transform { get; }
    SurfaceMaterial Material { get; }
}