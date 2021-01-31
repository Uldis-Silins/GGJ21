﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit_Peditatus : Unit_Base, ISelecteble
{
    public float attackDistance = 2f;
    public float attackDamage = 5f;
    public float attacksDelay = 0.5f;

    private Color m_startColor;
    private readonly int m_colorPropID = Shader.PropertyToID("_Color");

    private float m_attackTimer;

    public bool IsSelected { get; private set; }
    public override float AttackDistance { get { return attackDistance; } }

    protected override void Awake()
    {
        base.Awake();

        m_startColor = soldierRenderer.material.GetColor(m_colorPropID);
    }

    protected override void Update()
    {
        base.Update();

        if (m_currentTarget.Key != null && m_currentTarget.Value != null)
        {
            anim.SetBool("attack", false);
            Vector3 targetPos = m_currentTarget.Key.transform.position;
            targetPos.y = transform.position.y;

            if (Vector3.Distance(targetPos, transform.position) > attackDistance)
            {
                Vector3 dir = (targetPos - transform.position).normalized;
                seeker.SetDestination(targetPos + dir * attackDistance);
            }
            else
            {
                seeker.Stop();

                if (Vector3.Dot(Vector3.right, transform.position - targetPos) > 0)
                {
                    soldierRenderer.transform.localScale = new Vector3(-m_startScale.x, m_startScale.y, m_startScale.z);
                }
                else
                {
                    soldierRenderer.transform.localScale = new Vector3(m_startScale.x, m_startScale.y, m_startScale.z);
                }

                if (!anim.GetBool("attack") && m_attackTimer < attacksDelay / 2f)
                {
                    anim.SetBool("attack", true);

                    if (!soundSource.isPlaying)
                    {
                        soundSource.clip = attackClip;
                        soundSource.loop = false;
                        soundSource.Play();
                    }
                }
                if (m_attackTimer < 0f)
                {
                    m_currentTarget.Value.SetDamage(attackDamage, gameObject);
                    m_attackTimer = attacksDelay;
                    anim.SetBool("attack", false);
                }

                m_attackTimer -= Time.deltaTime;
            }
        }
        else
        {
            anim.SetBool("attack", false);
        }
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
    }

    public void Select()
    {
        IsSelected = true;
        //m_soldierRenderer.material.SetColor(m_colorPropID, Color.green);
    }

    public void Deselect()
    {
        IsSelected = false;
        //m_soldierRenderer.material.SetColor(m_colorPropID, m_startColor);
    }

    public override void SetAttackState(IDamageable target, GameObject obj)
    {
        base.SetAttackState(target, obj);

        m_attackTimer = attacksDelay;
    }
}
