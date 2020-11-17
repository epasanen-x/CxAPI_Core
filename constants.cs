using System;
using System.Collections.Generic;
using System.Text;

namespace CxAPI_Core
{
    static class CxConstant
    {
        public const string CxToken = "/cxrestapi/auth/identity/connect/token";
        public const string CxAllProjects = "/cxrestapi/projects";
        public const string CxProject = "/cxrestapi/projects/{0}";
        public const string CxScans = "/cxrestapi/sast/scans?scanStatus=Finished";
        public const string CxProjectScan = "/cxrestapi/sast/scans?projectId={0}&scanStatus=Finished";
        public const string CxLastProjectScan = "/cxrestapi/sast/scans?projectId={0}&scanStatus=Finished&last=1";
        public const string CxLastNProjectScan = "/cxrestapi/sast/scans?projectId={0}&scanStatus=Finished&last={1}";
        public const string CxLastScan = "/cxrestapi/sast/scans?scanStatus=Finished&last=1";
        public const string CxScanSettings = "/cxrestapi/sast/scanSettings/{0}";
        public const string CxTeams = "/cxrestapi/auth/teams";
        public const string CxReportRegister = "/cxrestapi/reports/sastScan";
        public const string CxReportFetch = "/cxrestapi/reports/sastScan/{0}";
        public const string CxReportStatus = "/cxrestapi/reports/sastScan/{0}/status";
        public const string CxPresets = "/cxrestapi/sast/presets";
        public const string CxScanStatistics = "/cxrestapi/sast/scans/{0}/resultsStatistics";
        public const string CxODATAScan = "/Cxwebinterface/odata/v1/Scans({0})";
    }
    static class _options
    {
        public static bool debug;
        public static int level;
        public static bool test;
        public static resultClass token;

    }

}