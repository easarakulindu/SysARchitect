using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SysARchitect.Simulation;

namespace SysARchitect.UI
{
    public class SimulationUI : MonoBehaviour
    {
        [SerializeField] private SimulationController simulation;
        [SerializeField] private InspectorUI inspector;
        [SerializeField] private Button runButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button stepButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button buildButton;
        [SerializeField] private Button failButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Slider trafficSlider;
        [SerializeField] private TextMeshProUGUI trafficText;
        [SerializeField] private TextMeshProUGUI countersText;

        private void Start()
        {
            runButton.onClick.AddListener(simulation.Run);
            pauseButton.onClick.AddListener(simulation.Pause);
            stepButton.onClick.AddListener(simulation.StepOnce);
            resetButton.onClick.AddListener(simulation.ResetSimulation);
            buildButton.onClick.AddListener(simulation.Build);
            failButton.onClick.AddListener(() => simulation.FailNode(inspector.CurrentNode));
            restartButton.onClick.AddListener(() => simulation.RestartNode(inspector.CurrentNode));
            trafficSlider.minValue = 0;
            trafficSlider.maxValue = 500;
            trafficSlider.wholeNumbers = true;
            trafficSlider.SetValueWithoutNotify((float)simulation.Engine.RequestsPerSecond);
            trafficSlider.onValueChanged.AddListener(value => simulation.Engine.RequestsPerSecond = value);
        }

        private void Update()
        {
            var engine = simulation.Engine;
            trafficText.text = $"Incoming traffic: {engine.RequestsPerSecond:F0} requests/s";
            countersText.text = $"{(simulation.IsRunning ? "RUNNING" : "PAUSED")}  |  t={engine.Time:F2}s\n" +
                $"Sent {engine.Generated}    Success {engine.Succeeded}    Failed {engine.Failed}\n" +
                $"In flight {engine.InFlight}    Timeouts {engine.TimedOut}\n" +
                $"Mean successful latency {engine.AverageLatencyMs:F0} ms";
            runButton.interactable = !simulation.IsRunning;
            pauseButton.interactable = simulation.IsRunning;
            failButton.interactable = restartButton.interactable = inspector.CurrentNode != null;
        }
    }
}
