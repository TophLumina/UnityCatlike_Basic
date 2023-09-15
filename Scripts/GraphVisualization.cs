using UnityEngine;

public class GraphVisualization : MonoBehaviour
{
    [SerializeField]
    Transform prefab;

    [SerializeField, Range(10, 500)]
    int resolution = 10;

    [SerializeField]
    FunctionLib.FuncType func, shift_func;

    public enum Shifting_Mode { Cycle, Random };

    [SerializeField]
    Shifting_Mode shifting_mod = Shifting_Mode.Cycle;

    [SerializeField, Min(0f)]
    float func_duration = 1f, shifting_duration = 1f;

    float duration = float.Epsilon;

    bool shifting;

    Transform[] points;

    private void Awake()
    {
        points = new Transform[resolution * resolution];
        var scale = Vector3.one / resolution;
        for (int i = 0; i < resolution * resolution; ++i)
        {
            points[i] = Instantiate(prefab);
            points[i].localScale = scale;
            points[i].SetParent(this.transform, false);
        }
    }

    private void Update()
    {
        duration += Time.deltaTime;

        if (shifting)
        {
            if (duration > shifting_duration)
            {
                duration -= shifting_duration;
                shifting = false;
            }
        }

        if (duration > func_duration)
        {
            duration -= func_duration;

            shifting = true;
            shift_func = func;

            switch (shifting_mod)
            {
                case Shifting_Mode.Cycle:
                    func = FunctionLib.ServeNextFunc(func);
                    break;
                case Shifting_Mode.Random:
                    func = FunctionLib.ServeRandFuncExcept(func);
                    break;
            }
        }

        if (shifting)
            func_update_shifting();
        else
            func_update();
    }

    private void func_update()
    {
        float t = Time.time;
        FunctionLib.Func f = FunctionLib.loadFunc(func);

        float step = 2f / resolution;

        for (int i = 0, x = 0, z = 0; i < points.Length; ++i, ++x)
        {
            if (x == resolution)
            {
                x = 0;
                ++z;
            }
            float u = x * step - 1f;
            float v = z * step - 1f;

            points[i].localPosition = f(u, v, t);
        }
    }

    private void func_update_shifting()
    {
        float t = Time.time;
        FunctionLib.Func
        from = FunctionLib.loadFunc(shift_func),
        to = FunctionLib.loadFunc(func);

        float progress = duration / shifting_duration;
        float step = 2f / resolution;

        for (int i = 0, x = 0, z = 0; i < points.Length; ++i, ++x)
        {
            if (x == resolution)
            {
                x = 0;
                ++z;
            }
            float u = x * step - 1f;
            float v = z * step - 1f;

            points[i].localPosition = FunctionLib.Morph(u, v, t, from, to, progress);
        }
    }
}
