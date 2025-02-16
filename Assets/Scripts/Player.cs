using UnityEngine;
using DG.Tweening;
using Cinemachine;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float preSquashAmount = 0.5f;
    [SerializeField] private float preStretchAmount = 1.5f;
    [SerializeField] private float preAnimationDuration = 1f;
    [SerializeField] private float squashAmount = 0.7f;
    [SerializeField] private float stretchAmount = 1.3f;
    [SerializeField] private float animationDuration = 0.2f;
    private Rigidbody rb;
    private bool isGrounded;
    private Vector3 originalScale;

    [Header("Camera Settings")]
    [SerializeField] private CinemachineVirtualCamera cam;
    [SerializeField] private float shakeDuration = 0.4f;
    [SerializeField] private float dropShakeIntensity = 2f;
    [SerializeField] private float chargeShakeIntensity = 3f;
    private CinemachineBasicMultiChannelPerlin camNoise;

    [Header("Particle Reference")]
    [SerializeField] private ParticleSystem landingParticleSystem;

    [Header("Audio References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip chargeClip;
    [SerializeField] private AudioClip dropClip;

    [Header("Secondary Objects")]
    [SerializeField] private List<Rigidbody> rigidbodyList;

    [Header("Flicker Settings")]
    [SerializeField] private Color flickerColor = Color.white;
    [SerializeField] private float flickerInterval = 0.1f;
    [SerializeField] private int flickerCount = 6;
    private Color originalColor;
    private Material material;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale; 
        isGrounded = true;

        material = GetComponent<MeshRenderer>().material;
        originalColor = material.color;

        if (cam != null)
        {
            camNoise = cam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    void Jump()
    {
        ScreenShake(dropShakeIntensity);
        StartCoroutine(FlickerEffect());
        PlayAudio(chargeClip, 0.2f);

        transform.DOScale(new Vector3(originalScale.x * preStretchAmount, originalScale.y * preSquashAmount, originalScale.z * preStretchAmount), preAnimationDuration * 0.5f)
            .OnComplete(() =>
            {
                transform.DOScale(originalScale, animationDuration * 0.5f).OnComplete(() =>
                {
                    rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
                    isGrounded = false;

                    DOVirtual.DelayedCall(0.1f, () =>
                    {
                        if (!isGrounded) 
                        {
                            StartCoroutine(RotateAndStretchInAir());
                        }
                    });
                });
            });
    }

    private IEnumerator FlickerEffect()
    {
        for (int i = 0; i < flickerCount; i++)
        {
            material.color = flickerColor; 
            yield return new WaitForSeconds(flickerInterval);
            material.color = originalColor; 
            yield return new WaitForSeconds(flickerInterval);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") && !isGrounded)
        {
            isGrounded = true;
            ScreenShake(dropShakeIntensity);
            landingParticleSystem.Play();
            PlayAudio(dropClip, 1f);
            ApplyShockwave();
        }
    }

    private void PlayAudio(AudioClip clip, float volume)
    {
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
    }

    private void ApplyShockwave()
    {
        float minShockForce = 2f; 
        float maxShockForce = 4f;      

        foreach (Rigidbody rb in rigidbodyList)
        {
            if (rb != null)
            {
                float shockForce = Random.Range(minShockForce, maxShockForce);

                Vector3 randomForce = new Vector3(0, shockForce, 0);
                rb.AddForce(randomForce, ForceMode.Impulse);
            }
        }
    }

    private void ScreenShake(float shakeIntensity)
    {
        if (camNoise != null)
        {
            camNoise.m_AmplitudeGain = shakeIntensity;
            DOVirtual.DelayedCall(shakeDuration, () => camNoise.m_AmplitudeGain = 0);
        }
    }

    private IEnumerator RotateAndStretchInAir()
    {
        float rotationSpeed = 360f / 0.2f; 
        float currentRotation = 0f;

        while (currentRotation < 360f)
        {
            float rotationThisFrame = rotationSpeed * Time.deltaTime;
            currentRotation += rotationThisFrame;
            transform.DOScale(new Vector3(originalScale.x * squashAmount, originalScale.y * stretchAmount, originalScale.z * squashAmount), animationDuration * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, 0, currentRotation);
            yield return null;
        }

        transform.rotation = Quaternion.Euler(0, 0, 0);

        transform.DOScale(originalScale, animationDuration);

    }
}
