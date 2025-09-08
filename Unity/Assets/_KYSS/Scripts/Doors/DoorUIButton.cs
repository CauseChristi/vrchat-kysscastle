using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class DoorUIButton : UdonSharpBehaviour
{
    [Header("Target")]
    [Tooltip("The DoorSlide this button controls.")]
    public DoorSlide linkedDoor;

    [Header("UI")]
    [Tooltip("Assign the Button component (or leave empty to auto-find on Start).")]
    public Button uiButton;

    [Tooltip("TMP label for the button.")]
    public TextMeshProUGUI tmpLabel;

    [Header("Labels (override here)")]
    [Tooltip("Shown when the door is CLOSED (pressing will OPEN it).")]
    public string openLabel = "Open";
    [Tooltip("Shown when the door is OPEN (pressing will CLOSE it).")]
    public string closeLabel = "Close";

    [Header("Behavior")]
    [Tooltip("If true, disables the button while the door is moving.")]
    public bool disableWhileMoving = true;
    
    [Header("Whitelist Settings")]
    public GeneralWhitelist generalWhitelist;
    public bool whitelistOnly = false;

    [Tooltip("Print debug logs.")]
    public bool debugLogs = false;

    private bool _hadInit;
    private bool _lastIsOpen;
    private bool _lastIsMoving;

    void Start()
    {
        if (uiButton == null)
        {
            uiButton = GetComponent<Button>();
        }

        _hadInit = true;
        RefreshLabelAndInteractable();
    }

    void Update()
    {
        if (linkedDoor == null) return;

        bool isOpen   = linkedDoor.IsOpen;
        bool isMoving = linkedDoor.IsMoving;

        if (!_hadInit || isOpen != _lastIsOpen || isMoving != _lastIsMoving)
        {
            RefreshLabelAndInteractable();
            _lastIsOpen = isOpen;
            _lastIsMoving = isMoving;
            _hadInit = true;
        }
    }

    // Hook this from the Button's OnClick (Inspector).
    public void OnUIButtonClick()
    {
        if (linkedDoor == null) return;

        if (whitelistOnly && !generalWhitelist.IsPlayerWhitelisted(Networking.LocalPlayer)) return;

        if (linkedDoor.IsMoving)
        {
            if (debugLogs) Debug.Log("[DoorUIButton] Door is moving; click ignored.");
            return;
        }

        //linkedDoor.Toggle();
        linkedDoor.SendCustomEvent("Toggle");
        if (debugLogs) Debug.Log("[DoorUIButton] Toggled door: " + linkedDoor.name);
    }

    private void RefreshLabelAndInteractable()
    {
        if (linkedDoor == null) return;

        // Update label
        if (tmpLabel != null)
        {
            tmpLabel.text = linkedDoor.IsOpen ? closeLabel : openLabel;
        }

        // Update interactable
        if (disableWhileMoving && uiButton != null)
        {
            uiButton.interactable = !linkedDoor.IsMoving;
        }

        if (debugLogs)
        {
            Debug.Log("[DoorUIButton] Label='" + (tmpLabel != null ? tmpLabel.text : "(no label)") +
                      "', interactable=" + (uiButton != null ? uiButton.interactable.ToString() : "n/a"));
        }
    }
}
