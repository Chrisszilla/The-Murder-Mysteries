using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerController : MonoBehaviour {

    public float sensitivity = 150f;
    public float clampAngle = 85f;
    public int tapes;
    public LayerMask targets;
    public Text tapeText;
    public Text timerText;
    public Text endingText;
    public Text logText;
    public Transform hand;
    public CanvasGroup fader;

    public bool isHoldingObject { get; set; }
    public bool countingTimer { get; set; }

    private float timer = 180;
    private Animator animator;
    private float xRot;
    private GameObject referenceObject;
    private Transform heldObject;
    private Vector3 heldObjectScale;

    private void Start()
    {
        timer = 180;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        animator = GetComponentInChildren<Animator>();
        StartCoroutine(Fade(0));
    }

    private void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(x, 0, z);

        transform.Translate(move.normalized * Time.deltaTime * (sensitivity / 40), Space.Self);

        VerticalLook();
        transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime, 0);

        if (countingTimer)
        {
            timer -= Time.deltaTime;
            string secs = "";
            float adjustableTimer = timer;
            int minutes = 0;
            while (adjustableTimer - 60 > 0)
            {
                adjustableTimer -= 60;
                minutes++;
            }
            secs = Mathf.Round(adjustableTimer).ToString();
            if (adjustableTimer <= 9.4999f)
            {
                secs = secs.Insert(0, "0");
            }
            timerText.text = "0" + minutes.ToString() + ":" + secs;
            if (timer <= 0)
            {
                endingText.text = "You couldn't find all the evidence...";
                StartCoroutine(Fade(1));
            }
        }

        RaycastHit hit;

        if (isHoldingObject)
        {
            if (Input.GetMouseButtonDown(0))
            {
                DropObject();
            }
            return;
        }

        Vector3 screenCenter = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(screenCenter, Camera.main.transform.forward, out hit, 2f, targets, QueryTriggerInteraction.Ignore))
        {
            if (Input.GetMouseButtonDown(0))
            {
                OnClick(hit);
            }
            else
            {
                if (referenceObject != hit.collider.gameObject)
                {
                    referenceObject = hit.collider.gameObject;
                    MeshRenderer r = referenceObject.GetComponentInParent<MeshRenderer>();
                    if (r)
                    {
                        if (r.materials.Length > 1)
                        {
                            for (int i = 0; i < r.materials.Length; i++)
                            {
                                r.materials[i].SetColor("_EmissionColor", Color.white * 0.25f);
                            }
                        }
                        else
                            r.material.SetColor("_EmissionColor", Color.white * 0.25f);
                    }
                }
            }
        }
        else
        {
            if (referenceObject)
            {
                MeshRenderer r = referenceObject.GetComponentInParent<MeshRenderer>();
                if (r)
                {
                    if (r.materials.Length > 1)
                    {
                        for (int i = 0; i < r.materials.Length; i++)
                        {
                            r.materials[i].SetColor("_EmissionColor", Color.clear);
                        }
                    }
                    else
                        r.material.SetColor("_EmissionColor", Color.clear);
                }
                referenceObject = null;
            }
            animator.ResetTrigger("Grab");
            if (Input.GetMouseButtonDown(0))
            {
                animator.SetTrigger("Grab");
            }
        }
    }

    private void OnClick(RaycastHit hit)
    {
        animator.SetTrigger("Grab");

        if (hit.collider.CompareTag("Door"))
        {
            if (!countingTimer)
            {
                StartCoroutine(LogText("Find all the evidence.", 4, 1));
                countingTimer = true;
            }
            Animator refAnim = hit.collider.GetComponent<ComponentReference>().reference;
            if (!refAnim.enabled)
                refAnim.enabled = true;
            else
            {
                refAnim.SetBool("Open", !refAnim.GetBool("Open"));
            }
        }
        if (hit.collider.CompareTag("Switch"))
        {
            if (hit.collider.GetComponent<LightSwitch>())
            {
                hit.collider.GetComponent<LightSwitch>().SwitchLights();
            }
        }
        if (hit.collider.CompareTag("Collectable"))
        {
            tapes++;
            tapeText.text = "Tapes: " + tapes + "/5";
            PickupObject(hit);
            Destroy(hit.collider.gameObject, 1f);
            if (tapes >= 5)
            {
                endingText.text = "You found all the evidence!";
                StartCoroutine(Fade(1));
            }
        }
        if (hit.collider.CompareTag("Untagged"))
        {
            if (hit.collider.attachedRigidbody)
            {
                heldObjectScale = hit.transform.localScale;
                heldObject = hit.transform;
                isHoldingObject = true;
                animator.SetBool("Holding", true);
                PickupObject(hit);
                return;
            }
        }
    }

    private void PickupObject(RaycastHit hit)
    {
        hit.collider.isTrigger = true;
        hit.collider.attachedRigidbody.isKinematic = true;
        hit.transform.SetParent(hand, false);
        hit.transform.localPosition = Vector3.zero;
    }

    private void DropObject()
    {
        animator.SetBool("Holding", false);
        heldObject.GetComponent<Collider>().isTrigger = false;
        heldObject.GetComponent<Rigidbody>().isKinematic = false;
        heldObject.SetParent(null, true);
        heldObject.localScale = heldObjectScale;
        isHoldingObject = false;
    }

    private void VerticalLook()
    {
        var mouseX = (-Input.GetAxis("Mouse Y")) * sensitivity;
        xRot += mouseX * Time.deltaTime;

        Quaternion localRotation = Quaternion.Euler(xRot, 0, 0);
        Camera.main.transform.localRotation = localRotation;

        xRot = Mathf.Clamp(xRot, -clampAngle, clampAngle);
    }

    private IEnumerator Fade(float alpha)
    {
        if (tapes >= 5)
        {
            yield return new WaitForSeconds(2);
        }
        while (!Mathf.Approximately(fader.alpha, alpha))
        {
            if (tapes >= 5)
            {
                sensitivity = Mathf.Lerp(sensitivity, 0, Time.deltaTime);
            }
            fader.alpha = Mathf.MoveTowards(fader.alpha, alpha, Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator LogText(string text, float length, float delay)
    {
        yield return new WaitForSeconds(delay);
        logText.text = text;
        yield return new WaitForSeconds(length);
        logText.text = "";
    }
}
