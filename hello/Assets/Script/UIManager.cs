using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject pickupPopupUI;
    public TextMeshProUGUI popupText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton; 

    [Header("Player Target")]
    public InventoryComponent playerInventory;

    [Header("Player Status")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private PlayerSkillSystem playerSkillSystem;
    [SerializeField] private Slider playerHealthSlider;

    [Header("Skill Cooldown Slots")]
    [SerializeField] private SkillSlotUI attackSkillSlot;
    [SerializeField] private SkillSlotUI hornSkillSlot;
    [SerializeField] private SkillSlotUI flashSkillSlot;

    private Action onYesConfirmed;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }

        // YES 버튼 이벤트 바인딩
        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(() =>
        {
            onYesConfirmed?.Invoke();
            ClosePopup();
        });

        // NO 버튼 이벤트 바인딩
        if (noButton != null)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(ClosePopup);
        }
    }

    private void Start()
    {
        if (playerInventory == null) { playerInventory = FindAnyObjectByType<InventoryComponent>(); }

        FindAndBindPlayerStatus();
        PrepareStatusUI();
        SubscribeStatusEvents();
        RefreshInitialStatus();
    }

    private void OnDestroy()
    {
        UnsubscribeStatusEvents();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowPickupPopup(FieldItem item)
    {
        popupText.text = $"{item.myItem.itemName}를 획득하시겠습니까?";

        // Yes 버튼에 이벤트 송신
        onYesConfirmed = () =>
        {
            if (playerInventory != null && playerInventory.AddItem(item.myItem))
            {
                Destroy(item.gameObject);
            }
        };

        pickupPopupUI.SetActive(true);
    }

    public void ClosePopup()
    {
        pickupPopupUI.SetActive(false);
        onYesConfirmed = null;
    }

    private void FindAndBindPlayerStatus()
    {
        GameObject player = GameObject.FindWithTag("Player");

        if (player == null)
        {
            return;
        }

        if (playerHealth == null)
        {
            playerHealth = player.GetComponentInParent<Health>();

            if (playerHealth == null)
            {
                playerHealth = player.GetComponentInChildren<Health>();
            }
        }

        if (playerSkillSystem == null)
        {
            playerSkillSystem = player.GetComponentInParent<PlayerSkillSystem>();

            if (playerSkillSystem == null)
            {
                playerSkillSystem =
                    player.GetComponentInChildren<PlayerSkillSystem>();
            }
        }
    }

    private void PrepareStatusUI()
    {
        if (playerHealthSlider == null)
        {
            Slider[] sliders = FindObjectsByType<Slider>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Slider slider in sliders)
            {
                if (slider.gameObject.name == "Hp Bar")
                {
                    playerHealthSlider = slider;
                    break;
                }
            }
        }

        if (attackSkillSlot == null)
        {
            attackSkillSlot = FindSkillSlot("Skill 1 Attack");
            hornSkillSlot = FindSkillSlot("Skill 2 Horn");
            flashSkillSlot = FindSkillSlot("Skill 3 Flash");
        }

    }

    private SkillSlotUI FindSkillSlot(string objectName)
    {
        GameObject slotObject = GameObject.Find(objectName);
        return slotObject != null
            ? slotObject.GetComponent<SkillSlotUI>()
            : null;
    }

    private void SubscribeStatusEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged += UpdateHealthUI;
        }

        if (playerSkillSystem != null)
        {
            playerSkillSystem.CooldownChanged += UpdateCooldownUI;
        }
    }

    private void UnsubscribeStatusEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= UpdateHealthUI;
        }

        if (playerSkillSystem != null)
        {
            playerSkillSystem.CooldownChanged -= UpdateCooldownUI;
        }
    }

    private void RefreshInitialStatus()
    {
        if (playerHealth != null)
        {
            UpdateHealthUI(
                playerHealth.CurrentHealth,
                playerHealth.MaxHealth);
        }

        attackSkillSlot?.SetCooldown(1f, 0f);
        hornSkillSlot?.SetCooldown(1f, 0f);
        flashSkillSlot?.SetCooldown(1f, 0f);
    }

    private void UpdateHealthUI(float current, float maximum)
    {
        if (playerHealthSlider != null)
        {
            playerHealthSlider.value =
                maximum <= 0f ? 0f : current / maximum;
        }
    }

    private void UpdateCooldownUI(
        PlayerSkillSystem.SkillId skill,
        float normalizedReady,
        float remaining)
    {
        SkillSlotUI targetSlot = skill switch
        {
            PlayerSkillSystem.SkillId.Attack => attackSkillSlot,
            PlayerSkillSystem.SkillId.Horn => hornSkillSlot,
            PlayerSkillSystem.SkillId.Flash => flashSkillSlot,
            _ => null
        };

        targetSlot?.SetCooldown(normalizedReady, remaining);
    }
}
