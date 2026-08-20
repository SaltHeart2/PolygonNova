using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossBlackHole : MonoBehaviour
{
    public float speed = 6f;
    public float lifeTime = 8f;
    public float damagePerSecond = 250f;

    Transform player;
    Rigidbody2D rb;


    void Start()
    {
        // Localizar al jugador
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>(); // Obtener referencias de RigidBody

        Destroy(gameObject, lifeTime); //Destruye todo los agujeros negros existentes al empezar
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        // Calcular la dirección y mover hacia el jugador
        Vector2 dir = (player.position - transform.position).normalized;
        rb.velocity = dir * speed;
    }

    // Aplicar el daño contínuo cuando se solapa con el jugador
    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player playerScript = other.GetComponent<Player>();
        if (playerScript == null)
            return;

        playerScript.TakeDamage(damagePerSecond * Time.deltaTime);
    }
}