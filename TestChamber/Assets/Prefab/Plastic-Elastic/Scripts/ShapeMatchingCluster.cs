using System.Collections.Generic;
using Unity.Mathematics;

public class ShapeMatchingCluster
{
    public int[] particleIndices;

    private float3[] q;

    // Sum mi * qi * qi^T
    private float3x3 restCovariance;

    public float3 RestCenter { get; private set; }
    public float3 CurrentCenter { get; private set; }

    public float3x3 Rotation { get; private set; }

    public float3x3 PlasticDeformation { get; private set; }

    public float PlasticYield { get; private set; }

    public bool Valid { get; private set; }

    public float PlasticDeformationNorm
    {
        get
        {
            return math.sqrt(
                math.lengthsq(PlasticDeformation.c0) +
                math.lengthsq(PlasticDeformation.c1) +
                math.lengthsq(PlasticDeformation.c2));
        }
    }

    public ShapeMatchingCluster(
        List<ShapeMatchingParticle> particles,
        List<int> indices,
        float initialPlasticYield)
    {
        particleIndices = indices.ToArray();

        PlasticDeformation = float3x3.identity;
        Rotation = float3x3.identity;

        PlasticYield = initialPlasticYield;

        BuildRestState(particles);
    }

    private void BuildRestState(
        List<ShapeMatchingParticle> particles)
    {
        float totalMass = 0f;
        float3 center = float3.zero;

        foreach (int index in particleIndices)
        {
            ShapeMatchingParticle p = particles[index];

            center += p.position * p.mass;
            totalMass += p.mass;
        }

        if (totalMass <= 0f)
        {
            Valid = false;
            return;
        }

        center /= totalMass;

        RestCenter = center;
        CurrentCenter = center;

        q = new float3[particleIndices.Length];

        restCovariance = new float3x3();

        for (int i = 0; i < particleIndices.Length; i++)
        {
            ShapeMatchingParticle p =
                particles[particleIndices[i]];

            q[i] = p.position - center;

            restCovariance +=
                Matrix3x3Util.OuterProduct(q[i], q[i])
                * p.mass;
        }

        if (math.abs(math.determinant(restCovariance)) < 1e-8f)
        {
            Valid = false;
            return;
        }

        Valid = true;
    }

    /// <summary>
    /// Calculates Apq, Aqq and the best-fit rotation
    /// for the cluster's current predicted particle state.
    /// </summary>
    private bool ComputeCurrentState(
        List<ShapeMatchingParticle> particles,
        out float3 center,
        out float3x3 Apq,
        out float3x3 Aqq,
        out float3x3 rotation)
    {
        center = float3.zero;
        Apq = new float3x3();
        Aqq = new float3x3();
        rotation = float3x3.identity;

        if (!Valid)
            return false;

        float totalMass = 0f;

        // Current center of mass.
        for (int i = 0; i < particleIndices.Length; i++)
        {
            ShapeMatchingParticle p =
                particles[particleIndices[i]];

            center +=
                p.predictedPosition * p.mass;

            totalMass += p.mass;
        }

        if (totalMass <= 0f)
            return false;

        center /= totalMass;

        CurrentCenter = center;

        // This is:
        //
        // sum mi (pi - c) qi^T
        //
        float3x3 rawApq = new float3x3();

        for (int i = 0; i < particleIndices.Length; i++)
        {
            ShapeMatchingParticle p =
                particles[particleIndices[i]];

            float3 currentRelative =
                p.predictedPosition - center;

            rawApq +=
                Matrix3x3Util.OuterProduct(
                    currentRelative,
                    q[i])
                * p.mass;
        }

        float3x3 FpT =
            math.transpose(PlasticDeformation);

        // Equation (1)
        Apq =
            math.mul(
                rawApq,
                FpT);

        // Equation (2)
        Aqq =
            math.mul(
                math.mul(
                    PlasticDeformation,
                    restCovariance),
                FpT);

        float determinant =
            math.determinant(Aqq);

        if (math.abs(determinant) < 1e-8f)
            return false;

        // Apq * inverse(Aqq)
        float3x3 matchedTransformation =
            math.mul(
                Apq,
                math.inverse(Aqq));

        if (!Matrix3x3Util.PolarRotation(
                matchedTransformation,
                out rotation))
        {
            return false;
        }

        Rotation = rotation;

        return true;
    }

