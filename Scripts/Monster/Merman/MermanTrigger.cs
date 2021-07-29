using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MermanTrigger : MonoBehaviour
{
    public MermanController mermanController;
    public Transform Unbroken;
    public Transform Broken;
    AudioSource AS;
    public AudioClip[] BrokenSound;
    public AudioClip[] RoarSound;

    bool PlayOnce;
    private void Awake()
    {
        AS = GetComponentInChildren<AudioSource>();
        PlayOnce = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.GetComponent<PlayerControl>() != null)
        {
            if (!PlayOnce)
            {
                PlayOnce = true;
                mermanController.Target = other.transform;

                StartCoroutine(JumpStart());
            }
        }
    }

    IEnumerator JumpStart()
    {
        yield return new WaitForSeconds(0.2f);
        TestCameraScript.mPthis.Camera_Shake(0.7f, 0.5f, 15, -1f);

        AS.PlayOneShot(BrokenSound[0]);
        AS.PlayOneShot(BrokenSound[1]);
        Unbroken.gameObject.SetActive(false);
        Broken.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.5f);
        int RandomSound = UnityEngine.Random.Range(0, RoarSound.Length);
        AS.PlayOneShot(RoarSound[RandomSound]);
    }
}
