using UnityEngine;

using static UnityEngine.Mathf;

public static class FunctionLib {
    public static int FuncCount => _Funcs.Length;

    public delegate Vector3 Func(float u, float v, float t);

    public enum FuncType { Wave, multiWave, Ripple, Sphere, Torus};

    static Func[] _Funcs = { Wave, multiWave, Ripple, Sphere, Torus};

    public static FuncType ServeNextFunc(FuncType func) =>
        (int)func < _Funcs.Length - 1 ? ++func : 0;

    public static FuncType ServeRandFuncExcept(FuncType func)
    {
        var randfunc = (FuncType)Random.Range(1, _Funcs.Length);
        return randfunc == func ? 0 : randfunc;
    }

    public static Vector3 Morph(float u, float v, float t,Func from, Func to, float progress)
    {
        return Vector3.LerpUnclamped(from(u, v, t), to(u, v, t), SmoothStep(0f, 1f, progress));
    }

    public static Vector3 Wave(float u, float v, float t)
    {
        float y = Sin(PI * (u + v + t));
        return new Vector3(u, y, v);
    }

    public static Vector3 multiWave(float u, float v, float t)
    {
        float y = Sin(PI * (u + .5f * t));
        y += .5f * Sin(2f * PI * (v + t));
        y += Sin(PI * (u + v + .25f * t));
        return new Vector3(u, y, v);
    }

    public static Vector3 Ripple(float u, float v, float t)
    {
        float d = Sqrt(Pow(u, 2) + Pow(v, 2));
        float y = Sin((4f * d - t) * PI);
        return new Vector3(u, y, v);
    }

    public static Vector3 Sphere(float u, float v, float t)
    {
        float r = .9f + .1f * Sin(PI * (12f * u + 8f * v + t));
        float s = r *  Cos(.5f * PI * v);

        float x = s * Sin(PI * u);
        float y = r * Sin(.5f * PI * v);
        float z = s * Cos(PI * u);

        return new Vector3(x, y, z);
    }

    public static Vector3 Torus(float u, float v, float t)
    {
        float r1 = .7f + .1f * Sin(PI * (8f * u + .5f * t));
        float outter = .15f + .05f * Sin(PI * (16f * u + 8f * v + 3f * t));
        float s = r1 + outter * Cos(PI * v);
		Vector3 p;
		p.x = s * Sin(PI * u);
		p.y = outter * Sin(PI * v);
		p.z = s * Cos(PI * u);
		return p;
    }

    public static Func loadFunc(FuncType type) => _Funcs[(int)type]; 
}