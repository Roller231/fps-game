using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HandIK : MonoBehaviour
{
    [SerializeField] private Transform leftHandTarget;
    [SerializeField, Range(0f, 1f)] private float weight = 1f;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void SetLeftHandTarget(Transform target)
    {
        leftHandTarget = target;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || leftHandTarget == null) return;

        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, weight);
        animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
        animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
    }
}