    // ============================================================
    // ELASTIC SHAPE MATCHING
    // ============================================================

    public void Project(
    List<ShapeMatchingParticle> particles,
    float stiffness,
    float3[] corrections,
    int[] correctionCounts)
    {
        if (!ComputeCurrentState(
                particles,
                out float3 center,
                out _,
                out _,
                out float3x3 rotation))
        {
            return;
        }

        for (int i = 0; i < particleIndices.Length; i++)
        {
            int particleIndex = particleIndices[i];

            ShapeMatchingParticle p =
                particles[particleIndex];

            float3 plasticallyDeformedRestPosition =
                math.mul(
                    PlasticDeformation,
                    q[i]);

            float3 goal =
                math.mul(
                    rotation,
                    plasticallyDeformedRestPosition)
                + center;

            float3 correction =
                (goal - p.predictedPosition)
                * stiffness;

            corrections[particleIndex] += correction;
            correctionCounts[particleIndex]++;
        }
    }
    private static bool IsFinite(float value)
    {
        return
            !float.IsNaN(value) &&
            !float.IsInfinity(value);
    }

    private static bool IsFinite(float3 value)
    {
        return
            IsFinite(value.x) &&
            IsFinite(value.y) &&
            IsFinite(value.z);
    }

    private static bool IsFinite(float3x3 matrix)
    {
        return
            IsFinite(matrix.c0) &&
            IsFinite(matrix.c1) &&
            IsFinite(matrix.c2);
    }

    private static float FrobeniusNorm(float3x3 matrix)
    {
        return math.sqrt(
            math.lengthsq(matrix.c0) +
            math.lengthsq(matrix.c1) +
            math.lengthsq(matrix.c2));
    }

    public bool RebaseToCurrentConfiguration(
    List<ShapeMatchingParticle> particles)
    {
        if (!Valid ||
            particleIndices == null ||
            particleIndices.Length < 4)
        {
            return false;
        }

        // Recalculate the cluster's current best-fit
        // orientation before replacing its rest state.
        if (!ComputeCurrentState(
                particles,
                out float3 center,
                out _,
                out _,
                out float3x3 rotation))
        {
            return false;
        }

        float3x3 inverseRotation =
            math.transpose(rotation);

        float3[] newQ =
            new float3[particleIndices.Length];

        float3x3 newRestCovariance =
            new float3x3();

        // ---------------------------------------------------------
        // Make the CURRENT particle arrangement the new rest shape.
        //
        // We remove the cluster's rigid rotation first:
        //
        // q_new = R^T (x - c)
        //
        // Therefore:
        //
        // R q_new + c = x
        //
        // immediately after rebasing.
        // ---------------------------------------------------------

        for (int i = 0;
             i < particleIndices.Length;
             i++)
        {
            ShapeMatchingParticle particle =
                particles[particleIndices[i]];

            float3 currentRelative =
                particle.predictedPosition - center;

            newQ[i] =
                math.mul(
                    inverseRotation,
                    currentRelative);

            newRestCovariance +=
                Matrix3x3Util.OuterProduct(
                    newQ[i],
                    newQ[i])
                * particle.mass;
        }

        // Don't accept a degenerate replacement cluster.
        float determinant =
            math.determinant(
                newRestCovariance);

        if (!IsFinite(determinant) ||
            math.abs(determinant) < 1e-8f)
        {
            return false;
        }

        if (!IsFinite(newRestCovariance))
            return false;

        // ---------------------------------------------------------
        // Commit replacement rest state.
        // ---------------------------------------------------------

        q =
            newQ;

        restCovariance =
            newRestCovariance;

        // Plastic deformation has now been embedded directly
        // into q, so the replacement cluster begins with FP = I.
        PlasticDeformation =
            float3x3.identity;

        // Preserve orientation so the surface skinning does
        // not suddenly snap back to identity rotation.
        Rotation =
            rotation;

        CurrentCenter =
            center;

        RestCenter =
            center;

        return true;
    }


