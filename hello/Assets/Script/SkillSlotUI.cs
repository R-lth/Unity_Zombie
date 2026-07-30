using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
    [SerializeField] private Image cooldownImage;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private string skillLabel;

    public void SetCooldown(float normalizedReady, float remaining)
    {
        if (cooldownImage != null)
        {
            cooldownImage.fillAmount = Mathf.Clamp01(normalizedReady);
        }

        if (cooldownText != null)
        {
            cooldownText.text =
                remaining > 0f
                    $"{skillLabel}\n{Mathf.CeilToInt(remaining)}"
                    : skillLabel;
        }
    }
}
