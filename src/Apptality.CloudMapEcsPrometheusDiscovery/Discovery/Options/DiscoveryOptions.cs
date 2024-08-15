using System.ComponentModel.DataAnnotations;

namespace Apptality.CloudMapEcsPrometheusDiscovery.Discovery.Options;

/// <summary>
/// Class for configuring service discovery options
/// </summary>
public class DiscoveryOptions : IValidatableObject
{
    /// <summary>
    /// Semicolon separated string containing ECS clusters names to query for services.
    /// </summary>
    /// <remarks>
    /// At least one of "EcsClusters" or "CloudMapNamespaces" must be provided.
    /// </remarks>
    public string EcsClusters { get; init; } = string.Empty;

    /// <summary>
    /// Semicolon separated string of tag key-value pairs to select ECS services.
    /// If selector is not provided, all services in the cluster are included.
    /// Example: "service_discovery=true;component=app"
    /// </summary>
    public string EcsServiceSelectorTags { get; init; } = string.Empty;

    /// <summary>
    /// Semicolon separated string containing Cloud Map namespaces names to query for services.
    /// All ECS services that are using Service Connect are always included in lookup.
    /// This parameter allows you to include additional namespaces to discover
    /// services using AWS Service Discovery (legacy).
    /// </summary>
    /// <remarks>
    /// At least one of "EcsClusters" or "CloudMapNamespaces" must be provided.
    /// </remarks>
    public string CloudMapNamespaces { get; init; } = string.Empty;

    /// <summary>
    /// Semicolon separated string of tag key-value pairs to select CloudMap services.
    /// If selector is not provided, all services in the CloudMap namespace are included.
    /// Example: "service_discovery=true;component=app"
    /// Default is empty string (match all services).
    /// </summary>
    /// <remarks>
    /// Read more at <a href="https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-connect.html">service-connect</a> docs.
    /// Read more at <a href="https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-discovery.html">service-discovery</a> docs.
    /// </remarks>
    public string CloudMapServiceSelectorTags { get; init; } = string.Empty;

    // Tags are resolved with the following priority:
    // 1) ECS Task
    // 2) ECS Service
    // 3) Cloud Map Service
    // 4) Cloud Map Namespace
    // For example, if both Cloud Map Namespace and ECS Task have a tag
    // with the same key, the value from the ECS Task will be used.

    /// <summary>
    /// When non-empty, tags that match from ECS tasks
    /// are included in the service discovery results as labels.
    /// </summary>
    /// <remarks>
    /// Note: tags are resolved from running tasks, not tasks definitions.
    /// If you would like to include tags from task definitions,
    /// consider modifying ECS service to propagate tags from task definitions to running tasks.
    /// You can read more <a href="https://docs.aws.amazon.com/AmazonECS/latest/developerguide/ecs-using-tags.html#tag-resources">here</a>
    /// </remarks>
    public string EcsTaskTags { get; set; } = string.Empty;

    /// <summary>
    /// When non-empty, tags that match from ECS services
    /// are included in the service discovery results as labels.
    /// </summary>
    public string EcsServiceTags { get; set; } = string.Empty;

    /// <summary>
    /// When non-empty, tags that match from Cloud Map services
    /// are included in the service discovery results as labels.
    /// </summary>
    public string CloudMapServiceTags { get; set; } = string.Empty;

    /// <summary>
    /// When non-empty, tags that match from Cloud Map namespaces
    /// are included in the service discovery results as labels.
    /// </summary>
    public string CloudMapNamespaceTags { get; set; } = string.Empty;

    /// <summary>
    /// Semicolon separated string of static labels to include in the
    /// service discovery response as metadata.
    /// </summary>
    /// <remarks>
    /// Once parsed, provided labels and values will be added to all targets metadata
    /// </remarks>
    public string ExtraPrometheusLabels { get; set; } = string.Empty;

    /// <summary>
    /// Specifies how long to cache the service discovery results in seconds. Default is five seconds.
    /// </summary>
    /// <remarks>
    /// When caching is disabled, the service discovery results are queried every time a service response is requested.
    /// Min value - 0 seconds (no caching). Max value - 65535 seconds.
    /// <para>
    /// <b>Important:</b> It is strongly recommended to keep caching at the very least for 5 seconds to avoid rate limiting.
    /// </para>
    /// </remarks>
    [Range(0, 65535)]
    public ushort CacheTtlSeconds { get; set; } = 60;

    /// <summary>
    /// Tag prefix to identify a metrics path, port, and name.
    /// Must be at least three characters long and end with an underscore.
    /// </summary>
    /// <remarks>
    /// This prefix has a special meaning and is used to identify the metrics path and port
    /// for the service. PATH, PORT, NAME will be appended to the prefix to identify
    /// metrics path, port, and container names respectively.
    /// <para>
    /// If your service contains multiple metrics paths
    /// or ports, you can tag your resources using the following format:
    /// <ul>
    /// <li>METRICS_PATH_1 = /metrics</li>
    /// <li>METRICS_PORT_1 = 9001</li>
    /// <li>METRICS_NAME_1 = application</li>
    /// <li>METRICS_PATH_2 = /metrics2</li>
    /// <li>METRICS_PORT_2 = 9002</li>
    /// <li>METRICS_NAME_2 = sidecar</li>
    /// </ul>
    /// ..etc.
    /// </para>
    /// <para>
    /// System will not care of anything after _PATH, _PORT, or _NAME,
    /// so as long as these postfixes are identical for triplets,
    /// you can use any postfix (including empty) you prefer,
    /// which makes the following combination valid:
    /// <ul>
    /// <li>METRICS_PATH = /metrics</li>
    /// <li>METRICS_PORT = 9001</li>
    /// </ul>
    /// <ul>
    /// <li>METRICS_PATH_SVC-ONE = /metrics</li>
    /// <li>METRICS_PORT_SVC-ONE = 9001</li>
    /// <li>METRICS_NAME_SVC-ONE = svc-one</li>
    /// </ul>
    /// </para>
    /// If a path is not provided, default path "/metrics" will be used.
    /// <br />
    /// If a name is not provided, the service name will be omitted from labels.
    /// <br />
    /// Here is an example of regex to match the tags: ^METRICS_(PATH|PORT|NAME)\w+$
    /// </remarks>
    [MinLength(3)]
    public string MetricsPathPortTagPrefix { get; set; } = "METRICS_";

    /// <summary>
    /// Validates the discovery options
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Keep track of validation results
        var validationResults = new List<ValidationResult>();

        // Ensure at least one Cloud Map namespace or ECS cluster is provided
        var ecsClusters = this.GetEcsClustersNames();
        var cloudMapNamespaces = this.GetCloudMapNamespaceNames();

        // If both are empty, add a validation error
        if (ecsClusters.Length == 0 && cloudMapNamespaces.Length == 0)
        {
            validationResults.Add(new ValidationResult(
                "At least one of 'EcsClusters' or 'CloudMapNamespaces' name must be specified.",
                new[] {nameof(EcsClusters), nameof(CloudMapNamespaces)}));
        }

        // Ensure that MetricsPathPortTagPrefix ends with an underscore
        if (!MetricsPathPortTagPrefix.EndsWith("_"))
        {
            validationResults.Add(new ValidationResult(
                "MetricsPathPortTagPrefix must end with an underscore.",
                new[] {nameof(MetricsPathPortTagPrefix)}));
        }

        return validationResults;
    }
}