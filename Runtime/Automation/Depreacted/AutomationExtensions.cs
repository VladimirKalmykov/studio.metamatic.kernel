namespace Metamatic {
    // public static class AutomationExtensionMethods {
    //     public static AutomationRuntime RunAutomation<T>(this MonoBehaviour component, AutomationRunConfig runConfig, object props = null, CancellationToken cancellationToken = default) {
    //         AutomationBehaviour script = component.GetComponent<T>() as AutomationBehaviour;
    //         return script.Run(runConfig, props, cancellationToken);
    //     }

    //     /// <summary>
    //     /// Allows you to run automation with custom props derived from deaultProps by special props former func [UNSTABLE EXPERIMATAL API]
    //     /// </summary>
    //     /// <example>
    //     /// Run(AutomationRunConfig.Duration(4f), (props) => { props.color = Color.blue; return props; })
    //     /// </example>
    //     /// <param name="runConfig"></param>
    //     /// <param name="propsFormer"></param>
    //     /// <param name="cancellationToken"></param>
    //     /// <returns></returns>
    //     public static AutomationRuntime RunAutomation<T>(this MonoBehaviour component, AutomationRunConfig runConfig, Func<object, object> propsFormer, CancellationToken cancellationToken = default) {
    //         AutomationBehaviour script = component.GetComponent<T>() as AutomationBehaviour;
    //         return script.Run(runConfig, propsFormer, cancellationToken);
    //     }

    //     /// <summary>
    //     /// Overload of run to call it with no props
    //     /// </summary>
    //     /// <param name="runConfig"></param>
    //     /// <param name="cancellationToken"></param>
    //     /// <returns></returns>
    //     public static AutomationRuntime RunAutomation<T>(this MonoBehaviour component, AutomationRunConfig runConfig, CancellationToken cancellationToken = default) {
    //         AutomationBehaviour script = component.GetComponent<T>() as AutomationBehaviour;
    //         return script.Run(runConfig, null, cancellationToken);
    //     }
        
    //     /// <summary>
    //     /// Run zero-configured automation
    //     /// </summary>
    //     /// <param name="cancellationToken"></param>
    //     /// <returns></returns>
    //     public static AutomationRuntime RunAutomation<T>(this MonoBehaviour component, CancellationToken cancellationToken = default) {
    //         AutomationBehaviour script = component.GetComponent<T>() as AutomationBehaviour;
    //         AutomationRunConfig runConfig = new AutomationRunConfig {};
    //         return script.Run(runConfig, null, cancellationToken);
    //     }

    // }
}