using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using CxAPI_Core.dto;
using System.Threading;

namespace CxAPI_Core
{
    class restReport_2 : IDisposable
    {
        public resultClass token;

        public restReport_2(resultClass token)
        {
            this.token = token;
        }

        public bool fetchReportsbyDate()
        {
            if (token.debug && token.verbosity > 1) { Console.WriteLine("Running: {0}", token.report_name); }
            List<ReportTrace> trace = new List<ReportTrace>();
            List<ReportResultAll> resultNew = new List<ReportResultAll>();
            Dictionary<long, ReportStaging> start = new Dictionary<long, ReportStaging>();
            Dictionary<long, ReportStaging> end = new Dictionary<long, ReportStaging>();
            Dictionary<long, List<ReportResultAll>> first = new Dictionary<long, List<ReportResultAll>>();
            Dictionary<long, List<ReportResultAll>> last = new Dictionary<long, List<ReportResultAll>>();
            Dictionary<long, ScanCount> scanCount = new Dictionary<long, ScanCount>();
            getScans scans = new getScans();
            getProjects projects = new getProjects(token);

            //List<ScanObject> scan = scans.getScan(token);
            Dictionary<Guid, Teams> teams = projects.CxTeams;
            List<ScanObject> scan = projects.filter_by_projects(token);
            Dictionary<long, ScanStatistics> resultStatistics = projects.CxResultStatistics;
            getScanResults scanResults = new getScanResults();

            if (scan.Count == 0)
            {
                Console.Error.WriteLine("No scans were found, pleas check argumants and retry.");
                return false;
            }

            foreach (ScanObject s in scan)
            {
                setCount(s.Project.Id, scanCount);
                findFirstandLastScan(s.Project.Id, s, resultStatistics[s.Id], start, end);

                ReportResult result = scanResults.SetResultRequest(s.Id, "XML", token);
                //                        ReportResult result = scanResults.SetResultRequest(s.Id, "XML", token);
                //                        if (result != null)
                //                        {
                //                            trace.Add(new ReportTrace(s.Project.Id, s.Project.Name, scans.getFullName(teams, s.OwningTeamId), s.DateAndTime.StartedOn, s.Id, result.ReportId, "XML"));
                //                        }
                if (trace.Count % token.max_threads == 0)
                {
                    waitForResult(trace, scanResults, resultNew, start, end, first, last);
                    trace.Clear();
                }
                if (result != null)
                {
                    trace.Add(new ReportTrace(s.Project.Id, s.Project.Name, teams[s.OwningTeamId].fullName, s.DateAndTime.StartedOn, s.Id, result.ReportId, "XML"));
                }

            }

            waitForResult(trace, scanResults, resultNew, start, end, first, last);
            trace.Clear();

            List<ReportOutputExtended> reportOutputs = totalScansandReports(start, end, resultNew, first, last, scanCount);
            if (token.debug) { Console.WriteLine("Processing data, number of rows: {0}", reportOutputs.Count); }
            if (token.pipe)
            {
                foreach (ReportOutputExtended csv in reportOutputs)
                {
                    Console.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21}", csv.ProjectName, csv.Team, csv.LastHigh, csv.LastMedium, csv.LastLow, csv.StartNotExploitable, csv.StartConfirmed, csv.StartToVerify, csv.LastOthers, csv.NewHigh, csv.NewMedium, csv.NewLow, csv.DiffHigh, csv.DiffMedium, csv.DiffLow, csv.LastNotExploitable, csv.LastConfirmed, csv.LastToVerify, csv.LastOthers, csv.firstScan, csv.lastScan, csv.ScanCount);
                }
            }
            else
            {
                csvHelper csvHelper = new csvHelper();
                csvHelper.writeCVSFile(reportOutputs, token);
            }
            return true;
        }

        private bool getFirstandLastReport(XElement result, Dictionary<long, ReportStaging> start, Dictionary<long, ReportStaging> end, Dictionary<long, List<ReportResultAll>> first, Dictionary<long, List<ReportResultAll>> last)
        {
            foreach (long key in end.Keys)
            {
                ReportStaging staging = end[key];
                if (result.Attribute("ScanId").Value == staging.ScanId.ToString())
                {
                    last.Add(staging.ProjectId, process_ScanResult(result, staging.ScanId));
                }
            }
            foreach (long key in start.Keys)
            {
                ReportStaging staging = start[key];
                if (result.Attribute("ScanId").Value == staging.ScanId.ToString())
                {
                    first.Add(staging.ProjectId, process_ScanResult(result, staging.ScanId));
                }
            }
            return true;
        }

