using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum EnemyType
    {
        Boss,
        BossCore
    }

    public enum BossAttack
    {
        Dash,
        EnergyBalls,
        Laser,
        Rays,
        BlackHole
    }

    [System.Serializable]
    public class BossAttackWeight
    {
        public BossAttack attack;
        [Range(0f, 1f)]
        public float weight;
    }

    [Header("Tipo de enemigo")]
    public EnemyType enemyType;

    [Header("Vidas")]
    public float maxHealth = 100f;
    float currentHealth;

    float healthPercent;

    [Header("Delay inicial del Boss")]
    public float attackLockTime = 5f;
    bool attacksLocked = true;

    [Header("Mapa cíclico")]
    float minX, maxX, minY, maxY;
    float bossRadius;

    [Header("Ruta de movimiento")]
    public Transform[] pathPoints;
    public float pathSpeed = 1f;
    public float pointReachDistance = 0.2f;
    public float rotationSpeed = 35f;

    int currentPathIndex;

    [Header("Decisión de ataque")]
    public float attackDecisionCooldown = 3f;
    float attackDecisionTimer;

    [Header("Probabilidad de patrones")]
    public BossAttackWeight[] phase1Weights;
    public BossAttackWeight[] phase2Weights;
    public BossAttackWeight[] phase3Weights;
    public BossAttackWeight[] phase4Weights;

    [Header("Configuración de Dash")]
    public float dashForce = 20f;
    public float dashDuration = 0.5f;

    bool isDashing;
    float dashTimer;
    Vector2 dashDirection;

    [Header("Configuración de bola de energía")]
    public GameObject energyBallPrefab;
    public float energyBallSpeed = 10f;

    public float energyBallAttackDuration = 6f;
    public float energyBallFireRate = 0.3f;
    public float energyBallAngleStep = 5f;

    bool isEnergyBallAttacking;
    float energyBallAttackTimer;
    float energyBallFireTimer;
    float energyBallAngleOffset;

    [Header("Configuración de láser")]
    public GameObject laserPrefab;
    public float laserDuration = 5f;
    public float laserLength = 2.5f;

    GameObject[] activeLasers;
    float laserTimer;
    bool isFiringLaser;

    public float laserWarningTime = 1.35f;
    float laserWarningTimer;
    bool laserInWarning;

    [Header("Configuración de rayos")]
    public GameObject bossRayPrefab;
    public float rayLength = 1f;
    public float rayAngleOffsetMin = 15f;
    public float rayAngleOffsetMax = 45f;

    public int rayRepeats = 5;
    public float rayRepeatDelay = 1f;

    bool isFiringRays;

    [Header("Configuración de agujero negro")]
    public GameObject blackHolePrefab;
    public Transform mouthPoint;
    public float blackHoleDelay = 2f;
    public int blackHoleCount = 3;


    SpriteRenderer sr;
    Color originalColor;
    float hitFlashTimer;
    const float HIT_FLASH_DURATION = 0.1f;

    class DotInstance
    {
        public float dps;
        public float timeLeft;

        public DotInstance(float dps, float duration)
        {
            this.dps = dps;
            this.timeLeft = duration;
        }
    }

    List<DotInstance> activeDots = new();
    UIController uiController;

    Transform player;
    Rigidbody2D rb;


    void Awake()
    {
        // Obtener referencias de RigidBody y SpriteRenderer
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // Color inicial (utilizaremos para hacer hitflash)
        originalColor = sr.color;

        // Buscar UIController en la escena
        uiController = FindObjectOfType<UIController>();
    }

    void Start()
    {
        // Definir la vida del enemigo
        currentHealth = maxHealth;

        if (enemyType == EnemyType.Boss)
        {
            // Busca el Player
            player = GameObject.FindGameObjectWithTag("Player").transform;

            // Calcular los límites de la mapa
            CalculateBounds();
            CalculateBossRadius();

            // Inicializar el láser (un ataque del Boss)
            InitLaserObjects();

            // Mostrar la barra de vida del Boss
            uiController = FindObjectOfType<UIController>();
            if (uiController != null)
            {
                uiController.ShowBossHealth(maxHealth);
            }

            // Bloquear ataques del Boss cierto tiempo cuando empieza el juego
            attacksLocked = true;
            Invoke(nameof(UnlockBossAttacks), attackLockTime);
        }

        // Reinicia el temporizador de decisión de ataque
        attackDecisionTimer = attackDecisionCooldown;
    }

    void Update()
    {
        UpdateDots(); // Aplicar daños de dot
        UpdateHitFlash(); // Gestionar el parpadeo de daño

        if (enemyType == EnemyType.Boss)
        {
            // Si el ataque está bloqueado, no hace nada
            if (attacksLocked)
                return;

            if (!IsBossBusy())
            {
                // Ejecuta el temporizador del ataque cuando el Boss no está atacando
                attackDecisionTimer -= Time.deltaTime;

                // Cuando el temporizador llega a 0, elige un ataque y se ejecuta
                if (attackDecisionTimer <= 0f)
                {
                    BossAttack next = DecideNextAttack();
                    ExecuteAttack(next);

                    // Resetea el temporizador después de atacar
                    attackDecisionTimer = attackDecisionCooldown;
                }
            }

            // Actualiza el estado de láser
            if (laserInWarning || isFiringLaser)
                UpdateLaser();
        }
    }

    void FixedUpdate()
    {
        // No hace nada si no es Boss
        if (enemyType != EnemyType.Boss)
            return;

        // Actualiza el Dash del Boss y aplicar el mecanismo del mapa cíclico
        if (isDashing)
        {
            UpdateDash();
            HandleBossWrap();
            return;
        }

        // Movimiento del Boss según la ruta y orientando hacia el jugador
        BossPathMovement();
        BossRotateToPlayer();

        HandleBossWrap(); // Aplicar el mecanismo del mapa cíclico

        // Actualizar el patrón de bola de energía
        if (isEnergyBallAttacking)
        {
            UpdateEnergyBallAttack();
            HandleBossWrap();
            return;
        }
    }

    // Calcula el porcentaje de la vida restante
    float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }

    // Recibir daño
    public void TakeDamage(float amount)
    {
        // Restar vida y hacer un hitflash
        currentHealth -= amount;
        TriggerHitFlash();

        // Muere cuando su vida baja a 0
        if (currentHealth <= 0f)
            Die();

        // En el caso de Boss, actualiza su barra de vida
        if (enemyType == EnemyType.Boss && uiController != null)
        {
            uiController.UpdateBossHealth(currentHealth);
        }
    }

    // Aplicar el daño de dot (daño por segundo)
    public void ApplyDot(float dps, float duration)
    {
        for (int i = 0; i < activeDots.Count; i++)
        {
            activeDots[i].dps = Mathf.Max(activeDots[i].dps, dps);
            activeDots[i].timeLeft = Mathf.Max(activeDots[i].timeLeft, duration);
        }

        activeDots.Add(new DotInstance(dps, duration));
    }

    // Actualizar el daño de dot
    void UpdateDots()
    {
        // No hace nada si no ha recibido efecto dot
        if (activeDots.Count == 0) return;

        bool tookDamage = false;

        for (int i = activeDots.Count - 1; i >= 0; i--)
        {
            DotInstance dot = activeDots[i];

            // Restar vida cada frame
            currentHealth -= dot.dps * Time.deltaTime;
            dot.timeLeft -= Time.deltaTime;
            tookDamage = true;

            // Quita el efecto de dot cuando se acaba el tiempo
            if (dot.timeLeft <= 0f)
                activeDots.RemoveAt(i);

            // Muere si su vida baja a 0
            if (currentHealth <= 0f)
            {
                Die();
                return;
            }
        }

        // Añadir hitflash por el dot
        if (tookDamage)
        {
            TriggerHitFlash();
        }

        // Actualizar la barra de vida del Boss
        if (tookDamage && enemyType == EnemyType.Boss && uiController != null)
        {
            uiController.UpdateBossHealth(currentHealth);
        }
    }

    // Efecto hitflash
    void TriggerHitFlash()
    {
        sr.color = Color.red;
        hitFlashTimer = HIT_FLASH_DURATION;
    }

    // Acabar el hitflash
    void UpdateHitFlash()
    {
        if (hitFlashTimer <= 0f)
            return;

        hitFlashTimer -= Time.deltaTime;

        if (hitFlashTimer <= 0f)
            sr.color = originalColor;
    }

    // Comportamiento del enemigo cuando se muere
    void Die()
    {
        switch (enemyType)
        {
            // Si el Boss se muere, se finaliza el juego
            case EnemyType.Boss:
                if (uiController != null)
                { 
                    uiController.HideBossHealth();
                    uiController.ShowEndGame();
                }

                Destroy(gameObject);
                break;

            // Si el núcleo se muere, el jugador puede elegir nuevo arma
            case EnemyType.BossCore:
                uiController.ShowWeaponHint();;
                Destroy(gameObject);
                break;
        }
    }

    // Desbloquear el ataque del Boss
    void UnlockBossAttacks()
    {
        attacksLocked = false;
        attackDecisionTimer = attackDecisionCooldown;
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

    // Calcular el tamaño del Boss para evitar teletransportación extraña
    void CalculateBossRadius()
    {
        bossRadius = GetComponent<SpriteRenderer>().bounds.extents.x;
    }

    // Aplicar el wrap-around al mapa
    void HandleBossWrap()
    {
        Vector3 pos = transform.position;

        if (pos.x > maxX + bossRadius)
            pos.x = minX - bossRadius;
        else if (pos.x < minX - bossRadius)
            pos.x = maxX + bossRadius;

        if (pos.y > maxY + bossRadius)
            pos.y = minY - bossRadius;
        else if (pos.y < minY - bossRadius)
            pos.y = maxY + bossRadius;

        transform.position = pos;
    }

    // Probabilidades de cada ataque según las vidas restantes del Boss
    BossAttackWeight[] GetCurrentAttackWeights()
    {
        float hp = GetHealthPercent();

        if (hp > 0.75f)
            return phase1Weights;
        else if (hp > 0.50f)
            return phase2Weights;
        else if (hp > 0.25f)
            return phase3Weights;
        else
            return phase4Weights;
    }

    // Seleccionar un patrón de ataque según las probabilidades
    BossAttack DecideNextAttack()
    {
        BossAttackWeight[] weights = GetCurrentAttackWeights();

        float total = 0f;
        foreach (var w in weights)
            total += w.weight;

        float random = Random.Range(0f, total);

        float current = 0f;
        foreach (var w in weights)
        {
            current += w.weight;
            if (random <= current)
                return w.attack;
        }

        return BossAttack.EnergyBalls;
    }

    // Ejecutar el ataque
    void ExecuteAttack(BossAttack attack)
    {
        switch (attack)
        {
            case BossAttack.EnergyBalls:
                StartEnergyBallAttack();
                break;

            case BossAttack.Laser:
                FireLaser4Directions();
                break;

            case BossAttack.Dash:
                StartDash();
                break;

            case BossAttack.Rays:
                StartRayAttack();
                break;

            case BossAttack.BlackHole:
                StartCoroutine(BlackHoleAttack());
                break;
        }
    }

    // Activar dash y reproduce el sonido
    void StartDash()
    {
        MusicManager.Instance.PlaySFX(
            MusicManager.SFXType.BossDash
        );

        isDashing = true;
        dashTimer = dashDuration;

        // Dirección actual del boss
        dashDirection = -transform.up;

        rb.velocity = Vector2.zero;
    }

    // Mantener el dash hacia una dirección
    void UpdateDash()
    {
        dashTimer -= Time.fixedDeltaTime;

        // Aplicar una fuerza hacia la dirección
        rb.velocity = dashDirection * dashForce;

        // Cuando el timer llega a 0, se detiene el dash
        if (dashTimer <= 0f)
        {
            rb.velocity = Vector2.zero;
            isDashing = false;
        }
    }

    // Crear una bola de energía en la posición del Boss e inicializa en una dirección
    void FireEnergyBallInDirection(Vector2 dir)
    {
        GameObject ball = Instantiate(
            energyBallPrefab,
            transform.position,
            Quaternion.identity
        );

        Bullet bullet = ball.GetComponent<Bullet>();
        bullet.owner = BulletOwner.Enemy;
        bullet.Init(dir);
    }

    // Actualizar los parámetros relacionados y reproduce el sonido
    void StartEnergyBallAttack()
    {
        isEnergyBallAttacking = true;
        energyBallAttackTimer = energyBallAttackDuration;
        energyBallFireTimer = 0f;
        energyBallAngleOffset = 0f;

        MusicManager.Instance.PlaySFXLoop(
            MusicManager.SFXType.BossEnergyBall
        );
    }

    // Ataque de bola de energía
    void UpdateEnergyBallAttack()
    {
        // Decrementando los timers
        energyBallAttackTimer -= Time.fixedDeltaTime;
        energyBallFireTimer -= Time.fixedDeltaTime;

        // Disparar la bola a 4 direcciones con un offset variable
        if (energyBallFireTimer <= 0f)
        {
            FireEnergyBallWithOffset();
            energyBallFireTimer = energyBallFireRate;

            // El offset va cambiando poco a poco cada vez que dispara
            energyBallAngleOffset += energyBallAngleStep;
        }

        // Cuando el ataque se termina, actualiza el paramétro asociado y para el sonido
        if (energyBallAttackTimer <= 0f)
        {
            isEnergyBallAttacking = false;
            MusicManager.Instance.StopLoopSFX(
                MusicManager.SFXType.BossEnergyBall
            );
        }
    }

    // Disparar la bola de energía a 4 direcciones con un offset
    void FireEnergyBallWithOffset()
    {
        FireEnergyBallInDirection(RotateVector(transform.up, energyBallAngleOffset));
        FireEnergyBallInDirection(RotateVector(-transform.up, energyBallAngleOffset));
        FireEnergyBallInDirection(RotateVector(transform.right, energyBallAngleOffset));
        FireEnergyBallInDirection(RotateVector(-transform.right, energyBallAngleOffset));
    }

    // Aplicar una rotación
    Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);

        return new Vector2(
            cos * v.x - sin * v.y,
            sin * v.x + cos * v.y
        );
    }

    // Crear 4 láseres como hijos del Boss
    void InitLaserObjects()
    {
        activeLasers = new GameObject[4];

        for (int i = 0; i < 4; i++)
        {
            GameObject laser = Instantiate(laserPrefab, transform);
            laser.SetActive(false);
            activeLasers[i] = laser;
        }
    }

    // Dispara láseres a 4 direcciones
    void FireLaser4Directions()
    {
        // Activar todos los láseres y empezar el ataque warning
        foreach (var laser in activeLasers)
        {
            laser.gameObject.SetActive(true);
            laser.GetComponent<BossLaser>().StartWarning();
        }

        // Si ya está ejecuntando el láser, no hace nada
        if (isFiringLaser || laserInWarning)
            return;

        laserInWarning = true;
        laserWarningTimer = laserWarningTime;

        // Asignar direcciones de cada láser
        Vector2[] dirs =
        {
            transform.up,
            -transform.up,
            transform.right,
            -transform.right
        };

        // Ajustar la posición y rotación de cada láser
        for (int i = 0; i < activeLasers.Length; i++)
        {
            GameObject laser = activeLasers[i];
            laser.SetActive(true);

            laser.transform.localPosition = Vector3.zero;
            laser.transform.up = dirs[i];

            // Escalar el láser para que complete toda la pantalla
            laser.transform.localScale = new Vector3(
                laserLength,
                laser.transform.localScale.y,
                laser.transform.localScale.z
            );
        }
    }

    // Ataque de láser
    void UpdateLaser()
    {
        // Fase de warning
        if (laserInWarning)
        {
            laserWarningTimer -= Time.deltaTime;

            // Una vez avisada, ejecuta el láser de verdad y reproduce el sonido
            if (laserWarningTimer <= 0f)
            {
                laserInWarning = false;
                isFiringLaser = true;
                laserTimer = laserDuration;

                    MusicManager.Instance.PlaySFXLoop(
                        MusicManager.SFXType.BossLaser
                    );

                foreach (var laser in activeLasers)
                    laser.GetComponent<BossLaser>().StartFiring();
            }
            return;
        }

        // Fase de disparo
        if (isFiringLaser)
        {
            laserTimer -= Time.deltaTime;

            // Termina el ataque cuando se termine el temporizador
            if (laserTimer <= 0f)
                StopLaser();
        }
    }

    // Desactiva el ataque láser y para el sonido
    void StopLaser()
    {
        isFiringLaser = false;

        MusicManager.Instance.StopLoopSFX(
            MusicManager.SFXType.BossLaser
        );

        foreach (var laser in activeLasers)
        {
            laser.GetComponent<BossLaser>().ResetLaser();
        }
    }

    // Configurar los ángulos de 3 rayos
    void FireRays()
    {
        if (player == null)
            return;

        // Calcula la dirección base hacia el jugador
        Vector2 baseDir = (player.position - transform.position).normalized;

        // Rayo central (directo al jugador)
        SpawnRay(baseDir);

        // Rayo izquierdo (offset negativo aleatorio)
        float offset1 = Random.Range(rayAngleOffsetMin, rayAngleOffsetMax);
        SpawnRay(RotateVector(baseDir, -offset1));

        // Rayo derecho (offset positivo aleatorio)
        float offset2 = Random.Range(rayAngleOffsetMin, rayAngleOffsetMax);
        SpawnRay(RotateVector(baseDir, offset2));
    }

    // Generar el rayo y reproducir el sonido
    void SpawnRay(Vector2 dir)
    {
        GameObject rayObj = Instantiate(
            bossRayPrefab,
            transform.position,
            Quaternion.identity
        );

        BossRay ray = rayObj.GetComponent<BossRay>();
        ray.Init(dir, rayLength);

        MusicManager.Instance.PlaySFX(
            MusicManager.SFXType.BossRays
        );
    }

    // Inicia la corrutina de ataque de rayos
    void StartRayAttack()
    {
        isFiringRays = true;

        StartCoroutine(RayAttackRoutine());

    }

    // Corrutina de ataque de rayos
    IEnumerator RayAttackRoutine()
    {
        // Dispara 3 rayos repitiendo varias veces con un delay
        for (int i = 0; i < rayRepeats; i++)
        {
            FireRays();
            yield return new WaitForSeconds(rayRepeatDelay);
        }

        isFiringRays = false;
    }

    // Corrutina del ataque de agujero negro
    IEnumerator BlackHoleAttack()
    {
        // Generar agujero negro varias veces con un delay y reproduce el sonido
        for (int i = 0; i < blackHoleCount; i++)
        {
            Instantiate(
                blackHolePrefab,
                mouthPoint.position,
                Quaternion.identity
            );

            MusicManager.Instance.PlaySFXLoop(
                MusicManager.SFXType.BossBlackHole
            );

            yield return new WaitForSeconds(blackHoleDelay);
        }

        // Parar el sonido cuando se termina el ataque
        MusicManager.Instance.StopLoopSFX(
            MusicManager.SFXType.BossBlackHole
        );
    }

    // Movimiento del Boss según la ruta
    void BossPathMovement()
    {
        if (pathPoints == null || pathPoints.Length == 0)
            return;

        Transform targetPoint = pathPoints[currentPathIndex];

        // Se mueve hacia el punto definido
        Vector2 newPos = Vector2.MoveTowards(
            rb.position,
            targetPoint.position,
            pathSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(newPos);

        // Cuando llega al punto definido, avanza al siguiente punto
        float distance = Vector2.Distance(rb.position, targetPoint.position);

        if (distance <= pointReachDistance)
        {
            currentPathIndex++;
            if (currentPathIndex >= pathPoints.Length)
                currentPathIndex = 0;
        }
    }

    // Calcula el vector hacia el jugador
    void BossRotateToPlayer()
    {
        if (player == null)
            return;

        Vector2 dir = (player.position - transform.position).normalized;

        float targetAngle =
            Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;

        float angle = Mathf.MoveTowardsAngle(
            rb.rotation,
            targetAngle,
            rotationSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(angle);
    }

    // Comprobar si el Boss está atacando o no
    bool IsBossBusy()
    {
        if (isEnergyBallAttacking)
            return true;

        if (isFiringLaser)
            return true;

        if (isFiringRays)
            return true;

        if (isDashing)
            return true;

        // No hemos puesto agujero negro a propósito para hacer combo con otros ataques
        return false;
    }
}