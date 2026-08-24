using UnityEngine;


public class DangerousCreature : CreatureBehaviorBase
{
    public override void OnDetected(Transform target)
    {
        controller.EnterState(AIController.State.Chase);
    }

    public override void OnAlertTimeout()
    {
        controller.EnterState(AIController.State.Investigate);
    }

    public override void OnAttack(Transform target)
    {
        Debug.Log($"{name} is attacking {target.name}");
    }
}
