using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FightingController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = 9.81f;

    [Header("Combat")]
    public float attackCooldown = 0.5f;
    public int attackDamages = 5;
    public string[] attackAnimations = { "Attack1Animation", "Attack2Animation", "Attack3Animation", "Attack4Animation" };
    public float dodgeDistance = 2f;
    public float attackRadius = 2.2f;
    public Transform[] opponents;
    public float dodgeCooldown = 1f; // Add cooldown for dodge
    private float lastDodgeTime; // Track last dodge time

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    public HealthBar healthBar;
    [SerializeField] private string hitAnimationName = "GetHit";
    [SerializeField] private float hitAnimationDuration = 0.5f;

    [Header("Hit Effects")]
    [SerializeField] private ParticleSystem hitEffect;
    [SerializeField] private AudioClip[] hitSounds;
    private AudioSource audioSource;
    private bool isTakingDamage;



    [Header("Effects and Sound")]
    public ParticleSystem attack1spEffect;
    public ParticleSystem attack2spEffect;
    public ParticleSystem attack3spEffect;
    public ParticleSystem attack4spEffect;

    [Header("UI References")]
    [SerializeField] private Button[] attackButtons;
    [SerializeField] private Sprite[] attackIcons;
    [SerializeField] private Button dodgeButton; // UI Dodge button

    [Header("Input")]
    [SerializeField] private InputActionReference moveActionToUse;

    // Components
    private CharacterController characterController;
    private Animator animator;
    private Camera mainCamera;

    // Runtime variables
    private Vector2 moveInput;
    private Vector3 velocity;
    private float lastAttackTime;
    private bool isDodging = false;

    public int Length { get; internal set; }

 


    private void Awake()
    {
        currentHealth = maxHealth;
        healthBar.GiveFullHealth(currentHealth);
        SetupComponents();
        SetupButtonListeners();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    private void SetupComponents()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();  // Add this line

        // Add AudioSource if it doesn't exist
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;  // Prevent automatic playing
            audioSource.spatialBlend = 1f;    // 3D sound
            audioSource.volume = 1f;          // Full volume
        }

        if (!characterController || !animator || !mainCamera)
        {
            Debug.LogError("Missing required components!");
            enabled = false;
        }

        if (attackButtons == null || attackButtons.Length != 4)
        {
            Debug.LogError("Please assign all 4 attack buttons in the inspector!");
            enabled = false;
        }
    }

    private void SetupButtonListeners()
    {
        for (int i = 0; i < attackButtons.Length; i++)
        {
            if (attackButtons[i] != null)
            {
                int index = i;
                attackButtons[i].onClick.RemoveAllListeners();
                attackButtons[i].onClick.AddListener(() => PerformAttack(index));

                if (attackIcons != null && i < attackIcons.Length && attackIcons[i] != null)
                {
                    Image iconImage = attackButtons[i].transform.GetComponentInChildren<Image>();
                    if (iconImage != null && iconImage.gameObject != attackButtons[i].gameObject)
                    {
                        iconImage.sprite = attackIcons[i];
                        iconImage.preserveAspect = true;
                    }
                }
            }
        }

        // Set up the DodgeButton listener
        if (dodgeButton != null)
        {
            dodgeButton.onClick.RemoveAllListeners();
            dodgeButton.onClick.AddListener(PerformDodge);
        }
    }


    public void TakeDamage(int damage, int takeDamage)
    {
        Debug.Log($"[Player] TakeDamage called with damage: {damage}");
        if (isTakingDamage || currentHealth <= 0)
        {
            Debug.Log("[Player] Already taking damage or dead, ignoring damage call");
            return;
        }

        isTakingDamage = true;

        // Apply damage
        currentHealth -= damage;
        Debug.Log($"[Player] Health reduced to: {currentHealth}/{maxHealth}");

        // Play hit effect if assigned
        if (hitEffect != null)
        {
            Debug.Log("[Player] Playing hit effect");
            hitEffect.Play();
        }
        else
        {
            Debug.Log("[Player] No hit effect assigned");
        }

        // Play hit sound
        if (audioSource != null && hitSounds != null && hitSounds.Length > 0)
        {
            Debug.Log("[Player] Playing hit sound");
            int randomIndex = Random.Range(0, hitSounds.Length);
            audioSource.PlayOneShot(hitSounds[randomIndex]);
        }
        else
        {
            Debug.Log("[Player] No audio source or hit sounds available");
        }

        // Start damage animation coroutine
        Debug.Log("[Player] Starting damage animation coroutine");
        StartCoroutine(PlayHitDamageAnimation(takeDamage));
    }

    //private IEnumerator PlayHitDamageAnimation()
    //{

    //    TakeDamage(10);
    //    Debug.Log("[Player] Beginning damage animation");
    //    animator.Play("HitDamageAnimation");

    //    // Play the damage animation
    //    if (animator != null)
    //    {
    //        Debug.Log($"[Player] Playing animation: {hitAnimationName}");
    //        animator.Play(hitAnimationName, 0, 0);
    //    }
    //    else
    //    {
    //        Debug.LogError("[Player] Animator component is null!");
    //    }

    //    Debug.Log($"[Player] Waiting for animation duration: {hitAnimationDuration}");
    //    yield return new WaitForSeconds(hitAnimationDuration);

    //    // Check for death
    //    if (currentHealth <= 0)
    //    {
    //        Debug.Log("[Player] Health depleted, calling Die()");
    //        Die();
    //    }

    //    Debug.Log("[Player] Resetting isTakingDamage flag");
    //    isTakingDamage = false;
    //}

    //void Die()
    //{
    //    Debug.Log("Player Died");
    //}



    private void OnEnable()
    {
        moveActionToUse.action.Enable();
    }

    private void OnDisable()
    {
        moveActionToUse.action.Disable();
    }

    public void Update()
    {
        PerformMovement();

        //for(int i=0;i<fightingController.Length;i++)
        //{
        //    if(player[i].gameObject.activeself)
        //    {
        //        Vector3 direction = (PlayerInput[i])
        //    }
        //}
        //PerformDodge;
        //if (Input.GetKeyDown(KeyCode.T)) // Press 'T' to test taking damage
        //{
        //    Debug.Log("Testing TakeDamage on key press.");

        //    //TakeDamage(10);
        //}
    }

    private void PerformMovement()
    {
        if (isDodging) return; // Don't allow movement during dodge

        moveInput = moveActionToUse.action.ReadValue<Vector2>();

        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 movement = (forward * moveInput.y + right * moveInput.x);

        if (movement.magnitude > 0.1f)
        {
            movement = movement.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            Vector3 moveDirection = movement * moveSpeed * Time.deltaTime;

            if (characterController.isGrounded)
            {
                velocity.y = 0f;
            }
            velocity.y -= gravity * Time.deltaTime;
            moveDirection.y = velocity.y;

            characterController.Move(moveDirection);
            animator.SetBool("Walking", true);
        }
        else
        {
            animator.SetBool("Walking", false);
            if (!characterController.isGrounded)
            {
                velocity.y -= gravity * Time.deltaTime;
                characterController.Move(new Vector3(0, velocity.y, 0) * Time.deltaTime);
            }
        }
    }


    //void PerformAttack(int attackIndex)
    //{
    //    if (Time.time - lastAttackTime > attackCooldown)
    //    {
    //        animator.Play(attackAnimations[attackIndex]);
    //        int damage = attackDamages;
    //        Debug.Log($"Player performing attack {attackIndex + 1} with {damage} damage");
    //        lastAttackTime = Time.time;

    //        foreach (Transform opponent in opponents)
    //        {
    //            float distance = Vector3.Distance(transform.position, opponent.position);
    //            Debug.Log($"Distance to opponent: {distance}, Attack radius: {attackRadius}");

    //            if (distance <= attackRadius)
    //            {
    //                OpponentAI opponentAI = opponent.GetComponent<OpponentAI>();
    //                if (opponentAI != null)
    //                {
    //                    Debug.Log($"Calling TakeDamage on opponent {opponent.name}");
    //                    opponentAI.TakeDamage(damage);
    //                }
    //                else
    //                {
    //                    Debug.Log($"No OpponentAI component found on {opponent.name}");
    //                }
    //            }
    //        }
    //    }
    //    else
    //    {
    //        Debug.Log("Cannot perform attack yet. Cooldown time remaining: " +
    //                  (attackCooldown - (Time.time - lastAttackTime)));
    //    }
    //}



    void PerformAttack(int attackIndex)
    {
        if (Time.time - lastAttackTime > attackCooldown)
        {
            animator.Play(attackAnimations[attackIndex]);
            int damage = attackDamages;
            Debug.Log($"Player performing attack {attackIndex + 1} with {damage} damage");
            lastAttackTime = Time.time;

            foreach (Transform opponent in opponents)
            {
                if (Vector3.Distance(transform.position, opponent.position) <= attackRadius)
                {
                    opponent.GetComponent<OpponentAI>().StartCoroutine(opponent.GetComponent<OpponentAI>().PlayHitDamageAnimation(attackDamages));
                }

            }
        }
    
        
            
        
        else
        {
            Debug.Log("Cannot perform attack yet. Cooldown time remaining: " +
                      (attackCooldown - (Time.time - lastAttackTime)));
        }
    }




    private void PerformDodge()
    {
        if (isDodging || Time.time < lastDodgeTime + dodgeCooldown) return;

        isDodging = true;
        lastDodgeTime = Time.time;

        // Play dodge animation
        animator.Play("DodgeFrontAnimation");

        // Calculate dodge movement
        Vector3 dodgeDirection = transform.forward * dodgeDistance;

        // Apply dodge movement
        StartCoroutine(DodgeCoroutine(dodgeDirection));
    }

    //void performDodgeFront()
    //{
    //    if (Input.GetKeydown(KeyCode.E))
    //    {
    //        animator.Play("DodgeFrontAnimation")
    //        Vector3 dodgeDirection = transform.forward * dodgeDistance;
    //        characterController.Move(dodgeDirection);
    //    }
    //}

    private IEnumerator DodgeCoroutine(Vector3 dodgeDirection)
    {
        float dodgeDuration = 0.5f; // Adjust this value to match your animation
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + dodgeDirection;

        while (elapsedTime < dodgeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / dodgeDuration;

            // Use smooth interpolation
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, t);
            characterController.Move(newPosition - transform.position);

            yield return null;
        }

        isDodging = false;
    }



    public IEnumerator PlayHitDamageAnimation(int takeDamage)
    {
        yield return new WaitForSeconds(0.5f);

        // Play hit sound using the AudioSource component
        if (hitSounds != null && hitSounds.Length > 0 && audioSource != null)
        {
            int randomIndex = Random.Range(0, hitSounds.Length);
            audioSource.PlayOneShot(hitSounds[randomIndex]);
            Debug.Log("Playing hit sound using AudioSource");
        }
        else
        {
            Debug.LogWarning("Missing audio source or hit sounds!");
        }

        currentHealth -= takeDamage;
        healthBar.SetHealth(currentHealth);
        if (currentHealth <= 0)
        {
            Die();
        }
        animator.Play("HitDamageAnimation");
    }

    void Die()
    {
        Debug.Log("player died");
    }



    private void OnDrawGizmosSelected()
    {
        // Visualize attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }


    public void Attack1spEffect()
    {
        attack1spEffect.Play();

    }
    public void Attack2spEffect()
    {
        attack2spEffect.Play();

    }
    public void Attack3spEffect()
    {
        attack3spEffect.Play();

    }
    public void Attack4spEffect()
    {
        attack4spEffect.Play();

    }

    // Animation event methods
    public void Attack1Effect() { Debug.Log("Attack1Effect triggered!"); }
    public void Attack2Effect() { Debug.Log("Attack2Effect triggered!"); }
    public void Attack3Effect() { Debug.Log("Attack3Effect triggered!"); }
    public void Attack4Effect() { Debug.Log("Attack4Effect triggered!"); }
}
