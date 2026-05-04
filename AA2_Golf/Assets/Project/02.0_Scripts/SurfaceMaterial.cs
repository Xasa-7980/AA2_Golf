using UnityEngine;
[CreateAssetMenu(fileName = "SurfaceMaterial", menuName = "Physics/Surface Material")]
public class SurfaceMaterial : ScriptableObject
{
    [Header("Friction")]
    public float friction = 0.4f;

    [Header("Restitution (Bounce)")]
    public float bouncing = 0.5f;
}