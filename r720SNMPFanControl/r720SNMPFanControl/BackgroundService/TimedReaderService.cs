using r720SNMPFanControl.Configs;
using SnmpSharpNet;
using System.Net;

namespace r720SNMPFanControl.BackgroundService
{
    public class TimedReaderService : IHostedService, IDisposable
    {
        private int executionCount = 0;
        private readonly ILogger<TimedReaderService> _logger;
        private Timer _timer = null!;
        private OIDs _Oids;

        public TimedReaderService(ILogger<TimedReaderService> logger, OIDs Oids)
        {
            _logger = logger;
            _Oids = Oids;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }

        private void DoWork(object? state)
        {
            var count = Interlocked.Increment(ref executionCount);

            _logger.LogInformation(
                "Timed Hosted Service is working. Count: {Count}", count);

            OctetString community = new OctetString("DaveHome");
            // Define agent parameters class
            AgentParameters param = new AgentParameters(community);
            // Set SNMP version to 1 (or 2)
            param.Version = SnmpVersion.Ver1;
            // Construct the agent address object
            // IpAddress class is easy to use here because
            //  it will try to resolve constructor parameter if it doesn't
            //  parse to an IP address
            IpAddress agent = new IpAddress("192.168.1.15");
            // Construct target
            UdpTarget target = new UdpTarget((IPAddress)agent, 161, 2000, 1);
            // Pdu class used for all requests
            Pdu pdu = new Pdu(PduType.Get);
            foreach(string fan in _Oids.Fans)
            {
                pdu.VbList.Add(fan);
            }

            foreach (string temperature in _Oids.Temperatures)
            {
                pdu.VbList.Add(temperature);
            }

            // Make SNMP request
            SnmpV1Packet result = (SnmpV1Packet)target.Request(pdu, param);
            // If result is null then agent didn't reply or we couldn't parse the reply.
            if (result != null)
            {
                // ErrorStatus other then 0 is an error returned by
                // the Agent - see SnmpConstants for error definitions
                if (result.Pdu.ErrorStatus != 0)
                {
                    // agent reported an error with the request
                    _logger.LogError("Error in SNMP reply. Error {0} index {1}",
                        result.Pdu.ErrorStatus,
                        result.Pdu.ErrorIndex);
                }
                else
                {
                    // Reply variables are returned in the same order as they were added
                    //  to the VbList

                    for (int i = 0; i < result.Pdu.VbList.Count();)
                    {
                        _logger.LogInformation("sysDescr({0}) ({1}): {2}",
                        result.Pdu.VbList[i].Oid.ToString(),
                        SnmpConstants.GetTypeName(result.Pdu.VbList[i].Value.Type),
                        result.Pdu.VbList[i].Value.ToString());
                        i++;
                    }
                }
            }
            else
            {
                _logger.LogError("No response received from SNMP agent.");
            }
            target.Close();



        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
