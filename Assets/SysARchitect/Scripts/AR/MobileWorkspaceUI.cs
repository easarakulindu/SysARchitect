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
        private Rect lastSafeArea;
        private void Start()
        {
            toggleToolbox.onClick.AddListener(() => toolbox.SetActive(!toolbox.activeSelf));
            reposition.onClick.AddListener(workspace.Reposition);
        }
        private void Update()
        {
            if (Screen.safeArea == lastSafeArea) return;
            lastSafeArea = Screen.safeArea;
            safeArea.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
        }
    }
}
