﻿using System.Collections.Generic;
using UnityEngine;

public class Avoid : AgentBehaviour
{
    public float tickRate = 0.5f;
    public float collisionRadius = 0.4f;

    private List<GameObject> targets;
    private float m_tickTimer;

    public override void Awake()
    {
        base.Awake();
        targets = new List<GameObject>();
        m_tickTimer = Random.Range(0f, tickRate);
    }

    public override void Update()
    {
        if (m_tickTimer <= 0f)
        {
            targets.Clear();

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, collisionRadius, 1 << LayerMask.NameToLayer("Unit"));

            if (hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].GetComponent<Agent>())
                    {
                        targets.Add(hits[i].gameObject);
                    }
                }
            }

            m_tickTimer = tickRate;
        }

        m_tickTimer -= Time.deltaTime;

        base.Update();
    }

    public override Steering GetSteering()
    {
        Steering steering = new Steering();

        float shortestTime = Mathf.Infinity;
        GameObject firstTarget = null;
        float firstMinSeparation = 0.0f;
        float firstDistance = 0.0f;
        Vector3 firstRelativePos = Vector3.zero;
        Vector3 firstRelativeVel = Vector3.zero;

        foreach (GameObject t in targets)
        {
            if (t == null) continue; // Handlo destroju

            Vector3 relativePos;
            Agent targetAgent = t.GetComponent<Agent>();
            relativePos = t.transform.position - transform.position;

            Vector3 relativeVel = targetAgent.velocity - agent.velocity;
            relativeVel += transform.right;
            float relativeSpeed = relativeVel.magnitude;

            float timeToCollision = Vector3.Dot(relativePos, relativeVel);
            timeToCollision /= relativeSpeed * relativeSpeed * -1;

            float distance = relativePos.magnitude;
            float minSeparation = distance - relativeSpeed * timeToCollision;

            if (minSeparation > collisionRadius * 2f)
            {
                continue;
            }

            if (timeToCollision > 0.0f && timeToCollision < shortestTime)
            {
                shortestTime = timeToCollision;
                firstTarget = t;
                firstMinSeparation = minSeparation;
                firstRelativePos = relativePos;
                firstRelativeVel = relativeVel;
            }
        }

        if (firstTarget == null)
        {
            return steering;
        }

        if (firstMinSeparation <= 0.0f || Mathf.Abs(firstDistance) < collisionRadius * 2)
        {
            firstRelativePos += firstRelativeVel * shortestTime;
        }

        firstRelativePos.Normalize();
        steering.linear = -firstRelativePos * agent.maxAccel;

        return steering;
    }
}