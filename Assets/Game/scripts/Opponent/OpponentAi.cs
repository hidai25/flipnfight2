using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController), typeof(Animator))]
public class OpponentAI : MonoBehaviour
{
    [System.Serializable]
    public class AttackMove
    {
        public string animationName = "AttackAnimation";
        public float range = 2.5f;
        public ParticleSystem effect;
        internal int attackDamages;
    }

    [Header("Opponent Movement")]
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float minDistanceToPlayer = 2f;
    [SerializeField] private float maxDistanceToPlayer = 4f;
    [SerializeField] private float gravity = 9.81f;
    public CharacterController characterController;

    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private LayerMask playerLayer;
    public FightingController fightingController;
    public int attackCount = 0;
    public int randomNumber;
    public float dodgeDistance = 2f;
    public int attackDamages = 10;
    public float attackRadius = 2.2f;

    [SerializeField]
    private AttackMove[] attacks = new AttackMove[]
    {
        new AttackMove { animationName = "Attack1Animation", attackDamages = 10, range = 2.5f },
        new AttackMove { animationName = "Attack2Animation", attackDamages = 15, range = 2.5f },
        new AttackMove { animationName = "Attack3Animation", attackDamages = 20, range = 2.5f },
        new AttackMove { animationName = "Attack4Animation", attackDamages = 25, range = 2.5f }
    };
    public string[] attackAnimations = { "Attack1Animation", "Attack2Animation", "Attack3Animation", "Attack4Animation" };

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Animation Settings")]
    [SerializeField] private string hitAnimationName = "HitDamageAnimation";
    [SerializeField] private string deathAnimationName = "Die";
    [SerializeField] private float hitAnimationDuration = 0.5f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem hitEffect;
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private AudioClip deathSound;

    [Header("References")]
    [SerializeField] private Transform playerTarget;

    // Components
    private CharacterController controller;
    public Animator animator;
    private AudioSource audioSource;

    // Animation Hash IDs
    private readonly int WalkingParam = Animator.StringToHash("IsWalking");
    private readonly int AttackingParam = Animator.StringToHash("IsAttacking");
    private readonly int HitTrigger = Animator.StringToHash("Hit");
    private readonly int DeathTrigger = Animator.StringToHash("Death");

    // State
    private bool isAttacking;
    public bool isTakingDamage;
    private bool isDead;
    private float lastAttackTime;
    private Vector3 velocity;

