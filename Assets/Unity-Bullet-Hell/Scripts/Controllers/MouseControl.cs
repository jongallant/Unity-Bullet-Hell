using UnityEngine;
using BulletHell;

public class MouseControl : MonoBehaviour
{
    ProjectileEmitterAdvanced Emitter;
    Camera Camera;

    float Charge;
    float ChargeSpeed = 0.75f;
    float MaxCharge = 1f;
    float MinCharge = 0.2f;

    GameObject ChargePreview;

    void Awake()
    {
        Camera = Camera.main;

        Emitter = transform.GetComponent<ProjectileEmitterAdvanced>();
        if (Emitter == null)
            Debug.Log("MouseControl script must be attached to a gameobject that contains a ProjectileEmitterAdvanced component.");

        ChargePreview = transform.Find("ChargePreview").gameObject;

        Emitter.AutoFire = false;
    }

    void Update()
    {
        Vector2 mousePos = Camera.ScreenToWorldPoint(Input.mousePosition);

        ChargePreview.transform.localScale = new Vector2(Charge, Charge);

        if (Input.GetMouseButton(0))
        {
            Charge += ChargeSpeed * Time.deltaTime;
            Charge = Mathf.Clamp(Charge, MinCharge, MaxCharge);            
        }

        if (Input.GetMouseButtonUp(0))
        {
            Vector2 direction = mousePos - new Vector2(this.transform.position.x, this.transform.position.y);
            Emitter.Scale = Charge / 5f;
            Emitter.FireProjectile(direction, 0);
            Charge = 0;
        }
    }

}
