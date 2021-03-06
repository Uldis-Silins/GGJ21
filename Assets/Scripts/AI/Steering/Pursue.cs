﻿using System.Collections;
using UnityEngine;

public class Pursue : Seek
{
    public float maxPrediction = 1.0f;
    public Agent target;

    private Vector2 m_targetAux;

    public override void Awake()
    {
        base.Awake();
        m_targetAux = m_moveTarget;
    }

    public override void Update()
    {
        base.Update();
    }

    public override Steering GetSteering()
    {
        if (target == null) return base.GetSteering();

        m_targetAux = target.transform.position;
        Vector3 direction = m_targetAux - m_position;
        float distance = direction.magnitude;
        float speed = agent.velocity.magnitude;
        float prediction;

        if(speed <= distance / maxPrediction)
        {
            prediction = maxPrediction;
        }
        else
        {
            prediction = distance / speed;
        }

        m_moveTarget = m_targetAux;
        m_moveTarget += target.velocity * prediction;

        return base.GetSteering();
    }
}