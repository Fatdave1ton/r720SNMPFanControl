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
            Pdu Temps = new Pdu(PduType.Get);
            Pdu CPUTemps = new Pdu(PduType.Get);
            Pdu FanRpms = new Pdu(PduType.Get);

            foreach (string fan in _Oids.Fans)
            {
                FanRpms.VbList.Add(fan);
            }
            foreach (string temperature in _Oids.Temperatures)
            {
                Temps.VbList.Add(temperature);
            }
            foreach (string temperature in _Oids.CPUTemperatures)
            {
                CPUTemps.VbList.Add(temperature);
            }

            Readings readings = new(_logger);

            readings.AddCpuReadings((SnmpV1Packet)target.Request(CPUTemps, param));
            readings.AddTempReadings((SnmpV1Packet)target.Request(Temps, param));
            readings.AddFanReadings((SnmpV1Packet)target.Request(FanRpms, param));

            int i = 1;
            foreach (int rpm in readings.FanRpms)
            {
                _logger.LogError(" fan {0} rpm {1}",
                                        i,
                                        rpm);
                i++;
            }
            i = 1;
            foreach (int temp in readings.Temps)
            {
                _logger.LogError(" temp {0} rpm {1}",
                                        i,
                                        temp);
                i++;
            }
            i = 1;
            foreach (int cputemp in readings.CPUTemps)
            {
                _logger.LogError(" temp {0} rpm {1}",
                                        i,
                                        cputemp);
                i++;
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
