using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using ARSoft.Tools.Net.Dns;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ARSoft.Tools.Net.Tests
{
    [TestClass]
    public class MxLookupTest
    {
        [TestMethod]
        public void CanValidateEmailsTest()
        {
            var emails = new List<string>();

            using (var sr = new StreamReader(new GZipStream(
                GetType().Assembly
                    .GetManifestResourceStream(
                        $"{GetType().Assembly.FullName.Split(',').First()}.TestData.MOCK_DATA.csv.gz") ??
                throw new InvalidOperationException(), CompressionMode.Decompress)))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    emails.Add(line);
                }
            }

            var domains = emails.Select(email => email.Substring(email.IndexOf('@') + 1)).Distinct().ToArray();

            var validDomains = new HashSet<string>();
            var checker =
                new DnsClient(DnsClient.GetLocalConfiguredDnsServers(), 30000);

            var aTask = Task.WhenAll(domains.Select(d => checker.ResolveAsync(DomainName.Parse(d))));
            var mxTask =
                Task.WhenAll(domains.Select(d => checker.ResolveAsync(DomainName.Parse(d), RecordType.Mx)));

            Task.WaitAll(mxTask, aTask);

            validDomains.UnionWith(mxTask.Result.Select(mx =>
                mx?.Questions.First().Name.ToString()));

            validDomains.UnionWith(aTask.Result.Select(a =>
                a?.Questions.First().Name.ToString()));

            Assert.AreEqual(435, validDomains.Count);
        }
    }
}