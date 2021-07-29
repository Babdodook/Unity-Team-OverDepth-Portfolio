using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TCP;

public class StoneMove : MonoBehaviour
{
    public Transform Target = null;
    public float firingAngle = 45.0f;
    public float gravity = 9.8f;

    public Transform Projectile;
    public Transform StoneModel;
    private Transform myTransform;

    public ParticleSystem WaterSplash;
    AudioSource Sound;
    public AudioClip waterImpact;
    public AudioClip groundImpact;

    CharacterController CC;

    void Awake()
    {
        CC = GetComponent<CharacterController>();
        myTransform = transform;
        WaterSplash.Stop();
        Sound = GetComponent<AudioSource>();
    }

    void Start()
    {
        //StartCoroutine(SimulateProjectile());
    }

    public void OnCollider()
    {
        StoneModel.GetComponent<BoxCollider>().isTrigger = false;
        StoneModel.GetComponent<Stone_isHit>().m_canHit = false;
    }

    public void OffCollider()
    {
        StoneModel.GetComponent<BoxCollider>().isTrigger = true;
    }

    // 서버에서 돌 위치 받기
    public void TCP_RecvStoneInfo(Vector3 _position, Quaternion _rotation, Quaternion _modelRotation)
    {
        transform.position = _position;
        transform.rotation = _rotation;
        StoneModel.rotation = _modelRotation;
    }

    public IEnumerator SimulateProjectile()
    {
        // Short delay added before Projectile is thrown
        //yield return new WaitForSeconds(1.5f);

        // Move projectile to the position of throwing object + add some offset if needed.
        Projectile.position = myTransform.position + new Vector3(0, 0.0f, 0);

        // Calculate distance to target
        float target_Distance = Vector3.Distance(Projectile.position, Target.position);

        // Calculate the velocity needed to throw the object to the target at specified angle.
        float projectile_Velocity = target_Distance / (Mathf.Sin(2 * firingAngle * Mathf.Deg2Rad) / gravity);

        // Extract the X  Y componenent of the velocity
        float Vx = Mathf.Sqrt(projectile_Velocity) * Mathf.Cos(firingAngle * Mathf.Deg2Rad);
        float Vy = Mathf.Sqrt(projectile_Velocity) * Mathf.Sin(firingAngle * Mathf.Deg2Rad);

        // Calculate flight time.
        float flightDuration = target_Distance / Vx;

        // Rotate projectile to face the target.
        Projectile.rotation = Quaternion.LookRotation(Target.position - Projectile.position);
        //Projectile.rotation = Quaternion.Euler(0, Projectile.rotation.eulerAngles.y, 0);

        float elapse_time = 0;
        StoneModel.GetComponent<Stone_isHit>().m_canHit = true;
        while (elapse_time < flightDuration)
        {
            Projectile.Translate(0, (Vy - (gravity * elapse_time)) * Time.deltaTime, Vx * Time.deltaTime);

            Vector3 moveDir = Projectile.position - transform.position;
            //moveDir = transform.InverseTransformDirection(moveDir);
            //CC.Move(moveDir.normalized * 30f * Time.deltaTime);

            StoneModel.Rotate(-4f, -2f, 0);

            elapse_time += Time.deltaTime;

            yield return null;
        }

        
        Camera.main.GetComponent<TestCameraScript>().Camera_Shake(2f, 0.3f, 15, -1);
        WaterSplash.Play();
        Sound.PlayOneShot(waterImpact);
        Sound.PlayOneShot(groundImpact);
        OnCollider();
    }
}