        private void setCount(long id, Dictionary<long, ScanCount> scanCount)
        {
            if (scanCount.ContainsKey(id))
            {
                ScanCount sc = scanCount[id];
                sc.count++;
                scanCount[id] = sc;
            }
            else
            {
                ScanCount sc = new ScanCount();
                sc.count = 1;
                scanCount.Add(id, sc);
            }
        }

        private List<ReportOutputExtended> totalScansandReports(Dictionary<long, ReportStaging> start, Dictionary<long, ReportStaging> end, List<ReportResultAll> resultNew, Dictionary<long, List<ReportResultAll>> firstScan, Dictionary<long, List<ReportResultAll>> lastScan, Dictionary<long, ScanCount> scanCount)
        {
            List<ReportOutputExtended> reports = new List<ReportOutputExtended>();
            foreach (long key in start.Keys)
            {
                ReportOutputExtended report = new ReportOutputExtended();

                ReportStaging first = start[key];
                ReportStaging last = end[key];
                List<ReportResultAll> lastScanResults = lastScan[key];
                List<ReportResultAll> firstScanResults = firstScan[key];
                foreach (ReportResultAll result in resultNew)
                {
                    if (result.projectId == first.ProjectId)
                    {
                        if (result.status == "New")
                        {
                            if (result.Severity == "High") { report.NewHigh++; }
                            else if (result.Severity == "Medium") { report.NewMedium++; }
                            else if (result.Severity == "Low") { report.NewLow++; }
                        }
                    }
                }
                foreach (ReportResultAll result in lastScanResults)
                {
                    if (result.state == 0) { report.LastToVerify++; }
                    else if (result.state == 1) { report.LastNotExploitable++; }
                    else if (result.state == 2) { report.LastConfirmed++; }
                    else { report.LastOthers++; }
                    if (result.status == "Fixed")
                    {
                        report.LastFixed++;
                    }
                }

                foreach (ReportResultAll result in firstScanResults)
                {
                    if (result.state == 0) { report.StartToVerify++; }
                    else if (result.state == 1) { report.StartNotExploitable++; }
                    else if (result.state == 2) { report.StartConfirmed++; }
                    else { report.StartOthers++; }
                    if (result.status == "Fixed")
                    {
                        report.StartFixed++;
                    }
                    report.Team = result.teamName;
                }
                report.ProjectName = first.ProjectName;
                report.StartHigh = first.High;
                report.StartMedium = first.Medium;
                report.StartLow = first.Low;
                report.firstScan = first.dateTime;

                report.LastHigh = last.High;
                report.LastMedium = last.Medium;
                report.LastLow = last.Low;
                report.lastScan = last.dateTime;

                report.DiffHigh = first.High - last.High;
                report.DiffMedium = first.Medium - last.Medium;
                report.DiffLow = first.Low - last.Low;
                report.ScanCount = scanCount[key].count;
                reports.Add(report);

            }
            return reports;
        }

