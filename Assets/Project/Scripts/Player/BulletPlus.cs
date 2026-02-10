using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletPlus : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifetime = 5f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Slime enemy'e çarptı mı?
        SlimeEnemyV2 slime = collision.GetComponent<SlimeEnemyV2>();
        if (slime != null)
        {
            slime.TakeDamage((int)damage, transform.position);
            Destroy(gameObject);
            return;
        }

        // Boss'a çarptı mı?
        ZeilBossController boss = collision.GetComponent<ZeilBossController>();
        if (boss != null)
        {
            boss.TakeDamage((int)damage); // Sadece damage parametresi
            Destroy(gameObject);
            return;
        }

        // Duvara çarptığında yok ol
        if (collision.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}