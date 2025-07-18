using System;
using System.Collections.Generic;

namespace Unity.PerformanceTesting.Data
{
    /// <summary>
    /// Represents a performance test result.
    /// </summary>
    [Serializable]
    public class PerformanceTestResult
    {
        /// <summary>
        /// Test name.
        /// </summary>
        [RequiredMember] public string Name;

        /// <summary>
        /// Test version.
        /// </summary>
        [RequiredMember] public string Version;

        /// <summary>
        /// Categories.
        /// </summary>
        [RequiredMember] public List<string> Categories = new List<string>();

        /// <summary>
        /// Groups of performance samples.
        /// </summary>
        [RequiredMember] public List<SampleGroup> SampleGroups = new List<SampleGroup>();
    }
}