    private void Start()
    {
        InitializeComponents();
        SetupInitialState();
        ValidateAttacks();
        FindPlayer();

        // Verify animator setup
        if (animator)
        {
            AnimatorStateInfo[] states = new AnimatorStateInfo[animator.layerCount];
            for (int i = 0; i < animator.layerCount; i++)
            {
                states[i] = animator.GetCurrentAnimatorStateInfo(i);
                Debug.Log($"[{gameObject.name}] Layer {i} has states: {states[i].shortNameHash}");
            }
            Debug.Log($"[{gameObject.name}] Damage animation state exists: {animator.HasState(0, Animator.StringToHash("hitDamageAnimation"))}");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] No animator component found!");
        }
    }

    void Awake()
    {
        createRandomNumber();
    }

    private void Update()
    {

        // Add this at the start of Update
        if (Input.GetKeyDown(KeyCode.R))  // 'R' for reset
        {
            ResetDamageState();
        }
        // Test key for damage animation
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log($"[{gameObject.name}] Testing damage animation");
            TakeDamage(10);
            return;
        }

        // Original behavior checks
        if (isDead || isTakingDamage || !playerTarget) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // If within attack range
        if (distanceToPlayer <= attackRadius)
        {
            // Stop moving and face the player
            animator.SetBool("Walking", false);
            FaceTarget();

            // Check if we can attack
            if (Time.time - lastAttackTime > attackCooldown && !isTakingDamage && !isAttacking)
            {
                PerformRandomAttack();
            }
        }
        // If outside attack range, move towards player
        else
        {
            MoveTowardsPlayer();
        }

        // Apply gravity
        ApplyGravity();
    }

    private void MoveTowardsPlayer()
    {
        if (!playerTarget) return;

        Vector3 direction = (playerTarget.position - transform.position).normalized;
        direction.y = 0; // Keep movement on the horizontal plane

        // Move towards player
        characterController.Move(direction * movementSpeed * Time.deltaTime);

        // Rotate towards player
        FaceTarget();

        // Set walking animation
        animator.SetBool("Walking", true);
    }

    private void FaceTarget()
    {
        if (!playerTarget) return;

        Vector3 direction = (playerTarget.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void PerformRandomAttack()
    {
        int randomAttackIndex = Random.Range(0, attackAnimations.Length);
        PerformAttack(randomAttackIndex);
    }

    void PerformAttack(int attackIndex)
    {
        if (isAttacking) return;

        isAttacking = true;
        animator.Play(attackAnimations[attackIndex]);
        int damage = attacks[attackIndex].attackDamages;
        Debug.Log("im here");
        Debug.Log($"[{gameObject.name}] Performed Attack {attackIndex + 1} dealing {damage} damage");
        lastAttackTime = Time.time;

        // Only trigger the damage animation when actually attacking
        if (fightingController != null)
        {
            fightingController.StartCoroutine(fightingController.PlayHitDamageAnimation(damage));
        }

        // Reset attack state after animation duration (approximately)
        StartCoroutine(ResetAttackState(1f));
    }

    private IEnumerator ResetAttackState(float delay)
    {
        yield return new WaitForSeconds(delay);
        isAttacking = false;
    }

    private void ApplyGravity()
    {
        if (!characterController.isGrounded)
        {
            velocity.y -= gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }
        else
        {
            velocity.y = -0.5f;
        }
    }

    private void InitializeComponents()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        characterController = GetComponent<CharacterController>();
    }

    private void FindPlayer()
    {
        if (!playerTarget)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player)
            {
                playerTarget = player.transform;
                Debug.Log($"[{gameObject.name}] Found player target");
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] No player found in scene!");
            }
        }
    }

    private void SetupInitialState()
    {
        currentHealth = maxHealth;
        isAttacking = false;
        isTakingDamage = false;
        isDead = false;
        lastAttackTime = -attackCooldown;
        velocity = Vector3.zero;
    }

    private void ValidateAttacks()
    {
        if (attacks == null || attacks.Length == 0)
        {
            Debug.LogError($"[{gameObject.name}] Initializing default attacks!");
            attacks = new AttackMove[]
            {
                new AttackMove { animationName = "Attack1Animation", attackDamages = 10, range = 2.5f },
                new AttackMove { animationName = "Attack2Animation", attackDamages = 15, range = 2.5f },
                new AttackMove { animationName = "Attack3Animation", attackDamages = 20, range = 2.5f },
                new AttackMove { animationName = "Attack4Animation", attackDamages = 25, range = 2.5f }
            };
        }

        for (int i = 0; i < attacks.Length; i++)
        {
            if (attacks[i] == null)
            {
                attacks[i] = new AttackMove
                {
                    animationName = $"Attack{i + 1}Animation",
                    attackDamages = 10 + (i * 5),
                    range = 2.5f
                };
            }
        }
    }

    void createRandomNumber()
    {
        randomNumber = Random.Range(1, 5);
    }

    public void TakeDamage(int damage)
    {
        Debug.Log($"[{gameObject.name}] TakeDamage called with damage: {damage}");

        // Check if we can take damage
        if (isDead || isTakingDamage)
        {
            Debug.Log($"[{gameObject.name}] Cannot take damage - isDead: {isDead}, isTakingDamage: {isTakingDamage}");
            return;
        }

        isTakingDamage = true;

        // Apply damage
        currentHealth -= damage;
        Debug.Log($"[{gameObject.name}] Health reduced to: {currentHealth}/{maxHealth}");

        // Cancel any current attack
        if (isAttacking)
        {
            isAttacking = false;
            StopCoroutine("ResetAttackState");
            Debug.Log($"[{gameObject.name}] Cancelled current attack");
        }

        // Play hit effect if assigned
        if (hitEffect)
        {
            hitEffect.Stop();
            hitEffect.Play();
            Debug.Log($"[{gameObject.name}] Playing hit effect");
        }

        // Play hit sound if assigned
        if (audioSource && hitSounds != null && hitSounds.Length > 0)
        {
            AudioClip hitSound = hitSounds[Random.Range(0, hitSounds.Length)];
            audioSource.PlayOneShot(hitSound);
            Debug.Log($"[{gameObject.name}] Playing hit sound");
        }

        // Check for death
        if (currentHealth <= 0)
        {
            Debug.Log($"[{gameObject.name}] Health depleted, calling Die()");
            Die();
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Starting damage animation");
            StartCoroutine(PlayHitDamageAnimation(damage));
        }
    }





    public IEnumerator PlayHitDamageAnimation(int takedamage)
    {
        Debug.Log($"[{gameObject.name}] PlayHitDamageAnimation started");

        // Reset isTakingDamage if it's been stuck for too long (failsafe)
        if (isTakingDamage && Time.time - lastAttackTime > hitAnimationDuration * 2)
        {
            Debug.Log($"[{gameObject.name}] Resetting stuck damage state");
            isTakingDamage = false;
        }

        if (isTakingDamage)
        {
            Debug.Log($"[{gameObject.name}] Already taking damage, skipping animation");
            yield break;
        }

        isTakingDamage = true;

        // Add initial delay to wait for player's attack animation to connect
        float initialDelay = 0.3f; // Adjust this value based on your player's attack animation
        yield return new WaitForSeconds(initialDelay);

        // Stop any current movement animation
        animator.SetBool("Walking", false);

        Debug.Log($"[{gameObject.name}] Playing hitDamageAnimation");
        // Play the hit damage animation
        animator.Play(hitAnimationName, 0, 0);  // Force animation to start from beginning

        // Set the trigger
        animator.SetTrigger(HitTrigger);

        Debug.Log($"[{gameObject.name}] Waiting for {hitAnimationDuration} seconds");
        yield return new WaitForSeconds(hitAnimationDuration);

        Debug.Log($"[{gameObject.name}] Animation complete, resetting state");
        isTakingDamage = false;  // Make sure this gets set to false

        // Resume walking animation if appropriate
        if (!isDead && Vector3.Distance(transform.position, playerTarget.position) > attackRadius)
        {
            Debug.Log($"[{gameObject.name}] Resuming walking animation");
            animator.SetBool("Walking", true);
        }

        Debug.Log($"[{gameObject.name}] Hit animation sequence completed");
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log($"[{gameObject.name}] Died!");

        isAttacking = false;
        isTakingDamage = false;
        controller.enabled = false;

        if (audioSource && deathSound)
        {
            audioSource.PlayOneShot(deathSound);
        }

        animator.SetTrigger(DeathTrigger);
        animator.Play(deathAnimationName);

        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }


    public void ResetDamageState()
    {
        isTakingDamage = false;
        Debug.Log($"[{gameObject.name}] Damage state manually reset");
    }
    // Animation event callbacks
    public void OnAttackStart()
    {
        Debug.Log($"[{gameObject.name}] Attack started");
    }

    public void OnAttackEnd()
    {
        Debug.Log($"[{gameObject.name}] Attack ended");
        isAttacking = false;
    }

    // Special attack effects
    public void Attack1spEffect()
    {
        if (attacks.Length > 0 && attacks[0].effect != null)
            attacks[0].effect.Play();
    }

    public void Attack2spEffect()
    {
        if (attacks.Length > 1 && attacks[1].effect != null)
            attacks[1].effect.Play();
    }

    public void Attack3spEffect()
    {
        if (attacks.Length > 2 && attacks[2].effect != null)
            attacks[2].effect.Play();
    }

    public void Attack4spEffect()
    {
        if (attacks.Length > 3 && attacks[3].effect != null)
            attacks[3].effect.Play();
    }
}