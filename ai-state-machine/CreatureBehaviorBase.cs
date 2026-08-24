using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AIController))]
public abstract class CreatureBehaviorBase : MonoBehaviour
{
    protected AIController controller;

    protected virtual void Awake()
    {
        controller = GetComponent<AIController>();
    }

    public virtual void OnEnterIdle()
    {
        controller.Agent.ResetPath();
        controller.Agent.speed = controller.DefaultSpeed;
        controller.StateTimer = controller.wanderInterval;
    }

    public virtual void OnUpdateIdle()
    {
        if (controller.CanSeePlayer())
        {
            OnDetected(controller.ChaseTarget);
            return;
        }
        if(controller.StateTimer <= 0f)
        {
            controller.EnterState(AIController.State.Wander);
        }
    }

    public virtual void OnDetected(Transform target)
    {
        controller.EnterState(AIController.State.Flee);
    }

    public virtual void OnAlertTimeout()
    {
        controller.EnterState(AIController.State.Wander);
    }

    public abstract void OnAttack(Transform target);

    public virtual void OnEnterWander()
    {
        controller.Agent.speed = controller.DefaultSpeed;
        controller.Agent.SetDestination(controller.RandomPointOnNavMesh(transform.position, controller.wanderRadius));
    }

    public virtual void OnUpdateWander()
    {
        if (controller.CanSeePlayer())
        {
            OnDetected(controller.ChaseTarget);
            return;
        }
        if(!controller.Agent.pathPending && controller.Agent.remainingDistance <= controller.Agent.stoppingDistance)
        {
            controller.EnterState(AIController.State.Idle);
        }
    }

    public virtual void OnEnterAlert()
    {
        controller.Agent.ResetPath();
        controller.StateTimer = controller.alertDuration;
    }

    public virtual void OnUpdateAlert()
    {
        controller.FaceAlertPoint();
        if (controller.CanSeePlayer())
        {
            OnDetected(controller.ChaseTarget);
            return;
        }
        if(controller.StateTimer <= 0f)
        {
            OnAlertTimeout();
        }
    }

    public virtual void OnEnterInvestigate()
    {
        controller.Agent.speed = controller.DefaultSpeed;
        controller.Agent.SetDestination(controller.AlertPoint);
    }

    public virtual void OnUpdateInvestigate()
    {
        if(controller.CanSeePlayer())
        {
            OnDetected(controller.ChaseTarget);
            return;
        }
        if(!controller.Agent.pathPending && controller.Agent.remainingDistance <= controller.Agent.stoppingDistance)
        {
            controller.EnterState(AIController.State.Idle);
        }
    }

    public virtual void OnEnterChase() { }

    public virtual void OnUpdateChase()
    {
        Transform target = controller.ChaseTarget;
        if(target == null)
        {
            controller.EnterState(AIController.State.Idle);
            return;
        }

        controller.FaceTarget(target, controller.alertTurnSpeed);

        if (!controller.CanSeeTarget(target))
        {
            controller.EnterState(AIController.State.Wander);
            return;
        }

        controller.Agent.SetDestination(target.position);

        if(Vector3.Distance(transform.position, target.position) <= controller.attackRange)
        {
            controller.EnterState(AIController.State.Attack);
        }
    }

    public virtual void OnEnterAttack() { }

    public virtual void OnUpdateAttack()
    {
        Transform target = controller.ChaseTarget;
        if(target == null)
        {
            controller.EnterState(AIController.State.Idle);
            return;
        }

        controller.FaceTarget(target, controller.alertTurnSpeed);

        if(Vector3.Distance(transform.position, target.position) > controller.attackRange)
        {
            controller.EnterState(AIController.State.Chase);
            return;
        }

        OnAttack(target);
    }

    public virtual void OnEnterFlee() { }

    public virtual void OnUpdateFlee()
    {
        Transform threat = controller.ChaseTarget;
        if(threat == null)
        {
            controller.EnterState(AIController.State.Idle);
            return;
        }

        float distanceToThreat = Vector3.Distance(transform.position, threat.position);
        if(distanceToThreat > controller.sightRange)
        {
            controller.EnterState(AIController.State.Idle);
            return;
        }

        if(!controller.Agent.pathPending && controller.Agent.remainingDistance <= controller.Agent.stoppingDistance)
        {
            Vector3 fleeDir = (transform.position - threat.position).normalized;
            Vector3 fleePoint = transform.position + fleeDir * controller.wanderRadius;
            if(NavMesh.SamplePosition(fleePoint, out NavMeshHit fleeHit, controller.wanderRadius, NavMesh.AllAreas))
            {
                controller.Agent.SetDestination(fleeHit.position);
            }
        }
    }
}