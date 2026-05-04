using UnityEngine;

public static class PhysicsManager
{
    public static float gravity = 9.81f;

    public static Vector3 CalculateFriction ( Vector3 velocity, float friction )
    {
        if (velocity.magnitude < 0.01f) return Vector3.zero;

        return -velocity.normalized * friction * gravity;
    }

    public static Vector3 CalculateAirResistance ( Vector3 velocity, float density, float drag, float area )
    {
        if (velocity.magnitude < 0.01f) return Vector3.zero;

        return -velocity.normalized *
               0.5f * density * velocity.sqrMagnitude * drag * area;
    }

    public static float CombineFriction ( float f1, float f2 )
    {
        return Mathf.Sqrt(f1 * f2);
    }

    public static float CombineBounce ( float b1, float b2 )
    {
        return Mathf.Sqrt(b1 * b2);
    }

    // clave para pendientes
    public static Vector3 ProjectGravityOnPlane ( Vector3 normal )
    {
        Vector3 gravityVector = Vector3.down * gravity;

        // quitar componente perpendicular > queda la paralela,
        // para que el objeto se quede pegado a la superficie, no se caiga ni se eleve
        // es decir la gravedad proyectada en el plano nos
        // da la fuerza que actúa sobre el objeto en ese plano 
        return gravityVector - Vector3.Dot(gravityVector, normal) * normal;
    }
}