    // ============================================================
    // PLASTICITY
    // ============================================================

    public bool UpdatePlasticity(
    List<ShapeMatchingParticle> particles,
    float plasticFlowRate,
    float hardening,
    float dt)
    {
        if (!ComputeCurrentState(
                particles,
                out _,
                out float3x3 Apq,
                out float3x3 Aqq,
                out float3x3 rotation))
        {
            return false;
        }

        // ============================================================
        // 1. Diagonalize Aqq
        // ============================================================

        Matrix3x3Util.SymmetricEigenDecomposition(
            Aqq,
            out float3 lambdaQQ,
            out float3x3 Vqq);

        if (!IsFinite(lambdaQQ))
            return false;

        float minQQ = math.cmin(lambdaQQ);
        float maxQQ = math.cmax(lambdaQQ);

        // Aqq should be positive definite.
        //
        // Do NOT replace bad eigenvalues with tiny positive numbers.
        // That was one source of the previous instability.
        if (minQQ <= 0f ||
            maxQQ <= 0f ||
            minQQ < maxQQ * 1e-5f)
        {
            return false;
        }

        // ============================================================
        // 2. Spq
        // ============================================================

        float3x3 RtApq =
            math.mul(
                math.transpose(rotation),
                Apq);

        float3x3 Spq =
            Matrix3x3Util.Symmetrize(RtApq);

        if (!IsFinite(Spq))
            return false;

        // ============================================================
        // 3. Solve for optimal stretch S
        // ============================================================

        float3x3 localSpq =
            math.mul(
                math.transpose(Vqq),
                math.mul(Spq, Vqq));

        float3x3 scaled = new float3x3();

        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                float lambdaI =
                    Matrix3x3Util.GetComponent(
                        lambdaQQ,
                        row);

                float lambdaJ =
                    Matrix3x3Util.GetComponent(
                        lambdaQQ,
                        column);

                float denominator =
                    lambdaI + lambdaJ;

                if (denominator <= 1e-8f)
                    return false;

                float lambdaBar =
                    2f / denominator;

                float value =
                    Matrix3x3Util.GetElement(
                        localSpq,
                        row,
                        column)
                    * lambdaBar;

                Matrix3x3Util.SetElement(
                    ref scaled,
                    row,
                    column,
                    value);
            }
        }

        float3x3 S =
            math.mul(
                Vqq,
                math.mul(
                    scaled,
                    math.transpose(Vqq)));

        S = Matrix3x3Util.Symmetrize(S);

        if (!IsFinite(S))
            return false;

        // ============================================================
        // 4. Diagonalize S
        // ============================================================

        Matrix3x3Util.SymmetricEigenDecomposition(
            S,
            out float3 stretch,
            out float3x3 V);

        if (!IsFinite(stretch))
            return false;

        // S should be positive definite.
        //
        // Previously we clamped negative eigenvalues to 0.0001.
        // That can create enormous plastic deformation.
        // Reject the update instead.
        if (stretch.x <= 1e-4f ||
            stretch.y <= 1e-4f ||
            stretch.z <= 1e-4f)
        {
            return false;
        }

        // ============================================================
        // 5. Logarithmic strain
        // ============================================================

        float3 logStretch = new float3(
            math.log(stretch.x),
            math.log(stretch.y),
            math.log(stretch.z));

        if (!IsFinite(logStretch))
            return false;

        float strainNorm =
            math.max(
                math.abs(logStretch.x),
                math.max(
                    math.abs(logStretch.y),
                    math.abs(logStretch.z)));

        if (!IsFinite(strainNorm))
            return false;

        if (strainNorm <= PlasticYield)
            return false;

        // ============================================================
        // 6. Plastic flow gamma
        // ============================================================

        float gamma =
            plasticFlowRate *
            (strainNorm - PlasticYield) /
            strainNorm;

        gamma =
            math.clamp(gamma, 0f, 1f);

        if (gamma <= 0f)
            return false;

        // ============================================================
        // 7. Isochoric plastic stretch
        //
        // Paper:
        //
        // ( |Lambda|^(-1/3) Lambda ) ^ gamma
        //
        // Rather than calculating determinant -> power -> power again,
        // do this in logarithmic space.
        //
        // Subtracting the average log stretch means:
        //
        // deltaLog.x + deltaLog.y + deltaLog.z = 0
        //
        // and therefore determinant(update) = 1.
        // ============================================================

        float meanLog =
            (logStretch.x +
             logStretch.y +
             logStretch.z)
            / 3f;

        float3 deviatoricLog =
            logStretch - meanLog;

        float3 deltaLog =
            deviatoricLog * gamma;

        // ============================================================
        // 8. 20% stability limit
        //
        // The paper restricts the plastic change to roughly
        // [0.8, 1.2].
        //
        // Instead of independently clamping all three dimensions
        // (which destroys det = 1), scale the entire logarithmic update.
        // This retains zero volume change.
        // ============================================================

        float lower =
            math.log(0.8f);

        float upper =
            math.log(1.2f);

        float scale = 1f;

        for (int i = 0; i < 3; i++)
        {
            float d =
                Matrix3x3Util.GetComponent(
                    deltaLog,
                    i);

            if (d > upper)
            {
                scale =
                    math.min(
                        scale,
                        upper / d);
            }
            else if (d < lower)
            {
                scale =
                    math.min(
                        scale,
                        lower / d);
            }
        }

        deltaLog *= scale;

        float3 plasticAmount =
            new float3(
                math.exp(deltaLog.x),
                math.exp(deltaLog.y),
                math.exp(deltaLog.z));

        if (!IsFinite(plasticAmount))
            return false;

        // Product should now be ~1.
        float updateDet =
            plasticAmount.x *
            plasticAmount.y *
            plasticAmount.z;

        if (!IsFinite(updateDet) ||
            updateDet <= 0f)
        {
            return false;
        }

        // ============================================================
        // 9. Construct the FP update
        // ============================================================

        float3x3 plasticUpdate =
            math.mul(
                V,
                math.mul(
                    Matrix3x3Util.Diagonal(
                        plasticAmount),
                    math.transpose(V)));

        if (!IsFinite(plasticUpdate))
            return false;

        float3x3 candidateFp =
            math.mul(
                plasticUpdate,
                PlasticDeformation);

        if (!IsFinite(candidateFp))
            return false;

        // ============================================================
        // 10. Numerical volume correction
        //
        // In theory det(FP) remains exactly one.
        // Floating point arithmetic means it will drift slightly.
        // Explicitly remove that drift.
        // ============================================================

        float determinant =
            math.determinant(candidateFp);

        if (!IsFinite(determinant) ||
            determinant <= 1e-6f)
        {
            return false;
        }

        float volumeCorrection =
            math.pow(
                determinant,
                -1f / 3f);

        candidateFp *= volumeCorrection;

        if (!IsFinite(candidateFp))
            return false;

        // ============================================================
        // 11. TEMPORARY limit until we implement cluster resampling
        //
        // The paper removes clusters at FP norm > ~2.0.
        // We cannot replace them yet, so freeze the plastic state
        // rather than allowing the cluster to become invalid.
        // ============================================================

        float candidateNorm =
            FrobeniusNorm(candidateFp);

        if (!IsFinite(candidateNorm))
            return false;

        if (candidateNorm > 1.98f)
            return false;

        // ============================================================
        // Accept the update
        // ============================================================

        PlasticDeformation =
            candidateFp;

        // Leave hardening = 0 for now.
        if (hardening > 0f)
        {
            PlasticYield +=
                hardening *
                strainNorm *
                dt;
        }

        return true;
    }
}