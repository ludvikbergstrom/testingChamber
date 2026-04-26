using UnityEngine;
using UnityEngine.InputSystem;

public class DestructablePoleImpl : DestructablePole
{

    public override void BreakPole(Vector3 breakPoint)
    {
        GameObject bottomPole;
        Vector3 bottomPoleSpawnPos;
        float bottomPoleLenght;

        GameObject topPole;
        Vector3 topPoleSpawnPos;
        float topPoleLenght;



        Vector3 OA_Vec = transform.position - (transform.up * transform.localScale.y); //position of the bottom of the pole.
        Vector3 OB_Vec = transform.position;
        Vector3 OC_Vec = transform.position + (transform.up * transform.localScale.y); //position of the top of the pole.

        Vector3 AB_Vec = OB_Vec - OA_Vec; //directional vector to line that goes through pole

        Vector3 PA_Vec = breakPoint - OA_Vec;
        float PA_Len = PA_Vec.magnitude;

        Vector3 OR_Vec; //point on line running through pole that's closest to the hit position

        OR_Vec = OA_Vec + (Vector3.Dot(PA_Vec,AB_Vec) / Vector3.Dot(AB_Vec,AB_Vec)) * AB_Vec;


        topPoleSpawnPos = (OR_Vec - OC_Vec) * 0.5f + OC_Vec;

        topPoleLenght = (OR_Vec - OC_Vec).magnitude * 0.5f;

        topPole = Instantiate(gameObject, topPoleSpawnPos, transform.rotation);

        topPole.transform.localScale = new Vector3(transform.localScale.x,
                                                      topPoleLenght,
                                                      transform.localScale.z);



        bottomPoleSpawnPos = (OR_Vec - OA_Vec) * 0.5f + OA_Vec;

        bottomPoleLenght = (OR_Vec - OA_Vec).magnitude * 0.5f;

    
        bottomPole = Instantiate(gameObject, bottomPoleSpawnPos, transform.rotation);

        bottomPole.transform.localScale = new Vector3(transform.localScale.x,
                                                          bottomPoleLenght,
                                                          transform.localScale.z);


        Destroy(gameObject);
    }



}
