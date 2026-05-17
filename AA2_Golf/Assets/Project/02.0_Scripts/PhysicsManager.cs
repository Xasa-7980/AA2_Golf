using UnityEngine;

public static class PhysicsManager
{
    public static float gravity = 9.81f;

    public static Vector3 CalculateFriction ( Vector3 velocity, float friction )
    {
        if (velocity.magnitude < 0.01f) return Vector3.zero;

        // Ff = -μv̂
        return -velocity.normalized * friction;
    }

    public static Vector3 CalculateAirResistance ( Vector3 velocity, float density, float drag, float area )
    {
        if (velocity.magnitude < 0.01f) return Vector3.zero;

        // Fd = -½ρv²CdA
        // ρ = density
        // v = velocity
        // Cd = drag coefficient
        // A = area
        return -velocity.normalized *
               0.5f * density * velocity.sqrMagnitude * drag * area;
    }

    public static float CombineFriction ( float f1, float f2 )
    {
        // μ = √(μ1μ2)
        // μ=friction coefficient
        return Mathf.Sqrt(f1 * f2);
    }

    public static float CombineBounce ( float b1, float b2 )
    {
        // e = √(e1e2)
        return Mathf.Sqrt(b1 * b2);
    }

    public static Vector3 gravityVector => Vector3.down * gravity;

    //Necesario para pendientes
    public static Vector3 ReturnGravityOnAngledSurface( Vector3 normal )
    {
        //ReturnGravityOnAngledSurface
        // quitar componente perpendicular > queda la paralela,
        // para que el objeto se quede pegado a la superficie, no se caiga ni se eleve
        // es decir la gravedad proyectada en el plano nos
        // da la fuerza que actúa sobre el objeto en ese plano 
        
        // G = G - (G·N)N
        return gravityVector - Vector3.Dot(gravityVector, normal) * normal;
    }
}