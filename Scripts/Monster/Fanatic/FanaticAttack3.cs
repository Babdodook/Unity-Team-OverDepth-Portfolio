using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FanaticAttack3 : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.gameObject.GetComponent<FanaticAnimEvent>().EndAttack();
        //Debug.Log("어택3 끝");
    }
}
