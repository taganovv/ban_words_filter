using System;

namespace BanWordsFilter.Models;

public enum UpdateRequirement
{
    None,
    Optional,
    Mandatory
}

public sealed class UpdateCheckResult
{
    public UpdateRequirement Requirement { get; init; }
    public Version? CurrentVersion { get; init; }
    public Version? LatestVersion { get; init; }
    public string? InstallerDownloadUrl { get; init; }
}
