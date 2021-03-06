﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Unit_Base : MonoBehaviour
{
    public enum UnitStateType { None, Idle, Move, Attack, Die }

    protected delegate void StateHandler();

    public SpriteAnimator anim;
    public Unit_Health health;
    public UnitStatsData stats;

    public UnitData.UnitType unitType;

    public SpriteRenderer soldierRenderer;
    public CircleCollider2D circleCollider;

    public AudioSource soundSource;
    public AudioClip walkClip;
    public AudioClip attackClip;

    public Transform fovTransform;

    [SerializeField] protected Agent m_agent;
    [SerializeField] protected Arrive m_seeker;
    [SerializeField] protected Avoid m_avoider;
    [SerializeField] protected AvoidObstacles m_obstacleAvoider;
    [SerializeField] protected Pursue m_pursuer;
    [SerializeField] protected Separate m_separator;

    protected Rigidbody2D m_rigidbody;

    private NavigationController m_navigationController;

    protected Camera m_mainCam;

    protected Vector3 m_startScale;
    protected IDamageable m_attackTarget;
    protected Vector3 m_moveTarget;
    protected bool m_hasMoveTarget;

    protected StateHandler m_currentStateHandler = null;

    private Vector2Int m_prevAnimDirection;

    private Vector3 m_prevSafePosition;
    private Vector3 m_safeMovePrevTarget;
    private float m_safeMoveCheckDist = 0.5f;
    private float m_safeMoveTimer;
    private int m_failedMoveChecks;
    private float m_safeMoveTick = 1f;

    public bool HasAttackTarget { get { return m_attackTarget != null && m_attackTarget.CurrentHealth > 0 && m_attackTarget.DamageableGameObject != null; } }
    public IDamageable AttackTarget { get { return m_attackTarget; } }
    public virtual float AttackDistance { get; }

    public bool HasMoveTarget { get { return m_hasMoveTarget; } }
    public Vector3 MoveTarget { get { return m_moveTarget; } }
    public Separate Separator { get { return m_separator; } }

    private void Awake()
    {
        m_mainCam = Camera.main;
        m_rigidbody = circleCollider.GetComponent<Rigidbody2D>();

        m_startScale = soldierRenderer.transform.localScale;        
    }

    protected virtual void Update()
    {
        Debug.Assert(m_currentStateHandler != null, "No state set.");
        m_currentStateHandler();

        if(m_hasMoveTarget && m_safeMoveTimer <= 0)
        {
            m_safeMoveTimer = m_safeMoveTick;

            if(Vector2.Distance(transform.position, m_prevSafePosition) < m_safeMoveCheckDist)
            {
                m_failedMoveChecks++;
            }
            else
            {
                m_prevSafePosition = transform.position;
            }

            if(m_failedMoveChecks > 4)
            {
                m_safeMovePrevTarget = m_moveTarget;
                SetMoveTarget(m_prevSafePosition);
            }
        }

        if (m_seeker.IsMoving)
        {
            m_safeMoveTimer -= Time.deltaTime;
        }
    }

    protected virtual void LateUpdate()
    {
        // billboard
        //soldierRenderer.transform.rotation = m_mainCam.transform.rotation;
        if (m_agent.velocity.sqrMagnitude > 0.1f)
        {
            anim.PlayAnimation(GetMoveAnimation());
        }
        else
        {
            anim.PlayAnimation(GetIdleAnimation());
        }
    }

    private void OnDrawGizmosSelected()
    {
        Color prevColor = Gizmos.color;

        Gizmos.color = Color.red;

        if (HasAttackTarget)
        {
            if (m_pursuer.target != null)
            {
                Gizmos.DrawLine(transform.position, m_pursuer.target.transform.position);
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, m_pursuer.MoveTarget);
        }

        Gizmos.DrawWireSphere(transform.position, AttackDistance);

        Gizmos.color = Color.green;

        if (HasMoveTarget)
        {
            Gizmos.DrawLine(transform.position, m_moveTarget);
        }

        Gizmos.DrawWireSphere(transform.position, stats.visionDistance);

        Gizmos.color = prevColor;
    }

    public abstract void SetState(UnitStateType type);

    public virtual void Initialize(UnitStatsData statsData, NavigationController navigationController)
    {
        stats = statsData;
        m_navigationController = navigationController;

        if (fovTransform != null)
        {
            fovTransform.localScale = new Vector3(stats.visionDistance * 2f, stats.visionDistance * 2f, 1f);
        }

        health.maxHealth = stats.maxHealth;
    }

    public virtual void SetAttackTarget(IDamageable target)
    {
        m_attackTarget = target;
    }

    public virtual void SetMoveTarget(Vector3 targetPosition)
    {
        const float MIN_NAVIGATION_DISTANCE = 2.5f;
        float dist = Vector2.Distance(transform.position, targetPosition);
        Collider2D hit = Physics2D.Linecast(transform.position, targetPosition, m_navigationController.obstacleLayers).collider;

        m_seeker.flowField = null;

        if (dist > MIN_NAVIGATION_DISTANCE && hit && !hit.isTrigger && hit != circleCollider)
        {
            FlowField flowField = m_navigationController.GetFlowField(this, targetPosition);

            if (flowField != null)
            {
                m_seeker.flowField = flowField;
                m_seeker.gridWorldOffset = m_navigationController.gridOffset;
            }
        }

        m_safeMoveTimer = m_safeMoveTick;
        m_prevSafePosition = transform.position;
        m_failedMoveChecks = 0;

        m_seeker.SetDestination(targetPosition);
        m_moveTarget = targetPosition;
        m_hasMoveTarget = true;
    }

    public void SetSeekerFlowField(FlowField flowField)
    {
        m_seeker.flowField = flowField;
        m_seeker.gridWorldOffset = m_navigationController.gridOffset;
    }

    public bool CanSeeUnit(Unit_Base unit)
    {
        return Vector3.SqrMagnitude(transform.position - unit.transform.position) < stats.visionDistance * stats.visionDistance;
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