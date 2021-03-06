﻿using System.Collections;
using UnityEngine;
using System;

public class Unit_Health : MonoBehaviour, IDamageable
{
    public event Action<IDamageable> onKilled = delegate { };

    public float maxHealth;

    [SerializeField] private OwnerType m_ownerType;

    public GameObject ragdoll;

    [SerializeField] private SpriteRenderer m_sprite;
    private float m_damageTimer;
    private readonly float m_damageFlashTime = 0.25f;

    [SerializeField] private float m_damageableRadius;

    private Unit_Base m_lastAttacker;

    public float CurrentHealth { get; protected set; }
    public GameObject DamageableGameObject { get { return this.gameObject; } }
    public OwnerType Faction { get { return m_ownerType; } }
    public float DamageableRadius { get { return m_damageableRadius; } }

    /// <summary>
    /// SideFX auto clear
    /// </summary>
    public Unit_Base Attacker
    {
        get
        {
            Unit_Base attacker = m_lastAttacker;
            m_lastAttacker = null;
            return attacker;
        }
    }

#if ENABLE_CONSOLE
    public bool Immortal { get; set; } = false;
#endif

    private void Start()
    {
        CurrentHealth = maxHealth;
    }

    private void Update()
    {
        if (m_damageTimer > 0f)
        {
            m_sprite.color = Color.Lerp(Color.white, Color.red, m_damageTimer / m_damageFlashTime);
            m_damageTimer -= Time.deltaTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Color prevColor = Gizmos.color;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(DamageableGameObject.transform.position, DamageableRadius);
        Gizmos.color = prevColor;
    }

    public void SetDamage(float damage, Unit_Base attacker)
    {
#if ENABLE_CONSOLE
        if(Immortal) return;
#endif
        Debug.Log($"damage received {this.gameObject.name} from {(attacker == null ? "null" :  attacker.gameObject.name)}");

        CurrentHealth -= damage;

        m_damageTimer = m_damageFlashTime;
        m_sprite.color = Color.red;

        m_lastAttacker = attacker;

        if (CurrentHealth <= 0)
        {
            Kill();
        }
    }

    public void Kill()
    {
        onKilled.Invoke(this);

        if (ragdoll != null)
        {
            Instantiate(ragdoll, transform.position, transform.rotation);
        }
    }
}