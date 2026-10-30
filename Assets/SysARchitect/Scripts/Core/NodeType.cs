namespace SysARchitect.Core
{
    /// <summary>
    /// Types of infrastructure components that can be placed in the architecture.
    /// </summary>
    public enum NodeType
    {
        Frontend,
        Backend,
        Database,
        LoadBalancer,
        Cache,
        Container,
        KubernetesNode,
        KubernetesPod
    }
}
