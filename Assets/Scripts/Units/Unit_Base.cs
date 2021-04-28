﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Unit_Base : MonoBehaviour
{
    public enum UnitStateType { None, Idle, Move, Attack, Die }

    protected delegate void StateHandler();

    public SpriteAnimator anim;
    public Unit_Health health;

    public UnitData.UnitType unitType;

    public SpriteRenderer soldierRenderer;
    public CircleCollider2D circleCollider;

    public AudioSource soundSource;
    public AudioClip walkClip;
    public AudioClip attackClip;

    public float visionDistance;
    public Transform fovTransform;

    [SerializeField] protected Agent m_agent;
    [SerializeField] protected Arrive m_seeker;
    [SerializeField] protected Avoid m_avoider;
    [SerializeField] protected AvoidObstacles m_obstacleAvoider;
    [SerializeField] protected Pursue m_pursuer;

    private Rigidbody2D m_rigidbody;

    protected Camera m_mainCam;

    protected Vector3 m_startScale;
    protected IDamageable m_attackTarget;
    protected Vector3 m_moveTarget;
    protected bool m_hasMoveTarget;

    protected StateHandler m_currentStateHandler = null;

    protected bool m_hasMoveDirectionChanged;
    private Vector2Int m_prevAnimDirection;

    public bool HasAttackTarget { get { return m_attackTarget != null && m_attackTarget.CurrentHealth > 0 && m_attackTarget.DamageableGameObject != null; } }
    public virtual float AttackDistance { get; }

    public bool HasMoveTarget { get { return m_hasMoveTarget; } }
    public Vector3 MoveTarget { get { return m_moveTarget; } }

    protected virtual void Awake()
    {
        m_mainCam = Camera.main;
        m_rigidbody = circleCollider.GetComponent<Rigidbody2D>();

        m_startScale = soldierRenderer.transform.localScale;

        if (fovTransform != null)
        {
            fovTransform.localScale = new Vector3(visionDistance * 2f, 1f, visionDistance * 2f);
        }
    }

    protected virtual void Update()
    {
        m_hasMoveDirectionChanged = m_prevAnimDirection != GetMoveDirection();

        Debug.Assert(m_currentStateHandler != null, "No state set.");
        m_currentStateHandler();
    }

    protected virtual void LateUpdate()
    {
        // billboard
        soldierRenderer.transform.rotation = m_mainCam.transform.rotation;
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up, GetMoveDirection().ToString());
#endif
    }

    private void OnDrawGizmosSelected()
    {
        Color prevColor = Gizmos.color;

        Gizmos.color = Color.red;

        if (HasAttackTarget)
        {
            Gizmos.DrawLine(transform.position, m_attackTarget.DamageableGameObject.transform.position);
        }

        Gizmos.DrawWireSphere(transform.position, AttackDistance);

        Gizmos.color = Color.green;

        if (HasMoveTarget)
        {
            Gizmos.DrawLine(transform.position, m_moveTarget);
        }

        Gizmos.DrawWireSphere(transform.position, visionDistance);

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(transform.position, m_avoider.collisionRadius);
        Vector2 pos = transform.position;
        Gizmos.DrawLine(transform.position, pos + m_agent.velocity * m_obstacleAvoider.lookAhead);

        Gizmos.color = prevColor;
    }

    public abstract void SetState(UnitStateType type);

    public virtual void SetAttackTarget(IDamageable target)
    {
        m_attackTarget = target;
    }

    public virtual void SetMoveTarget(Vector3 targetPosition)
    {
        m_moveTarget = targetPosition;
        m_hasMoveTarget = true;
    }

    public bool CanSeeUnit(Unit_Base unit)
    {
        return Vector3.Distance(transform.position, unit.transform.position) <= visionDistance;
    }

    private Vector2Int GetMoveDirection()
    {
        Vector2Int direction = Vector2Int.zero;

        if (Vector3.Dot(Vector3.right, m_agent.velocity) < -0.5f)
        {
            direction.x = -1;
        }
        else if (Vector3.Dot(Vector3.right, m_agent.velocity) > 0.5f)
        {
            direction.x = 1;
        }

        if (Vector3.Dot(Vector3.up, m_agent.velocity) < -0.5f)
        {
            direction.y = -1;
        }
        else if (Vector3.Dot(Vector3.up, m_agent.velocity) > 0.5f)
        {
            direction.y = 1;
        }

        return direction;
    }

    protected SpriteAnimatorData.AnimationType GetMoveAnimation()
    {
        Vector2Int direction = GetMoveDirection();

        if (direction != Vector2Int.zero)
        {
            m_prevAnimDirection = direction;
        }

        if (direction.x == 0 && direction.y == 0)
        {
            return SpriteAnimatorData.AnimationType.IdleLeft;
        }

        if (direction.x == 1 && direction.y == 1)
        {
            return SpriteAnimatorData.AnimationType.WalkRightUp;
        }
        else if (direction.x == -1 && direction.y == 1)
        {
            return SpriteAnimatorData.AnimationType.WalkLeftUp;
        }
        else if (direction.x == 1 && direction.y == -1)
        {
            return SpriteAnimatorData.AnimationType.WalkRightDown;
        }
        else if (direction.x == -1 && direction.y == -1)
        {
            return SpriteAnimatorData.AnimationType.WalkLeftDown;
        }
        else if (direction.x == -1 && direction.y == 0)
        {
            return SpriteAnimatorData.AnimationType.WalkLeft;
        }
        else if (direction.x == 1 && direction.y == 0)
        {
            return SpriteAnimatorData.AnimationType.WalkRight;
        }
        else if (direction.x == 0 && direction.y == 1)
        {
            return SpriteAnimatorData.AnimationType.WalkUp;
        }
        else if (direction.x == 0 && direction.y == -1)
        {
            return SpriteAnimatorData.AnimationType.WalkDown;
        }

        throw new System.Exception("No animation for direction " + direction + " not found.");
    }

    protected SpriteAnimatorData.AnimationType GetAttackAnimation(Vector3 targetPosition)
    {
        Vector3 dir = (transform.position - targetPosition).normalized;

        if (Vector3.Dot(Vector3.right, dir) > 0)
        {
            return SpriteAnimatorData.AnimationType.AttackLeft;
        }
        else if (Vector3.Dot(Vector3.right, dir) < 0)
        {
            return SpriteAnimatorData.AnimationType.AttackRight;
        }

        throw new System.Exception("Attack animation for direction " + dir + " not found");
    }

    protected SpriteAnimatorData.AnimationType GetIdleAnimation()
    {
        if(m_prevAnimDirection.x <= 0)
        {
            return SpriteAnimatorData.AnimationType.IdleLeft;
        }
        else
        {
            return SpriteAnimatorData.AnimationType.IdleRight;
        }
    }
}