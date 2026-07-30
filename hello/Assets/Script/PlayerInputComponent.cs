using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerSkillSystem))]
public class PlayerInputComponent : MonoBehaviour
{
    [SerializeField] private PlayerSkillSystem skillSystem;
    [SerializeField] private GameObject inventoryUI;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }

    private void Awake()
    {
        if (skillSystem == null)
        {
            skillSystem = GetComponent<PlayerSkillSystem>();
        }

        if (inventoryUI == null)
        {
            inventoryUI = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include).gameObject;
        }
    }

    public Vector2 LookAt()
    {
        Vector2 value = LookInput;
        LookInput = Vector2.zero;
        return value;
    }

    private void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }

    private void OnLook(InputValue value)
    {
        LookInput = value.Get<Vector2>();
    }

    private void OnInventoryToggle()
    {
        if (inventoryUI != null)
        {
            inventoryUI.SetActive(!inventoryUI.activeSelf);
        }
    }

    private void OnAttack()
    {
        skillSystem.TryUseSkill(PlayerSkillSystem.SkillId.Attack);
    }

    private void OnHorn()
    {
        skillSystem.TryUseSkill(PlayerSkillSystem.SkillId.Horn);
    }

    private void OnFlash()
    {
        skillSystem.TryUseSkill(PlayerSkillSystem.SkillId.Flash);
    }
}
