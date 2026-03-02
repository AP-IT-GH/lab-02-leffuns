using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem; // Voor Keyboard.current in Unity 6

public class CubeAgent : Agent
{
    public Transform target;
    public Transform greenZone;
    public float speedMultiplier = 10f;
    public bool ballTouched = false;
    private Rigidbody rb;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        // Reset bal en target
        target.gameObject.SetActive(true);
        ballTouched = false;

        // Reset positie agent
        this.transform.localPosition = new Vector3(0, 0.5f, 0);
        this.transform.localRotation = Quaternion.identity;

        // Reset snelheid (Unity 6 hernoeming)
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Verplaats target
        target.localPosition = new Vector3(Random.value * 8 - 4, 0.5f, Random.value * 8 - 4);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // sensor.AddObservation(target.localPosition);
        sensor.AddObservation(this.transform.localPosition);
        
        // TOEGEVOEGD: Observaties zodat de agent weet waar hij heen moet
        sensor.AddObservation(target.localPosition);
        sensor.AddObservation(greenZone.localPosition);
        sensor.AddObservation(ballTouched);

        if (rb != null)
        {
            sensor.AddObservation(rb.linearVelocity.x);
            sensor.AddObservation(rb.linearVelocity.z);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        Vector3 controlSignal = Vector3.zero;

        // DISCRETE LOGICA:
        // Branch 0: Horizontaal (0: stil, 1: links, 2: rechts)
        int moveHorizontal = actions.DiscreteActions[0];
        // Branch 1: Verticaal (0: stil, 1: vooruit, 2: achteruit)
        int moveVertical = actions.DiscreteActions[1];

        // Vertalen naar beweging
        if (moveHorizontal == 1) controlSignal.x = -1f;
        else if (moveHorizontal == 2) controlSignal.x = 1f;

        if (moveVertical == 1) controlSignal.z = 1f;
        else if (moveVertical == 2) controlSignal.z = -1f;

        transform.Translate(controlSignal * speedMultiplier * Time.deltaTime);

        // Gebruik een kleine vaste waarde of check of MaxStep groter is dan 0
        if (MaxStep > 0)
        {
            AddReward(-1f / MaxStep);
        }
        else
        {
            // Als MaxStep op 0 staat, geven we een kleine vaste straf per stap
            AddReward(-0.001f);
        }

        // Beloningen
        float distanceToTarget = Vector3.Distance(this.transform.localPosition, target.localPosition);

        // if (distanceToTarget < 1.42f)
        // {
        //     SetReward(1.0f);
        //     EndEpisode();
        // }

        if (this.transform.localPosition.y < 0)
        {
            AddReward(-1.0f);
            EndEpisode();
        }
    }

    // Dit is zodat er geen botsing/kanteling gebeurt voordat de bal is aangeraakt
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target") && !ballTouched)
        {
            AddReward(0.5f);
            ballTouched = true;
            target.gameObject.SetActive(false); 
        }
    }

    private void OnCollisionEnter(Collision collision)
    {

        if (collision.gameObject.CompareTag("GreenZone") && !ballTouched)
        {
            AddReward(-0.01f);    
        }

        if (collision.gameObject.CompareTag("GreenZone") && ballTouched)
        {
            AddReward(1.0f); // Aangepast naar SetReward voor duidelijke finish
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        var keyboard = Keyboard.current;

        if (keyboard != null)
        {
            // Reset naar 0 (stilstaan)
            discreteActionsOut[0] = 0;
            discreteActionsOut[1] = 0;

            // Horizontaal
            if (keyboard.aKey.isPressed) discreteActionsOut[0] = 1;
            else if (keyboard.dKey.isPressed) discreteActionsOut[0] = 2;

            // Verticaal
            if (keyboard.wKey.isPressed) discreteActionsOut[1] = 1;
            else if (keyboard.sKey.isPressed) discreteActionsOut[1] = 2;
        }
    }
}