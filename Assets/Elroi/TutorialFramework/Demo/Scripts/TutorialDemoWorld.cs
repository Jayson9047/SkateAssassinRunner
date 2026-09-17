using UnityEngine;

namespace Elroi.Tutorials.Demo
{
    public sealed class TutorialDemoWorld : MonoBehaviour
    {
        [SerializeField] private TutorialManager manager;
        [SerializeField] private TutorialVariableStore variables;
        [SerializeField] private Transform runner;
        [SerializeField] private float speed = 1.5f;
        [SerializeField] private float variableThresholdX = -5f;
        [SerializeField] private float eventThresholdX = -2.5f;
        private bool variableRaised;
        private bool eventRaised;

        private void Update()
        {
            if (runner == null) return;
            runner.position += Vector3.right * (speed * Time.deltaTime);
            if (!variableRaised && runner.position.x >= variableThresholdX)
            {
                variableRaised = true;
                variables?.SetInteger("Coins", 1);
            }
            if (!eventRaised && runner.position.x >= eventThresholdX)
            {
                eventRaised = true;
                manager?.RaiseEvent("DemoWorldReachedGate");
            }
            if (runner.position.x > 7f) runner.position = new Vector3(7f, runner.position.y, runner.position.z);
        }

        public void ConfigureForAuthoring(TutorialManager tutorialManager, TutorialVariableStore store, Transform runnerTransform)
        {
            manager = tutorialManager;
            variables = store;
            runner = runnerTransform;
        }
    }
}
