using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Mapa cíclico")]
    private float minX, maxX, minY, maxY;
    
    [Header("Movimiento")]
    public float baseThrust = 10f;
    public float maxThrust = 30f;
    public float accelerationRate = 12f;

    [Header("Rotación")]
    public float rotationSpeed = 270f;

    [Header("Efecto propulsión")]
    public ParticleSystem engineParticles;

    [Header("Vidas")]
    public float maxHealth = 250f;
    private float currentHealth;

    [Header("Regeneración de vida")]
    public float regenDelay = 2.5f;
    public float regenRate = 30f;

    [Header("UI")]
    public UIController uiController;

    [Header("Colisión")]
    public float knockbackForce = 5f;
    public float collisionDamage = 20f;


    private Rigidbody2D rb;
    private float playerRadius;
    private float currentThrust;

    private SpriteRenderer sr;
    private Color originalColor;
    private Coroutine hitFlashCoroutine;

    private float lastDamageTime;

    private bool isInvulnerable;
    public float invulnerableTime = 0.05f;

    private bool isDead;


    void Awake()
    {
        // Obtener referencias de RigidBody y SpriteRenderer
        rb = GetComponent<Rigidbody2D>(); 
        sr = GetComponent<SpriteRenderer>();

        // Color inicial (utilizaremos para hacer hitflash)
        originalColor = sr.color;
    }

    void Start()
    {
        // Comenzar con la fuerza mínima de movimiento
        currentThrust = baseThrust;

        // Definir la vida y mostrar con UI
        currentHealth = maxHealth;
        uiController.UpdateHealth(currentHealth, maxHealth);

        // Calcular los límites de la mapa
        CalculateBounds();
        CalculatePlayerRadius();
    }

    void Update()
    {
        // Si el jugador está muerto, no hace nada
        if (isDead)
            return;

        RotateToMouse(); // Para controlar la orientación con el ratón
        HandleHealthRegen(); // Recuperar vidas
    }

    void FixedUpdate()
    {
        HandleMovement(); // Aplicar fuerzas de movimiento
        HandleWrapAround(); // Conseguir el mapa cíclico
    }

    // Rotación del jugador con el ratón
    void RotateToMouse()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector2 direction = mouseWorldPos - transform.position;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    // Movimiento del jugador con el teclado
    void HandleMovement()
    {
        // Consigue propulsion cuando pulsamos la tecla espaciadora
        if (Input.GetKey(KeyCode.Space))
        {
            // Aumentar progresivamente la fuerza de propulsión
            currentThrust += accelerationRate * Time.fixedDeltaTime;
            currentThrust = Mathf.Min(currentThrust, maxThrust);

            rb.AddForce(transform.up * currentThrust);

            // Activar el efecto de propulsión
            if (!engineParticles.isPlaying)
                engineParticles.Play();
        }
        else
        {
            // Resetear de aceleración al soltar
            currentThrust = baseThrust;

            // Desactiva el efecto de propulsión al soltar
            if (engineParticles.isPlaying)
                engineParticles.Stop();
        }
    }

    // Calcular los límites del mapa con la cámara
    void CalculateBounds()
    {
        Camera cam = Camera.main;

        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        minX = -camWidth;
        maxX = camWidth;
        minY = -camHeight;
        maxY = camHeight;
    }

    // Calcular el tamaño del jugador para evitar teletransportación extraña
    void CalculatePlayerRadius()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        playerRadius = sr.bounds.extents.x;
    }

    // Aplicar el wrap-around al mapa
    void HandleWrapAround()
    {
        Vector3 pos = transform.position;

        if (pos.x > maxX + playerRadius)
            pos.x = minX - playerRadius;
        else if (pos.x < minX - playerRadius)
            pos.x = maxX + playerRadius;

        if (pos.y > maxY + playerRadius)
            pos.y = minY - playerRadius;
        else if (pos.y < minY - playerRadius)
            pos.y = maxY + playerRadius;

        transform.position = pos;
    }

    // Mostrar GameOver y desactivar el jugador
    void Die()
    {
        uiController.ShowGameOver();
        gameObject.SetActive(false);
    }

    // Recibir daño
    public void TakeDamage(float damage)
    {
        // No hace nada si el jugador está muerto o está invulnerable
        if (isDead)
            return;
        if (isInvulnerable)
            return;

        lastDamageTime = Time.time; // Sirve para recuperar vida

        // Restar vida y actualiza la barra de vida del jugador
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        uiController.UpdateHealth(currentHealth, maxHealth);

        // Comprobar si el jugador está muerto o no después de cada daño
        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        // Hacer un hitflash como feedback
        if (hitFlashCoroutine != null)
            StopCoroutine(hitFlashCoroutine);
        hitFlashCoroutine = StartCoroutine(HitFlash());
        StartCoroutine(Invulnerability());
    }

    // Corrutina de hitflash
    IEnumerator HitFlash()
    {
        sr.color = new Color(1f, 0.5f, 0.5f);

        yield return new WaitForSeconds(0.05f);
        sr.color = originalColor;
    }

    // Corrutina de invulnerabilidad después de recibir daño
    IEnumerator Invulnerability()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerableTime);
        isInvulnerable = false;
    }

    // Regeneración de vida
    void HandleHealthRegen()
    {
        // No recupera si el jugador está muerto o si tiene vida máxima
        if (currentHealth <= 0f)
            return;
        if (currentHealth >= maxHealth)
            return;

        // Comprobar el tiempo de recibir el último daño
        if (Time.time - lastDamageTime < regenDelay)
            return;

        // Regenerar la vida poco a poco y actualiza la barra de vida del jugador
        currentHealth += regenRate * Time.deltaTime;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        uiController.UpdateHealth(currentHealth, maxHealth);
    }

    // Controla la colisión con los enemigos
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Comprobar si es enemigo
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy == null)
            return;

        // Calcula dirección de knockback y hacer una fuerza de empuje al jugador
        Vector2 knockDir = (transform.position - collision.transform.position).normalized;
        rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);

        // Hacer el daño de colisión al ambos
        TakeDamage(collisionDamage);
        enemy.TakeDamage(collisionDamage);
    }
}