        private bool process_CxResponse(XElement result, List<ReportResultAll> response)
        {
            try
            {
                IEnumerable<XElement> newVulerability = from el in result.Descendants("Query").Descendants("Result")
                                                        where (string)el.Attribute("Status").Value == "New"
                                                        select el;

                foreach (XElement el in newVulerability)
                {
                    XElement query = el.Parent;
                    XElement root = query.Parent;
                    ReportResultAll isnew = new ReportResultAll()
                    {
                        Query = query.Attribute("name").Value.ToString(),
                        Group = query.Attribute("group").Value.ToString(),
                        projectName = root.Attribute("ProjectName").Value.ToString(),
                        presetName = root.Attribute("Preset").Value.ToString(),
                        teamName = root.Attribute("Team").Value.ToString(),
                        scanDate = Convert.ToDateTime(root.Attribute("ScanStart").Value.ToString()),
                        projectId = Convert.ToInt64(root.Attribute("ProjectId").Value.ToString()),
                        scanId = Convert.ToInt64(root.Attribute("ScanId").Value.ToString()),
                        status = el.Attribute("Status").Value.ToString(),
                        Severity = el.Attribute("Severity").Value.ToString(),
                        state = Convert.ToInt32(el.Attribute("state").Value.ToString())
                    };
                    response.Add(isnew);

                }
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failure reading XML from scan");
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                if (token.debug && token.verbosity > 1)
                {
                    Console.Error.WriteLine("Dumping XML:");
                    Console.Error.Write(result.ToString());
                }
                return true;
            }

        }
        private List<ReportResultAll> process_ScanResult(XElement result, long scanId)
        {
            List<ReportResultAll> reportResults = new List<ReportResultAll>();
            try
            {
                if (result.Attribute("ScanId").Value == scanId.ToString())
                {
                    IEnumerable<XElement> lastScan = from el in result.Descendants("Query").Descendants("Result")
                                                     select el;
                    foreach (XElement el in lastScan)
                    {
                        XElement query = el.Parent;
                        XElement root = query.Parent;
                        ReportResultAll isnew = new ReportResultAll()
                        {
                            Query = query.Attribute("name").Value.ToString(),
                            Group = query.Attribute("group").Value.ToString(),
                            projectName = root.Attribute("ProjectName").Value.ToString(),
                            presetName = root.Attribute("Preset").Value.ToString(),
                            teamName = root.Attribute("Team").Value.ToString(),
                            scanDate = Convert.ToDateTime(root.Attribute("ScanStart").Value.ToString()),
                            projectId = Convert.ToInt64(root.Attribute("ProjectId").Value.ToString()),
                            scanId = Convert.ToInt64(root.Attribute("ScanId").Value.ToString()),
                            status = el.Attribute("Status").Value.ToString(),
                            Severity = el.Attribute("Severity").Value.ToString(),
                            state = Convert.ToInt32(el.Attribute("state").Value.ToString())
                        };

                        reportResults.Add(isnew);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }

            return reportResults;

        }
        private bool findFirstandLastScan(long projectId, ScanObject scan, ScanStatistics scanStatistics, Dictionary<long, ReportStaging> keyStartPairs, Dictionary<long, ReportStaging> keyLastPairs)
        {
            getScans scans = new getScans();

            if (keyStartPairs.ContainsKey(scan.Project.Id))
            {
                ReportStaging reportStaging = keyStartPairs[scan.Project.Id];
                long diff = DateTimeOffset.Compare(reportStaging.dateTime, (DateTimeOffset)scan.DateAndTime.StartedOn);
                if (diff > 0)
                {
                    ReportStaging staging = new ReportStaging()
                    {
                        ProjectId = scan.Project.Id,
                        ProjectName = scan.Project.Name,
                        dateTime = (DateTimeOffset)scan.DateAndTime.StartedOn,
                        High = scanStatistics.HighSeverity,
                        Medium = scanStatistics.MediumSeverity,
                        Low = scanStatistics.LowSeverity,
                        ScanId = scan.Id
                    };
                    keyStartPairs[scan.Project.Id] = staging;
                }
            }
            else
            {
                keyStartPairs.Add(scan.Project.Id, new ReportStaging()
                {
                    ProjectId = scan.Project.Id,
                    ProjectName = scan.Project.Name,
                    dateTime = (DateTimeOffset)scan.DateAndTime.StartedOn,
                    High = scanStatistics.HighSeverity,
                    Medium = scanStatistics.MediumSeverity,
                    Low = scanStatistics.LowSeverity,
                    ScanId = scan.Id
                });
            }

            if (keyLastPairs.ContainsKey(scan.Project.Id))
            {
                ReportStaging reportStaging = keyLastPairs[scan.Project.Id];
                long diff = DateTimeOffset.Compare(reportStaging.dateTime, (DateTimeOffset)scan.DateAndTime.StartedOn);
                if (diff < 0)
                {
                    ReportStaging staging = new ReportStaging()
                    {
                        ProjectId = scan.Project.Id,
                        ProjectName = scan.Project.Name,
                        dateTime = (DateTimeOffset)scan.DateAndTime.StartedOn,
                        High = scanStatistics.HighSeverity,
                        Medium = scanStatistics.MediumSeverity,
                        Low = scanStatistics.LowSeverity,
                        ScanId = scan.Id
                    };
                    keyLastPairs[scan.Project.Id] = staging;
                }
            }
            else
            {
                keyLastPairs.Add(scan.Project.Id, new ReportStaging()
                {
                    ProjectId = scan.Project.Id,
                    ProjectName = scan.Project.Name,
                    dateTime = (DateTimeOffset)scan.DateAndTime.StartedOn,
                    High = scanStatistics.HighSeverity,
                    Medium = scanStatistics.MediumSeverity,
                    Low = scanStatistics.LowSeverity,
                    ScanId = scan.Id
                });
            }

            return true;
        }

        public bool waitForResult(List<ReportTrace> trace, getScanResults scanResults, List<ReportResultAll> resultNew, Dictionary<long, ReportStaging> start, Dictionary<long, ReportStaging> end, Dictionary<long, List<ReportResultAll>> first, Dictionary<long, List<ReportResultAll>> last)
        {
            ConsoleSpinner spinner = new ConsoleSpinner();
            bool waitFlag = false;
            DateTime wait_expired = DateTime.UtcNow;
            while (!waitFlag)
            {
                if (wait_expired.AddMinutes(2) < DateTime.UtcNow)
                {
                    Console.Error.WriteLine("waitForResult timeout! {0}", getTimeOutObjects(trace));
                    break;
                }
                spinner.Turn();
                waitFlag = true;
                if (token.debug && token.verbosity > 0) { Console.WriteLine("Sleeping 1 second(s)"); }
                Thread.Sleep(1000);
                foreach (ReportTrace rt in trace)
                {
                    if (!rt.isRead)
                    {
                        waitFlag = false;
                        if (token.debug && token.verbosity > 0) { Console.WriteLine("Testing report.Id {0}", rt.reportId); }
                        if (scanResults.GetResultStatus(rt.reportId, token))
                        {
                            if (token.debug && token.verbosity > 0) { Console.WriteLine("Found report.Id {0}", rt.reportId); }
                            Thread.Sleep(2000);
                            var result = scanResults.GetResult(rt.reportId, token);
                            if (result != null)
                            {
                                if (process_CxResponse(result, resultNew))
                                {
                                    rt.isRead = true;
                                    getFirstandLastReport(result, start, end, first, last);
                                }
                                else
                                {
                                    rt.isRead = true;
                                    if (token.debug && token.verbosity > 1)
                                    {
                                        Console.Error.WriteLine("Dumping XML:");
                                        Console.Error.Write(result.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        private string getTimeOutObjects(List<ReportTrace> trace)
        {
            string result = String.Empty;
            foreach (ReportTrace rt in trace)
            {
                result += String.Format("ProjectName {0}, ScanId {1}, TimeStamp {2}, isRead {3}", rt.projectName, rt.scanId, rt.TimeStamp, rt.isRead) + Environment.NewLine;
            }
            return result;
        }

        private bool writeXMLOutput(ReportTrace rt, XElement result)
        {
            try
            {
                if ((!String.IsNullOrEmpty(token.save_result)) && (!String.IsNullOrEmpty(token.save_result_path)))
                {
                    string filename = token.save_result_path + @"\" + rt.projectName + '-' + rt.scanTime.Value.ToString("yyyyMMddhhmmss") + ".xml";
                    File.WriteAllText(filename, result.ToString(), System.Text.Encoding.UTF8);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                throw ex;
            }
        }
        public bool matchProjectandTeam(ScanObject s, List<Teams> teams)
        {
            bool result = false;
            getScans scans = new getScans();

            string fullName = scans.getFullName(teams, s.OwningTeamId);

            if ((String.IsNullOrEmpty(token.project_name) || ((!String.IsNullOrEmpty(token.project_name)) && (s.Project.Name.Contains(token.project_name)))))
            {
                if ((String.IsNullOrEmpty(token.team_name) || ((!String.IsNullOrEmpty(token.team_name)) && (!String.IsNullOrEmpty(fullName)) && (fullName.Contains(token.team_name)))))
                {
                    result = true;
                }
            }
            return result;
        }

        public void Dispose()
        {

        }

    }

}