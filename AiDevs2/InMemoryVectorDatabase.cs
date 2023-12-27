// https://crispycode.net/vector-search-with-c-a-practical-approach-for-small-datasets/

namespace AiDevs2;

internal interface IVectorObject
{
    float[] GetVector();
}

internal sealed class VectorCollection<T> where T : IVectorObject
{
    private readonly List<T> _objects = [];

    public void AddRange(IEnumerable<T> objects)
    {
        _objects.AddRange(objects);
    }

    public T FindNearest(float[] query)
    {
        float maxDotProduct = 0;
        var bestIndex = 0;

        for (var i = 0; i < _objects.Count; i++)
        {
            var dotProd = DotProduct(_objects[i].GetVector(), query);
            if (dotProd > maxDotProduct)
            {
                maxDotProduct = dotProd;
                bestIndex = i;
            }
        }

        return _objects[bestIndex];
    }
    
    private static float DotProduct(float[] a, float[] b)
    {
        float sum = 0;
        for (int i = 0; i < a.Length; i++)
        {
            sum += a[i] * b[i];
        }

        return sum;
    }
}
