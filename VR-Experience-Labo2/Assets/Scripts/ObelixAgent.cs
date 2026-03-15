using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class ObelixAgent : Agent
{
    public float moveSpeed = 15f;
    public float turnSpeed = 200f;

    public GameObject menhirPrefab;
    public GameObject destinationPrefab;
    public int aantalMenhirs = 1;
    public float destinationRadius = 6f; 
    public float veiligeAfstand = 2.5f; // min afstand tussen menhirs en bestemmingen en obelix

    private List<GameObject> gespawndeObjecten = new List<GameObject>();
    private bool heeftSteen = false; 
    private int afgeleverdeMenhirs = 0; 

    public override void OnEpisodeBegin()
    {
        heeftSteen = false;
        afgeleverdeMenhirs = 0;

        foreach (GameObject obj in gespawndeObjecten)
        {
            Destroy(obj);
        }
        gespawndeObjecten.Clear();

        // 1. Obelix spawnt in het midden
        transform.localPosition = new Vector3(0f, 1f, 0f);
        transform.localRotation = Quaternion.Euler(0, 0, 0);

        List<Vector3> bezettePlekken = new List<Vector3>();
        bezettePlekken.Add(new Vector3(0f, 0f, 0f));

        for (int i = 0; i < aantalMenhirs; i++)
        {
            float hoek = i * Mathf.PI * 2f / aantalMenhirs;
            Vector3 vastePositie = new Vector3(Mathf.Cos(hoek) * destinationRadius, 1f, Mathf.Sin(hoek) * destinationRadius);

            GameObject nieuweDest = Instantiate(destinationPrefab, transform.parent.position + vastePositie, Quaternion.identity, transform.parent);
            nieuweDest.tag = "Destination"; 
            nieuweDest.GetComponent<Collider>().isTrigger = true;
            gespawndeObjecten.Add(nieuweDest);

            bezettePlekken.Add(new Vector3(vastePositie.x, 0f, vastePositie.z));
        }

        for (int i = 0; i < aantalMenhirs; i++)
        {
            Vector3 randomMenhirPos = Vector3.zero;
            bool goedePlekGevonden = false;
            int pogingen = 0;

            while (!goedePlekGevonden && pogingen < 100)
            {
                randomMenhirPos = new Vector3(Random.Range(-8f, 8f), 5f, Random.Range(-8f, 8f));
                Vector3 checkPositie = new Vector3(randomMenhirPos.x, 0f, randomMenhirPos.z);

                goedePlekGevonden = true; 

                foreach (Vector3 bezettePlek in bezettePlekken)
                {
                    if (Vector3.Distance(checkPositie, bezettePlek) < veiligeAfstand)
                    {
                        goedePlekGevonden = false;
                        break; 
                    }
                }
                pogingen++;
            }

            GameObject nieuweMenhir = Instantiate(menhirPrefab, transform.parent.position + randomMenhirPos, Quaternion.identity, transform.parent);
            gespawndeObjecten.Add(nieuweMenhir);
            
            bezettePlekken.Add(new Vector3(randomMenhirPos.x, 0f, randomMenhirPos.z));
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int moveAction = actions.DiscreteActions[0];
        int turnAction = actions.DiscreteActions[1];

        float moveDirection = 0f;
        if (moveAction == 1) moveDirection = 1f;
        else if (moveAction == 2) moveDirection = -1f;

        float turnDirection = 0f;
        if (turnAction == 1) turnDirection = -1f;
        else if (turnAction == 2) turnDirection = 1f;

        transform.Translate(Vector3.forward * moveDirection * moveSpeed * Time.deltaTime);
        transform.Rotate(Vector3.up * turnDirection * turnSpeed * Time.deltaTime);

        AddReward(-0.0005f); // straf voor elke stap om sneller te leren
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Menhir"))
        {
            if (!heeftSteen) 
            {
                heeftSteen = true; 
                gespawndeObjecten.Remove(collision.gameObject);
                Destroy(collision.gameObject); 
                
                AddReward(0.5f); 
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Destination"))
        {
            if (heeftSteen)
            {
                heeftSteen = false; 
                afgeleverdeMenhirs++;
                
                other.GetComponent<Collider>().isTrigger = false;
                //other.GetComponent<Rigidbody>().isKinematic = true;
                other.GetComponent<Renderer>().material.color = Color.orange;

                other.tag = "Untagged";

                AddReward(1.0f); 

                if (afgeleverdeMenhirs >= aantalMenhirs)
                {
                    EndEpisode();    
                }
            }
        }
    }
    private void FixedUpdate()
    {
        if (transform.localPosition.y < -1f)
        {
            AddReward(-1.0f);
            EndEpisode();
            return;
        }

        foreach (GameObject obj in gespawndeObjecten)
        {
            if (obj != null && obj.CompareTag("Menhir"))
            {
                if (obj.transform.localPosition.y < -1f)
                {
                    AddReward(-1.0f);
                    EndEpisode();
                    return; 
                }
            }
        }
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        // 1f als hij een steen heeft, 0f als hij leeg is.
        sensor.AddObservation(heeftSteen ? 1f : 0f);
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0; 
        discreteActionsOut[1] = 0;

        if (Input.GetKey(KeyCode.W)) discreteActionsOut[0] = 1;
        else if (Input.GetKey(KeyCode.S)) discreteActionsOut[0] = 2;

        if (Input.GetKey(KeyCode.A)) discreteActionsOut[1] = 1;
        else if (Input.GetKey(KeyCode.D)) discreteActionsOut[1] = 2;
    }
}