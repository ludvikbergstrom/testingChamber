using Unity.Mathematics;

public static class Matrix3x3Util
{
    public static float3x3 OuterProduct(float3 a, float3 b)
    {
        return new float3x3(
            a * b.x,
            a * b.y,
            a * b.z
        );
    }

    public static float3x3 Diagonal(float3 d)
    {
        return new float3x3(
            new float3(d.x, 0f, 0f),
            new float3(0f, d.y, 0f),
            new float3(0f, 0f, d.z)
        );
    }

    public static float3x3 Symmetrize(float3x3 m)
    {
        return (m + math.transpose(m)) * 0.5f;
    }

    public static float GetElement(float3x3 m, int row, int column)
    {
        if (column == 0)
        {
            if (row == 0) return m.c0.x;
            if (row == 1) return m.c0.y;
            return m.c0.z;
        }

        if (column == 1)
        {
            if (row == 0) return m.c1.x;
            if (row == 1) return m.c1.y;
            return m.c1.z;
        }

        if (row == 0) return m.c2.x;
        if (row == 1) return m.c2.y;
        return m.c2.z;
    }

    public static void SetElement(
        ref float3x3 m,
        int row,
        int column,
        float value)
    {
        if (column == 0)
        {
            if (row == 0) m.c0.x = value;
            else if (row == 1) m.c0.y = value;
            else m.c0.z = value;

            return;
        }

        if (column == 1)
        {
            if (row == 0) m.c1.x = value;
            else if (row == 1) m.c1.y = value;
            else m.c1.z = value;

            return;
        }

        if (row == 0) m.c2.x = value;
        else if (row == 1) m.c2.y = value;
        else m.c2.z = value;
    }

    public static float GetComponent(float3 v, int index)
    {
        if (index == 0) return v.x;
        if (index == 1) return v.y;
        return v.z;
    }

    /// <summary>
    /// Extract rotation using iterative polar decomposition.
    /// </summary>
    public static bool PolarRotation(
        float3x3 matrix,
        out float3x3 rotation)
    {
        rotation = matrix;

        if (math.abs(math.determinant(rotation)) < 1e-8f)
        {
            rotation = float3x3.identity;
            return false;
        }

        for (int i = 0; i < 8; i++)
        {
            float determinant = math.determinant(rotation);

            if (math.abs(determinant) < 1e-8f)
            {
                rotation = float3x3.identity;
                return false;
            }

            float3x3 inverseTranspose =
                math.transpose(math.inverse(rotation));

            rotation =
                (rotation + inverseTranspose) * 0.5f;
        }

        // Avoid reflection.
        if (math.determinant(rotation) < 0f)
        {
            rotation.c2 = -rotation.c2;
        }

        return true;
    }

    /// <summary>
    /// Jacobi eigendecomposition for a symmetric 3x3 matrix.
    ///
    /// matrix = V * diag(eigenvalues) * transpose(V)
    ///
    /// Columns of V are the eigenvectors.
    /// </summary>
    public static void SymmetricEigenDecomposition(
        float3x3 matrix,
        out float3 eigenvalues,
        out float3x3 eigenvectors)
    {
        float3x3 a = Symmetrize(matrix);
        float3x3 v = float3x3.identity;

        for (int iteration = 0; iteration < 12; iteration++)
        {
            float a01 = math.abs(GetElement(a, 0, 1));
            float a02 = math.abs(GetElement(a, 0, 2));
            float a12 = math.abs(GetElement(a, 1, 2));

            int p = 0;
            int q = 1;
            float largest = a01;

            if (a02 > largest)
            {
                largest = a02;
                p = 0;
                q = 2;
            }

            if (a12 > largest)
            {
                largest = a12;
                p = 1;
                q = 2;
            }

            if (largest < 1e-6f)
                break;

            float app = GetElement(a, p, p);
            float aqq = GetElement(a, q, q);
            float apq = GetElement(a, p, q);

            float angle =
                0.5f *
                math.atan2(
                    2f * apq,
                    app - aqq);

            float c = math.cos(angle);
            float s = math.sin(angle);

            float3x3 j = float3x3.identity;

            SetElement(ref j, p, p, c);
            SetElement(ref j, q, q, c);

            SetElement(ref j, p, q, -s);
            SetElement(ref j, q, p, s);

            a = math.mul(
                math.transpose(j),
                math.mul(a, j));

            a = Symmetrize(a);

            v = math.mul(v, j);
        }

        eigenvalues = new float3(
            GetElement(a, 0, 0),
            GetElement(a, 1, 1),
            GetElement(a, 2, 2));

        eigenvectors = v;
    }
}