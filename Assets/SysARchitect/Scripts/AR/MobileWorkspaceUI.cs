using UnityEngine;
using UnityEngine.UI;

namespace SysARchitect.AR
{
    public class MobileWorkspaceUI : MonoBehaviour
    {
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private GameObject toolbox;
        [SerializeField] private Button toggleToolbox, reposition;
        [SerializeField] private ARWorkspace workspace;
        [SerializeField] private SysARchitect.Interaction.NodeSelectionController selection;
        [SerializeField] private SysARchitect.Interaction.InteractionManager interactions;
        private MobilePanelLayout layout;
        private Rect lastSafeArea;
        private void Start()
        {
            toggleToolbox.onClick.AddListener(() => toolbox.SetActive(!toolbox.activeSelf));
            reposition.onClick.AddListener(workspace.Reposition);
            layout = new MobilePanelLayout(safeArea, toolbox, toggleToolbox, reposition, selection, interactions, () => workspace.CanInteract);
        }
        private void LateUpdate() => layout?.Refresh();
        private void Update()
        {
            if (Screen.safeArea == lastSafeArea) return;
            lastSafeArea = Screen.safeArea;
            ApplySafeArea(safeArea, lastSafeArea, new Vector2(Screen.width, Screen.height));
        }
        public static void ApplySafeArea(RectTransform target, Rect pixels, Vector2 screenSize)
        {
            if (screenSize.x <= 0 || screenSize.y <= 0 || pixels.width <= 0 || pixels.height <= 0) return;
            target.anchorMin = new Vector2(Mathf.Clamp01(pixels.xMin / screenSize.x), Mathf.Clamp01(pixels.yMin / screenSize.y));
            target.anchorMax = new Vector2(Mathf.Clamp01(pixels.xMax / screenSize.x), Mathf.Clamp01(pixels.yMax / screenSize.y));
            target.offsetMin = target.offsetMax = Vector2.zero;
        }
    }
}
